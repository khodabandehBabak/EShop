namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس تولید Slug یکتا
/// </summary>
public interface ISlugService
{
    /// <summary>تولید slug از متن (بدون تضمین یکتایی)</summary>
    string Generate(string input);

    /// <summary>تولید slug یکتا با بررسی وجود</summary>
    Task<string> GenerateUniqueAsync(string input, Func<string, Task<bool>> existsCheck);
}