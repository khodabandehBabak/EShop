using System.ComponentModel.DataAnnotations;

namespace EShop.Application.DTOs;

// ============ SUBMIT ============
public class SubmitReviewDto
{
    public int ProductId { get; set; }

    [Required(ErrorMessage = "امتیاز الزامی است")]
    [Range(1, 5, ErrorMessage = "امتیاز باید بین ۱ تا ۵ باشد")]
    public int Rating { get; set; }

    [MaxLength(100, ErrorMessage = "حداکثر ۱۰۰ کاراکتر")]
    [Display(Name = "عنوان (اختیاری)")]
    public string? Title { get; set; }

    [MaxLength(2000, ErrorMessage = "حداکثر ۲۰۰۰ کاراکتر")]
    [Display(Name = "نظر شما")]
    public string? Comment { get; set; }
}

// ============ PUBLIC LIST ============
public class ReviewListItemDto
{
    public int Id { get; set; }
    public string UserFullName { get; set; } = "";
    public string? UserAvatar { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CurrentUserVote { get; set; }
    public bool IsOwnReview { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }
}

public class ReviewPageDto
{
    public int ProductId { get; set; }
    public double? AvgRating { get; set; }
    public int TotalCount { get; set; }
    public int Rating5Count { get; set; }
    public int Rating4Count { get; set; }
    public int Rating3Count { get; set; }
    public int Rating2Count { get; set; }
    public int Rating1Count { get; set; }

    public List<ReviewListItemDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    public bool CanSubmitReview { get; set; }
    public bool HasAlreadyReviewed { get; set; }
    public bool IsLoggedIn { get; set; }
    public SubmitReviewDto? ExistingReview { get; set; }
}

// ============ ADMIN ============
public class ReviewFilterDto
{
    public int? ProductId { get; set; }
    public bool? IsApproved { get; set; }
    public int? MinRating { get; set; }
    public int? MaxRating { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ReviewAdminListDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductSlug { get; set; } = "";
    public string UserFullName { get; set; } = "";
    public string? UserPhone { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RejectionReason { get; set; }
}

// ============ CUSTOMER: MY REVIEWS ============
public class MyReviewListItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductSlug { get; set; } = "";
    public string? ProductImage { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class MyReviewsPageDto
{
    public List<MyReviewListItemDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(Total / (double)PageSize) : 0;
    public string? StatusFilter { get; set; }
    public string? Search { get; set; }
    public int ApprovedCount { get; set; }
    public int PendingCount { get; set; }
    public int RejectedCount { get; set; }
    public double? AvgRating { get; set; }
}

public class MyReviewEditDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ProductSlug { get; set; } = "";
    public string? ProductImage { get; set; }

    [Required(ErrorMessage = "امتیاز الزامی است")]
    [Range(1, 5, ErrorMessage = "امتیاز باید بین ۱ تا ۵ باشد")]
    public int Rating { get; set; }

    [MaxLength(100)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }

    public bool IsApproved { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool CanStillEdit { get; set; }
    public int RemainingHours { get; set; }
}