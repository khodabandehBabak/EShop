using System.ComponentModel.DataAnnotations;

namespace EShop.Application.DTOs;

// ============ USERS ============
public class UserListItemDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UserCreateDto
{
    [Required(ErrorMessage = "نام الزامی است")]
    [MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "نام خانوادگی الزامی است")]
    [MaxLength(100)]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "شماره موبایل الزامی است")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست")]
    public string PhoneNumber { get; set; } = "";

    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
    public string? Email { get; set; }

    [MaxLength(10)]
    public string? NationalCode { get; set; }

    [Required(ErrorMessage = "رمز عبور الزامی است")]
    [MinLength(6, ErrorMessage = "حداقل ۶ کاراکتر")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "تکرار رمز الزامی است")]
    [Compare(nameof(Password), ErrorMessage = "رمز عبور و تکرار آن یکسان نیستند")]
    public string ConfirmPassword { get; set; } = "";

    public bool IsActive { get; set; } = true;
    public List<int> RoleIds { get; set; } = new();
    public List<int> DirectPermissionIds { get; set; } = new();
}

public class UserEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام الزامی است")]
    [MaxLength(100)]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "نام خانوادگی الزامی است")]
    [MaxLength(100)]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "شماره موبایل الزامی است")]
    [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل معتبر نیست")]
    public string PhoneNumber { get; set; } = "";

    [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
    public string? Email { get; set; }

    [MaxLength(10)]
    public string? NationalCode { get; set; }

    public bool IsActive { get; set; }

    public string? NewPassword { get; set; }

    [Compare(nameof(NewPassword), ErrorMessage = "رمزها یکسان نیستند")]
    public string? ConfirmNewPassword { get; set; }

    public List<int> RoleIds { get; set; } = new();
    public List<int> DirectPermissionIds { get; set; } = new();

    public string UserNameDisplay { get; set; } = "";
    public bool IsSelf { get; set; }
    public DateTime RegisteredAt { get; set; }
}

// ============ ROLES ============
public class RoleListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int UserCount { get; set; }
    public int PermissionCount { get; set; }
    public bool IsSystemRole { get; set; }
}

public class RoleCreateDto
{
    [Required(ErrorMessage = "نام نقش الزامی است")]
    [MaxLength(100)]
    [RegularExpression(@"^[A-Za-z][A-Za-z0-9_]*$",
        ErrorMessage = "فقط حروف انگلیسی، عدد و _ مجاز است")]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    public List<int> PermissionIds { get; set; } = new();
}

public class RoleEditDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "نام نقش الزامی است")]
    [MaxLength(100)]
    [RegularExpression(@"^[A-Za-z][A-Za-z0-9_]*$",
        ErrorMessage = "فقط حروف انگلیسی، عدد و _ مجاز است")]
    public string Name { get; set; } = "";

    [MaxLength(500)]
    public string? Description { get; set; }

    public List<int> PermissionIds { get; set; } = new();
    public bool IsSystemRole { get; set; }
    public int UserCount { get; set; }
}

// ============ PERMISSIONS ============
public class PermissionGroupDto
{
    public string Group { get; set; } = "";
    public List<PermissionItemDto> Permissions { get; set; } = new();
}

public class PermissionItemDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Group { get; set; } = "";
    public int DisplayOrder { get; set; }
}