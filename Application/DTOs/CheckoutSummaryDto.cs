using EShop.Domain.Enums;

namespace EShop.Application.DTOs;

public class CheckoutSummaryDto
{
    public List<CartItemDto> Items { get; set; } = new();
    public decimal SubTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public List<ShippingOptionDto> ShippingOptions { get; set; } = new();
}

public class CartItemDto
{
    public int ProductId { get; set; }
    public int? VariantId { get; set; }
    public string Name { get; set; } = "";
    public string? VariantDescription { get; set; }
    public string? Sku { get; set; }
    public string ImageUrl { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public bool InStock { get; set; } = true;
    public decimal LineTotal => Price * Quantity;
}

public class ShippingOptionDto
{
    public ShippingMethod Method { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Cost { get; set; }
    public string EstimatedDelivery { get; set; } = "";
    public bool IsFree => Cost == 0;
}