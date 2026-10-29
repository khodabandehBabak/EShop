using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Enums;
using EShop.Application.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;

namespace EShop.Application.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _db;
    private readonly ILogger<OrderService> _logger;
    private readonly IHttpContextAccessor _http;

    public OrderService(IApplicationDbContext db, ILogger<OrderService> logger, IHttpContextAccessor http)
    {
        _db = db; _logger = logger; _http = http;
    }

    public async Task<Order> CreateAsync(CheckoutInputDto input, CheckoutSummaryDto summary)
    {
        if (!summary.Items.Any())
            throw new InvalidOperationException("سبد خرید خالی است");

        string orderNumber = $"ES-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";
        while (await _db.Orders.AnyAsync(o => o.OrderNumber == orderNumber))
            orderNumber = $"ES-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(10000, 99999)}";

        var userIdClaim = _http.HttpContext?.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier);
        int? customerId = userIdClaim != null ? int.Parse(userIdClaim.Value) : null;

        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = customerId,
            ReceiverName = input.ReceiverName,
            ReceiverPhone = input.ReceiverPhone,
            Province = input.Province,
            City = input.City,
            PostalCode = input.PostalCode,
            FullAddress = input.FullAddress,
            PaymentMethod = input.PaymentMethod,
            ShippingMethod = input.ShippingMethod,
            SubTotal = summary.SubTotal,
            ShippingCost = summary.ShippingCost,
            DiscountAmount = summary.DiscountAmount,
            TotalAmount = summary.TotalAmount,
            Status = OrderStatus.Pending,
            InternalNote = input.Note,
            Items = summary.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.Name,
                UnitPrice = i.Price,
                Quantity = i.Quantity,
                TotalPrice = i.LineTotal
            }).ToList()
        };

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Pending,
            Note = "ثبت سفارش توسط مشتری",
            ChangedAt = DateTime.UtcNow
        });

        _db.Orders.Add(order);

        // کاهش موجودی
        foreach (var item in summary.Items)
        {
            var product = await _db.Products.FindAsync(item.ProductId);
            if (product == null) continue;

            if (product.Stock < item.Quantity)
                throw new InvalidOperationException(
                    $"موجودی محصول «{product.Name}» کافی نیست");

            product.Stock -= item.Quantity;
        }

        // ذخیره آدرس
        if (customerId.HasValue && input.SaveAddress)
        {
            var exists = await _db.Addresses.AnyAsync(a =>
                a.UserId == customerId.Value &&
                a.FullAddress == input.FullAddress &&
                a.PostalCode == input.PostalCode);

            if (!exists)
            {
                _db.Addresses.Add(new Address
                {
                    UserId = customerId.Value,
                    ReceiverName = input.ReceiverName,
                    Phone = input.ReceiverPhone,
                    Province = input.Province,
                    City = input.City,
                    PostalCode = input.PostalCode,
                    FullAddress = input.FullAddress
                });
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Order {OrderNumber} created. Total: {Total}",
            orderNumber, order.TotalAmount);
        return order;
    }

    public async Task<Order?> GetByOrderNumberAsync(string orderNumber)
        => await _db.Orders.Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

    public async Task<Order?> GetByIdAsync(int id)
        => await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);

    public async Task MarkAsPaidAsync(int orderId, string refId)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return;

        if (order.Status == OrderStatus.Paid) return; // idempotent

        order.Status = OrderStatus.Paid;
        order.PaymentTrackingCode = refId;
        order.PaidAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Paid,
            Note = $"پرداخت آنلاین موفق - شماره پیگیری: {refId}",
            ChangedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Order {OrderNumber} paid. RefId: {RefId}",
            order.OrderNumber, refId);
    }

    public async Task MarkAsFailedAsync(int orderId, string? reason = null)
    {
        var order = await _db.Orders.FindAsync(orderId);
        if (order == null) return;

        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Pending,
            Note = $"تلاش پرداخت ناموفق - {reason ?? "نامشخص"}",
            ChangedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}