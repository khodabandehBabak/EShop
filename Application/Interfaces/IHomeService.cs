using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس صفحه اصلی (با Cache)
/// </summary>
public interface IHomeService
{
    Task<HomeViewModel> GetHomeDataAsync();
    Task InvalidateCacheAsync();
}