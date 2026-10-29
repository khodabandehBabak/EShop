namespace EShop.Domain.Entities;

public class SiteSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? UpdatedByUserId { get; set; }
}