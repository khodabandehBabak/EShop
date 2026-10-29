using EShop.Application.Interfaces;
using EShop.Application.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace EShop.Application.Services;

public class ProductViewTracker : IProductViewTracker
{
    private const string SessionKey = "ViewedProducts";
    private static readonly TimeSpan DedupWindow = TimeSpan.FromMinutes(30);
    private const int FlushThreshold = 50;

    private static readonly ConcurrentDictionary<int, int> _pending = new();
    private static DateTime _lastFlush = DateTime.UtcNow;
    private static readonly TimeSpan MinFlushInterval = TimeSpan.FromMinutes(10);
    private static readonly object _flushLock = new();

    private readonly IHttpContextAccessor _http;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProductViewTracker> _logger;

    public ProductViewTracker(IHttpContextAccessor http,
        IServiceScopeFactory scopeFactory, ILogger<ProductViewTracker> logger)
    {
        _http = http; _scopeFactory = scopeFactory; _logger = logger;
    }

    public int PendingCount => _pending.Count;

    public Task TrackAsync(int productId)
    {
        if (productId <= 0) return Task.CompletedTask;

        var session = _http.HttpContext?.Session;
        if (session == null)
        {
            _pending.AddOrUpdate(productId, 1, (_, v) => v + 1);
            TryFlushIfNeeded();
            return Task.CompletedTask;
        }

        var viewedDict = ParseViewed(session.GetString(SessionKey) ?? "");

        if (viewedDict.TryGetValue(productId, out var lastSeen) &&
            DateTime.UtcNow - lastSeen < DedupWindow)
            return Task.CompletedTask;

        viewedDict[productId] = DateTime.UtcNow;

        if (viewedDict.Count > 20)
        {
            var oldest = viewedDict.OrderBy(x => x.Value).First().Key;
            viewedDict.Remove(oldest);
        }

        session.SetString(SessionKey, SerializeViewed(viewedDict));
        _pending.AddOrUpdate(productId, 1, (_, v) => v + 1);
        TryFlushIfNeeded();
        return Task.CompletedTask;
    }

    public async Task FlushAsync()
    {
        Dictionary<int, int> snapshot;
        lock (_flushLock)
        {
            if (_pending.IsEmpty) return;
            snapshot = new Dictionary<int, int>(_pending);
            _pending.Clear();
            _lastFlush = DateTime.UtcNow;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            foreach (var (productId, count) in snapshot)
            {
                await db.Products.Where(p => p.Id == productId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.ViewCount, p => p.ViewCount + count)
                        .SetProperty(p => p.LastViewAt, DateTime.UtcNow));
            }
            _logger.LogInformation("Flushed view counts for {N} products", snapshot.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to flush view counts");
        }
    }

    private void TryFlushIfNeeded()
    {
        var shouldFlush = _pending.Count >= FlushThreshold ||
            DateTime.UtcNow - _lastFlush >= MinFlushInterval;
        if (!shouldFlush) return;
        _ = FlushAsync();
    }

    private static Dictionary<int, DateTime> ParseViewed(string raw)
    {
        var dict = new Dictionary<int, DateTime>();
        if (string.IsNullOrWhiteSpace(raw)) return dict;

        foreach (var entry in raw.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split(':');
            if (parts.Length != 2) continue;
            if (!int.TryParse(parts[0], out var pid)) continue;
            if (!DateTime.TryParse(parts[1], out var dt)) continue;
            dict[pid] = dt;
        }
        return dict;
    }

    private static string SerializeViewed(Dictionary<int, DateTime> dict)
        => string.Join("|", dict.Select(kv => $"{kv.Key}:{kv.Value:O}"));
}