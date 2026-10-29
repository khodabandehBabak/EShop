using Microsoft.AspNetCore.Identity;

namespace EShop.Domain.Entities;

public class AppRole : IdentityRole<int>
{
    public string? Description { get; set; }

    public AppRole() { }
    public AppRole(string roleName) : base(roleName) { }
}