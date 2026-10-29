using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Enums;
using EShop.Application.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace EShop.Application.Services;

public class SalesReportService : ISalesReportService
{
    private static readonly PersianCalendar _pc = new();

    private static readonly string[] _persianMonths =
        { "فروردین", "اردیبهشت", "خرداد", "تیر", "مرداد", "شهریور",
          "مهر", "آبان", "آذر", "دی", "بهمن", "اسفند" };

    private static readonly Dictionary<OrderStatus, string> _statusLabels = new()
    {
        [OrderStatus.Pending] = "در انتظار پرداخت",
        [OrderStatus.Paid] = "پرداخت شده",
        [OrderStatus.Processing] = "در حال پردازش",
        [OrderStatus.Packed] = "بسته‌بندی شده",
        [OrderStatus.Shipped] = "ارسال شده",
        [OrderStatus.Delivered] = "تحویل شده",
        [OrderStatus.Cancelled] = "لغو شده",
        [OrderStatus.Returned] = "مرجوعی"
    };

    private static readonly Dictionary<PaymentMethod, string> _paymentLabels = new()
    {
        [PaymentMethod.Online] = "پرداخت آنلاین",
        [PaymentMethod.CashOnDelivery] = "پرداخت درب منزل"
    };

    private readonly IApplicationDbContext _db;
    public SalesReportService(IApplicationDbContext db) => _db = db;

    public async Task<SalesReportDto> GenerateAsync(SalesReportFilterDto filter)
    {
        var (from, to) = filter.ResolveRange();
        var result = new SalesReportDto { Filter = filter, From = from, To = to };

        var successful = new[]
        {
            OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Packed,
            OrderStatus.Shipped, OrderStatus.Delivered
        };

        var ordersInRange = _db.Orders.Where(o => o.CreatedAt >= from && o.CreatedAt <= to);
        var successfulOrders = ordersInRange.Where(o => successful.Contains(o.Status));

        var totalOrders = await ordersInRange.CountAsync();
        var paidOrders = await successfulOrders.CountAsync();
        var pendingOrders = await ordersInRange.CountAsync(o => o.Status == OrderStatus.Pending);
        var cancelledOrders = await ordersInRange.CountAsync(o => o.Status == OrderStatus.Cancelled);
        var totalRevenue = await successfulOrders.SumAsync(o => (decimal?)o.TotalAmount) ?? 0;
        var uniqueCustomers = await ordersInRange.Where(o => o.CustomerId.HasValue)
            .Select(o => o.CustomerId).Distinct().CountAsync();
        var newCustomers = await _db.Users.CountAsync(u =>
            u.RegisteredAt >= from && u.RegisteredAt <= to);
        var totalItemsSold = await _db.OrderItems
            .Where(oi => oi.Order.CreatedAt >= from && oi.Order.CreatedAt <= to)
            .Where(oi => successful.Contains(oi.Order.Status))
            .SumAsync(oi => (int?)oi.Quantity) ?? 0;
        var uniqueProductsSold = await _db.OrderItems
            .Where(oi => oi.Order.CreatedAt >= from && oi.Order.CreatedAt <= to)
            .Where(oi => successful.Contains(oi.Order.Status))
            .Select(oi => oi.ProductId).Distinct().CountAsync();

        var aov = paidOrders > 0 ? totalRevenue / paidOrders : 0;

        var periodLength = (to - from).TotalDays;
        var prevFrom = from.AddDays(-periodLength);
        var prevTo = from.AddTicks(-1);
        var prevOrders = await _db.Orders.Where(o =>
            o.CreatedAt >= prevFrom && o.CreatedAt <= prevTo &&
            successful.Contains(o.Status)).ToListAsync();
        var prevCount = prevOrders.Count;
        var prevRevenue = prevOrders.Sum(o => o.TotalAmount);
        var prevAov = prevCount > 0 ? prevRevenue / prevCount : 0;

        result.Summary = new SalesSummaryDto
        {
            TotalOrders = totalOrders,
            PaidOrders = paidOrders,
            PendingOrders = pendingOrders,
            CancelledOrders = cancelledOrders,
            TotalRevenue = totalRevenue,
            AverageOrderValue = aov,
            UniqueCustomers = uniqueCustomers,
            NewCustomers = newCustomers,
            TotalItemsSold = totalItemsSold,
            UniqueProductsSold = uniqueProductsSold,
            RevenueGrowthPercent = GrowthPercent(prevRevenue, totalRevenue),
            OrdersGrowthPercent = GrowthPercent(prevCount, paidOrders),
            AovGrowthPercent = GrowthPercent(prevAov, aov)
        };

        var days = (to - from).TotalDays;
        result.SalesTimeline = days > 60
            ? await BuildMonthlyTimelineAsync(from, to, successful)
            : await BuildDailyTimelineAsync(from, to, successful);

        // Top products
        var topProductsRaw = await _db.OrderItems
            .Where(oi => oi.Order.CreatedAt >= from && oi.Order.CreatedAt <= to)
            .Where(oi => successful.Contains(oi.Order.Status))
            .GroupBy(oi => oi.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                QuantitySold = g.Sum(oi => oi.Quantity),
                OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                Revenue = g.Sum(oi => oi.TotalPrice)
            })
            .OrderByDescending(x => x.QuantitySold).Take(10).ToListAsync();

        var productIds = topProductsRaw.Select(x => x.ProductId).ToList();
        var productInfo = await _db.Products.Where(p => productIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Slug,
                Image = p.Images.Where(i => i.IsMain).Select(i => i.Path).FirstOrDefault()
            }).ToDictionaryAsync(p => p.Id);

        result.TopProducts = topProductsRaw.Select(x =>
        {
            var p = productInfo.GetValueOrDefault(x.ProductId);
            return new TopProductDto
            {
                Id = x.ProductId,
                Name = p?.Name ?? "—",
                Slug = p?.Slug ?? "",
                ImageUrl = p?.Image,
                QuantitySold = x.QuantitySold,
                OrderCount = x.OrderCount,
                Revenue = x.Revenue
            };
        }).ToList();

        // Top customers
        var topCustomersRaw = await _db.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .Where(o => successful.Contains(o.Status))
            .Where(o => o.CustomerId.HasValue)
            .GroupBy(o => o.CustomerId!.Value)
            .Select(g => new
            {
                CustomerId = g.Key,
                OrderCount = g.Count(),
                TotalSpent = g.Sum(o => o.TotalAmount),
                LastOrderAt = g.Max(o => o.CreatedAt)
            })
            .OrderByDescending(x => x.TotalSpent).Take(10).ToListAsync();

        var customerIds = topCustomersRaw.Select(x => x.CustomerId).ToList();
        var users = await _db.Users.Where(u => customerIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.PhoneNumber, u.Email })
            .ToDictionaryAsync(u => u.Id);

        result.TopCustomers = topCustomersRaw.Select(x =>
        {
            var u = users.GetValueOrDefault(x.CustomerId);
            return new TopCustomerDto
            {
                Id = x.CustomerId,
                FullName = u == null ? "—" : $"{u.FirstName} {u.LastName}".Trim(),
                PhoneNumber = u?.PhoneNumber,
                Email = u?.Email,
                OrderCount = x.OrderCount,
                TotalSpent = x.TotalSpent,
                AverageOrderValue = x.OrderCount > 0 ? x.TotalSpent / x.OrderCount : 0,
                LastOrderAt = x.LastOrderAt
            };
        }).ToList();

        // Order status breakdown
        var statusBreakdown = await ordersInRange.GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();

        result.OrderStatusBreakdown = statusBreakdown.Select(x => new NameValueDto
        {
            Name = _statusLabels.GetValueOrDefault(x.Status, x.Status.ToString()),
            Count = x.Count,
            Value = x.Count
        }).ToList();

        // Payment breakdown
        var paymentBreakdown = await successfulOrders.GroupBy(o => o.PaymentMethod)
            .Select(g => new { Method = g.Key, Count = g.Count(), Amount = g.Sum(o => o.TotalAmount) })
            .ToListAsync();

        result.PaymentMethodBreakdown = paymentBreakdown.Select(x => new NameValueDto
        {
            Name = _paymentLabels.GetValueOrDefault(x.Method, x.Method.ToString()),
            Count = x.Count,
            Value = x.Amount
        }).ToList();

        // ============ Category Sales — Method Syntax (بدون تداخل با کلمه‌کلیدی from) ============

        var categorySalesRaw = await _db.OrderItems
            .Where(oi => oi.Order.CreatedAt >= from
                      && oi.Order.CreatedAt <= to
                      && successful.Contains(oi.Order.Status))
            .GroupBy(oi => new { oi.Product!.CategoryId, oi.Product!.Category.Name })
            .Select(g => new
            {
                Name = g.Key.Name,
                Revenue = g.Sum(oi => oi.TotalPrice),
                Quantity = g.Sum(oi => oi.Quantity)
            })
            .OrderByDescending(x => x.Revenue)
            .Take(8)
            .ToListAsync();

        result.CategorySales = categorySalesRaw.Select(x => new NameValueDto
        {
            Name = x.Name,
            Value = x.Revenue,
            Count = x.Quantity
        }).ToList();

        //var _from = from;
        //var _to = to;

        //var categorySalesRaw = await (
        //    from oi in _db.OrderItems
        //    join o in _db.Orders on oi.OrderId equals o.Id
        //    join p in _db.Products on oi.ProductId equals p.Id
        //    join c in _db.Categories on p.CategoryId equals c.Id
        //    where o.CreatedAt >= _from && o.CreatedAt <= _to && successful.Contains(o.Status)
        //    group new { oi, c } by new { c.Id, c.Name } into g
        //    select new { g.Key.Name, Revenue = g.Sum(x => x.oi.TotalPrice), Quantity = g.Sum(x => x.oi.Quantity) }
        //).OrderByDescending(x => x.Revenue).Take(8).ToListAsync();

        //var categorySalesRaw = await (
        //    from oi in _db.OrderItems
        //    join o in _db.Orders on oi.OrderId equals o.Id
        //    join p in _db.Products on oi.ProductId equals p.Id
        //    join c in _db.Categories on p.CategoryId equals c.Id
        //    where o.CreatedAt >= from && o.CreatedAt <= to && successful.Contains(o.Status)
        //    group new { oi, c } by new { c.Id, c.Name } into g
        //    select new { g.Key.Name, Revenue = g.Sum(x => x.oi.TotalPrice), Quantity = g.Sum(x => x.oi.Quantity) }
        //).OrderByDescending(x => x.Revenue).Take(8).ToListAsync();

        result.CategorySales = categorySalesRaw.Select(x => new NameValueDto
        {
            Name = x.Name,
            Value = x.Revenue,
            Count = x.Quantity
        }).ToList();

        // Day of week
        var dowRaw = await successfulOrders.GroupBy(o => o.CreatedAt.DayOfWeek)
            .Select(g => new { DOW = g.Key, Count = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
            .ToListAsync();

        result.DayOfWeekSales = Enumerable.Range(0, 7).Select(d =>
        {
            var dow = (DayOfWeek)d;
            var m = dowRaw.FirstOrDefault(x => x.DOW == dow);
            return new NameValueDto
            {
                Name = GetPersianDayName(dow),
                Count = m?.Count ?? 0,
                Value = m?.Revenue ?? 0
            };
        }).ToList();

        return result;
    }

    private async Task<List<ChartPointDto>> BuildMonthlyTimelineAsync(
        DateTime from, DateTime to, OrderStatus[] statuses)
    {
        var raw = await _db.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to && statuses.Contains(o.Status))
            .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Count = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount)
            }).ToListAsync();

        var startMonth = new DateTime(from.Year, from.Month, 1);
        var endMonth = new DateTime(to.Year, to.Month, 1);

        var result = new List<ChartPointDto>();
        for (var m = startMonth; m <= endMonth; m = m.AddMonths(1))
        {
            var jm = _pc.GetMonth(m);
            var jy = _pc.GetYear(m);
            var label = $"{_persianMonths[jm - 1]} {jy}";
            var found = raw.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month);

            result.Add(new ChartPointDto
            {
                Label = label,
                Key = $"{m.Year}-{m.Month:D2}",
                Value = found?.Revenue ?? 0,
                Count = found?.Count ?? 0
            });
        }
        return result;
    }

    private async Task<List<ChartPointDto>> BuildDailyTimelineAsync(
        DateTime from, DateTime to, OrderStatus[] statuses)
    {
        var raw = await _db.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to && statuses.Contains(o.Status))
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
            .ToListAsync();

        var result = new List<ChartPointDto>();
        for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
        {
            var jDay = _pc.GetDayOfMonth(d);
            var jMonth = _pc.GetMonth(d);
            var found = raw.FirstOrDefault(r => r.Date == d);

            result.Add(new ChartPointDto
            {
                Label = $"{jDay} {_persianMonths[jMonth - 1]}",
                Key = d.ToString("yyyy-MM-dd"),
                Value = found?.Revenue ?? 0,
                Count = found?.Count ?? 0
            });
        }
        return result;
    }

    private static decimal GrowthPercent(decimal oldVal, decimal newVal)
    {
        if (oldVal == 0) return newVal > 0 ? 100 : 0;
        return Math.Round((newVal - oldVal) / oldVal * 100, 1);
    }

    private static string GetPersianDayName(DayOfWeek dow) => dow switch
    {
        DayOfWeek.Saturday => "شنبه",
        DayOfWeek.Sunday => "یکشنبه",
        DayOfWeek.Monday => "دوشنبه",
        DayOfWeek.Tuesday => "سه‌شنبه",
        DayOfWeek.Wednesday => "چهارشنبه",
        DayOfWeek.Thursday => "پنجشنبه",
        DayOfWeek.Friday => "جمعه",
        _ => ""
    };
}