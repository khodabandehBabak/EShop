using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

/// <summary>
/// سرویس نظرات و امتیازدهی
/// </summary>
public interface IReviewService
{
    // ============ PUBLIC ============
    Task<ReviewPageDto> GetForProductAsync(
        int productId, int page = 1, int pageSize = 10, int? currentUserId = null);

    Task<(bool Ok, string Message)> SubmitAsync(SubmitReviewDto dto, int userId);
    Task<(bool Ok, string Message)> VoteAsync(int reviewId, int userId, bool isHelpful);
    Task<(bool Ok, string Message)> DeleteOwnAsync(int reviewId, int userId);

    // ============ ADMIN ============
    Task<(List<ReviewAdminListDto> Items, int Total)> GetForAdminAsync(ReviewFilterDto filter);
    Task ApproveAsync(int reviewId, int moderatorId);
    Task<(bool Ok, string Message)> RejectAsync(int reviewId, string? reason, int moderatorId);
    Task DeleteAsync(int reviewId);

    // ============ CUSTOMER: MY REVIEWS ============
    Task<MyReviewsPageDto> GetMyReviewsAsync(
        int userId, string? statusFilter = null, string? search = null,
        int page = 1, int pageSize = 10);

    Task<MyReviewEditDto?> GetForEditAsync(int reviewId, int userId);
    Task<(bool Ok, string Message)> UpdateAsync(MyReviewEditDto dto, int userId);
}