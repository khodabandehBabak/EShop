using Domain.Common;

namespace EShop.Domain.Entities;

public class Address : BaseEntity
{
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string ReceiverName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}