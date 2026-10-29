using EShop.Application.Data;
using EShop.Application.DTOs;
using EShop.Application.Extensions;
using EShop.Application.Interfaces;
using EShop.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EShop.Application.Services;

public class CartService : ICartService
{
    private const string CartKey = "EShop.Cart";
    private const int MaxQuantityPerItem = 20;

    private readonly IApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly IShippingService _shipping;

    public CartService(IApplicationDbContext db, IHttpContextAccessor http, IShippingService shipping)
    {
        _db = db; _http = http; _shipping = shipping;
    }

    private List<CartStorageItem> Storage
    {
        get
        {
            var s = _http.HttpContext!.Session;
            return s.GetObject<List<CartStorageItem>>(CartKey) ?? new();
        }
        set => _http.HttpContext!.Session.SetObject(CartKey, value);
    }

    public Task AddAsync(int productId, int quantity)
        => AddAsync(productId, null, quantity);

    public async Task AddAsync(int productId, int? variantId, int quantity)
    {
        var product = await _db.Products.FindAsync(productId)
            ?? throw new InvalidOperationException("محصول یافت نشد");

        if (!product.IsActive)
            throw new InvalidOperationException("این محصول در دسترس نیست");

        int availableStock;
        if (product.HasVariants)
        {
            if (!variantId.HasValue)
                throw new InvalidOperationException("لطفاً ابتدا مشخصات محصول را انتخاب کنید");

            var variant = await _db.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId && v.IsActive)
                ?? throw new InvalidOperationException("این ترکیب از مشخصات موجود نیست");

            availableStock = variant.Stock;
        }
        else
        {
            if (variantId.HasValue)
                throw new InvalidOperationException("این محصول دارای متغیر نیست");
            availableStock = product.Stock;
        }

        if (availableStock < 1)
            throw new InvalidOperationException("این محصول موجود نیست");

        var storage = Storage;
        var existing = storage.FirstOrDefault(x =>
            x.ProductId == productId && x.VariantId == variantId);

        if (existing != null)
        {
            var newQty = Math.Min(existing.Quantity + quantity, MaxQuantityPerItem);
            if (newQty > availableStock)
                throw new InvalidOperationException($"فقط {availableStock} عدد موجود است");
            existing.Quantity = newQty;
        }
        else
        {
            if (quantity > availableStock)
                throw new InvalidOperationException($"فقط {availableStock} عدد موجود است");

            storage.Add(new CartStorageItem
            {
                ProductId = productId,
                VariantId = variantId,
                Quantity = Math.Clamp(quantity, 1, MaxQuantityPerItem)
            });
        }

        Storage = storage;
    }

    public async Task UpdateQuantityAsync(int productId, int? variantId, int quantity)
    {
        var storage = Storage;
        var item = storage.FirstOrDefault(x =>
            x.ProductId == productId && x.VariantId == variantId);
        if (item == null) return;

        if (quantity <= 0) storage.Remove(item);
        else item.Quantity = Math.Min(quantity, MaxQuantityPerItem);

        Storage = storage;
        await Task.CompletedTask;
    }

    public Task RemoveAsync(int productId, int? variantId)
    {
        var storage = Storage;
        storage.RemoveAll(x => x.ProductId == productId && x.VariantId == variantId);
        Storage = storage;
        return Task.CompletedTask;
    }

    public async Task<int> GetItemCountAsync()
        => await Task.FromResult(Storage.Sum(x => x.Quantity));

    public void Clear() => _http.HttpContext!.Session.Remove(CartKey);

    public async Task<CheckoutSummaryDto> GetSummaryAsync(ShippingMethod? method = null)
    {
        var storage = Storage;
        if (!storage.Any()) return new CheckoutSummaryDto();

        var productIds = storage.Select(x => x.ProductId).Distinct().ToList();
        var variantIds = storage.Where(x => x.VariantId.HasValue)
            .Select(x => x.VariantId!.Value).ToList();

        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Price,
                p.DiscountPrice,
                p.Stock,
                p.HasVariants,
                p.Sku,
                Image = p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Path).FirstOrDefault()
            })
            .ToListAsync();

        var variants = variantIds.Any()
            ? await _db.ProductVariants
                .Where(v => variantIds.Contains(v.Id))
                .Select(v => new
                {
                    v.Id,
                    v.Sku,
                    v.Price,
                    v.DiscountPrice,
                    v.Stock,
                    v.ImageUrl,
                    AttributeLabel = string.Join(" - ", v.AttributeValues
                        .OrderBy(av => av.Attribute.DisplayOrder)
                        .Select(av => av.Option.Value))
                })
                .ToListAsync()
            : new();

        var items = new List<CartItemDto>();
        foreach (var s in storage)
        {
            var p = products.FirstOrDefault(x => x.Id == s.ProductId);
            if (p == null) continue;

            CartItemDto item;
            if (s.VariantId.HasValue)
            {
                var v = variants.FirstOrDefault(x => x.Id == s.VariantId.Value);
                if (v == null) continue;
                var qty = Math.Min(s.Quantity, v.Stock);
                if (qty <= 0) continue;

                item = new CartItemDto
                {
                    ProductId = p.Id,
                    VariantId = v.Id,
                    Name = p.Name,
                    Sku = v.Sku,
                    VariantDescription = v.AttributeLabel,
                    ImageUrl = v.ImageUrl ?? p.Image ?? "/assets/images/placeholder.jpg",
                    Price = v.DiscountPrice ?? v.Price,
                    Quantity = qty,
                    InStock = v.Stock >= s.Quantity
                };
            }
            else
            {
                var qty = Math.Min(s.Quantity, p.Stock);
                if (qty <= 0) continue;

                item = new CartItemDto
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    ImageUrl = p.Image ?? "/assets/images/placeholder.jpg",
                    Price = p.DiscountPrice ?? p.Price,
                    Quantity = qty,
                    InStock = p.Stock >= s.Quantity
                };
            }

            items.Add(item);
        }

        var subTotal = items.Sum(x => x.LineTotal);
        var selectedMethod = method ?? ShippingMethod.Normal;
        var shippingCost = items.Any()
            ? await _shipping.GetCostAsync(selectedMethod, subTotal)
            : 0;
        var shippingOptions = items.Any()
            ? await _shipping.GetOptionsAsync(subTotal, "")
            : new List<ShippingOptionDto>();

        return new CheckoutSummaryDto
        {
            Items = items,
            ItemCount = items.Sum(x => x.Quantity),
            SubTotal = subTotal,
            ShippingCost = shippingCost,
            DiscountAmount = 0,
            TotalAmount = subTotal + shippingCost,
            ShippingOptions = shippingOptions
        };
    }

    private class CartStorageItem
    {
        public int ProductId { get; set; }
        public int? VariantId { get; set; }
        public int Quantity { get; set; }
    }
}