namespace EShop.Domain.Enums;

public enum OrderStatus : byte
{
    Pending = 0,
    Paid = 1,
    Processing = 2,
    Packed = 3,
    Shipped = 4,
    Delivered = 5,
    Cancelled = 6,
    Returned = 7
}

public enum PaymentMethod : byte
{
    CashOnDelivery = 0,
    Online = 1
}

public enum ShippingMethod : byte
{
    Normal = 0,
    Express = 1,
    Pickup = 2
}
