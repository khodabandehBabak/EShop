namespace EShop.Domain.Entities;

public class RolePermission
{
    public int RoleId { get; set; }
    public AppRole Role { get; set; } = null!;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}