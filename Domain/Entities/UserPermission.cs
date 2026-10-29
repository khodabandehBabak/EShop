namespace EShop.Domain.Entities;

public class UserPermission
{
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public bool IsGranted { get; set; } = true;
}