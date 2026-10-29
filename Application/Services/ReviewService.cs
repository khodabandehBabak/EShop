using EShop.Application.DTOs;
using EShop.Application.Interfaces;
using EShop.Domain.Entities;
using EShop.Domain.Enums;
using EShop.Application.Data;
using Microsoft.EntityFrameworkCore;

namespace EShop.Application.Services;

public class ReviewService : IReviewService
{
    private const int EditWindowHours = 24;

    private readonly IApplicationDbContext _db;
    public ReviewService(IApplicationDbContext db) => _db = db;

    public async Task<ReviewPageDto> GetForProductAsync(
        int productId, int page = 1, int pageSize = 10, int? currentUserId = null)
    {
        var allApproved = _db.Reviews.Where(r => r.ProductId == productId && r.IsApproved);

        var total = await allApproved.CountAsync();
        var avg = await allApproved.AnyAsync()
            ? await allApproved.AverageAsync(r => (double)r.Rating) : (double?)null;

        var breakdown = await allApproved.GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() }).ToListAsync();
        int GetCount(int s) => breakdown.FirstOrDefault(b => b.Rating == s)?.Count ?? 0;

        page = Math.Max(1, page);
        var items = await allApproved
            .OrderByDescending(r => r.HelpfulCount).ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new ReviewListItemDto
            {
                Id = r.Id,
                UserFullName = $"{r.User.FirstName} {r.User.LastName}".Trim(),
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                HelpfulCount = r.HelpfulCount,
                NotHelpfulCount = r.NotHelpfulCount,
                CreatedAt = r.CreatedAt,
                IsApproved = r.IsApproved
            })
            .ToListAsync();

        var dto = new ReviewPageDto
        {
            ProductId = productId,
            AvgRating = avg,
            TotalCount = total,
            Rating5Count = GetCount(5),
            Rating4Count = GetCount(4),
            Rating3Count = GetCount(3),
            Rating2Count = GetCount(2),
            Rating1Count = GetCount(1),
            Items = items,
            Page = page,
            PageSize = pageSize,
            IsLoggedIn = currentUserId.HasValue
        };

        if (currentUserId.HasValue)
        {
            var own = await _db.Reviews.FirstOrDefaultAsync(r =>
                r.ProductId == productId && r.UserId == currentUserId.Value);

            dto.CanSubmitReview = own == null;
            dto.HasAlreadyReviewed = own != null;

            if (own != null)
            {
                dto.ExistingReview = new SubmitReviewDto
                {
                    ProductId = productId,
                    Rating = own.Rating,
                    Title = own.Title,
                    Comment = own.Comment
                };
            }

            var reviewIds = items.Select(i => i.Id).ToList();
            var votes = await _db.ReviewVotes
                .Where(v => reviewIds.Contains(v.ReviewId) && v.UserId == currentUserId.Value)
                .ToDictionaryAsync(v => v.ReviewId, v => v.IsHelpful);

            foreach (var item in items)
            {
                if (votes.TryGetValue(item.Id, out var h))
                    item.CurrentUserVote = h ? "helpful" : "nothelpful";

                var review = await _db.Reviews.Where(r => r.Id == item.Id)
                    .Select(r => new { r.UserId, r.CreatedAt }).FirstOrDefaultAsync();
                if (review != null)
                {
                    item.IsOwnReview = review.UserId == currentUserId.Value;
                    item.CanDelete = item.IsOwnReview;
                    item.CanEdit = item.IsOwnReview &&
                        (DateTime.UtcNow - review.CreatedAt).TotalHours <= EditWindowHours;
                }
            }
        }

        return dto;
    }

    public async Task<(bool Ok, string Message)> SubmitAsync(SubmitReviewDto dto, int userId)
    {
        var productExists = await _db.Products.AnyAsync(p => p.Id == dto.ProductId && p.IsActive);
        if (!productExists) return (false, "محصول یافت نشد");

        var existing = await _db.Reviews.FirstOrDefaultAsync(r =>
            r.ProductId == dto.ProductId && r.UserId == userId);
        if (existing != null) return (false, "شما قبلاً نظر داده‌اید");

        var isVerified = await _db.OrderItems
            .Where(oi => oi.ProductId == dto.ProductId && oi.Order.CustomerId == userId)
            .Where(oi => oi.Order.Status == OrderStatus.Delivered ||
                         oi.Order.Status == OrderStatus.Shipped)
            .AnyAsync();

        _db.Reviews.Add(new Review
        {
            ProductId = dto.ProductId,
            UserId = userId,
            Rating = Math.Clamp(dto.Rating, 1, 5),
            Title = dto.Title?.Trim(),
            Comment = dto.Comment?.Trim(),
            IsVerifiedPurchase = isVerified,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return (true, "نظر شما ثبت شد و پس از تأیید نمایش داده می‌شود");
    }

    public async Task<(bool Ok, string Message)> VoteAsync(int reviewId, int userId, bool isHelpful)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId && r.IsApproved);
        if (review == null) return (false, "نظر یافت نشد");
        if (review.UserId == userId) return (false, "نمی‌توانید به نظر خودتان رأی دهید");

        var existingVote = await _db.ReviewVotes
            .FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.UserId == userId);

        if (existingVote != null)
        {
            if (existingVote.IsHelpful == isHelpful)
            {
                _db.ReviewVotes.Remove(existingVote);
                if (isHelpful) review.HelpfulCount = Math.Max(0, review.HelpfulCount - 1);
                else review.NotHelpfulCount = Math.Max(0, review.NotHelpfulCount - 1);
                await _db.SaveChangesAsync();
                return (true, "رأی شما برداشته شد");
            }

            if (existingVote.IsHelpful)
            {
                review.HelpfulCount = Math.Max(0, review.HelpfulCount - 1);
                review.NotHelpfulCount++;
            }
            else
            {
                review.NotHelpfulCount = Math.Max(0, review.NotHelpfulCount - 1);
                review.HelpfulCount++;
            }
            existingVote.IsHelpful = isHelpful;
        }
        else
        {
            _db.ReviewVotes.Add(new ReviewVote
            {
                ReviewId = reviewId,
                UserId = userId,
                IsHelpful = isHelpful
            });
            if (isHelpful) review.HelpfulCount++; else review.NotHelpfulCount++;
        }

        await _db.SaveChangesAsync();
        return (true, "ممنون از بازخورد");
    }

    public async Task<(bool Ok, string Message)> DeleteOwnAsync(int reviewId, int userId)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r =>
            r.Id == reviewId && r.UserId == userId);
        if (review == null) return (false, "نظر یافت نشد");

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
        return (true, "نظر حذف شد");
    }

    public async Task<(List<ReviewAdminListDto> Items, int Total)> GetForAdminAsync(
        ReviewFilterDto filter)
    {
        var q = _db.Reviews.AsQueryable();

        if (filter.ProductId.HasValue) q = q.Where(r => r.ProductId == filter.ProductId.Value);
        if (filter.IsApproved.HasValue) q = q.Where(r => r.IsApproved == filter.IsApproved.Value);
        if (filter.MinRating.HasValue) q = q.Where(r => r.Rating >= filter.MinRating.Value);
        if (filter.MaxRating.HasValue) q = q.Where(r => r.Rating <= filter.MaxRating.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            q = q.Where(r =>
                (r.Comment != null && r.Comment.Contains(s)) ||
                (r.Title != null && r.Title.Contains(s)) ||
                r.User.FirstName.Contains(s) || r.User.LastName.Contains(s) ||
                r.Product.Name.Contains(s));
        }

        var total = await q.CountAsync();

        var items = await q
            .OrderBy(r => r.IsApproved).ThenByDescending(r => r.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize)
            .Select(r => new ReviewAdminListDto
            {
                Id = r.Id,
                ProductName = r.Product.Name,
                ProductSlug = r.Product.Slug,
                UserFullName = $"{r.User.FirstName} {r.User.LastName}".Trim(),
                UserPhone = r.User.PhoneNumber,
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                IsApproved = r.IsApproved,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                CreatedAt = r.CreatedAt,
                RejectionReason = r.RejectionReason
            })
            .ToListAsync();

        return (items, total);
    }

    public async Task ApproveAsync(int reviewId, int moderatorId)
    {
        var review = await _db.Reviews.FindAsync(reviewId);
        if (review == null) return;

        review.IsApproved = true;
        review.ApprovedAt = DateTime.UtcNow;
        review.RejectionReason = null;
        review.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<(bool Ok, string Message)> RejectAsync(
        int reviewId, string? reason, int moderatorId)
    {
        var review = await _db.Reviews.FindAsync(reviewId);
        if (review == null) return (false, "نظر یافت نشد");

        review.IsApproved = false;
        review.RejectionReason = reason?.Trim();
        review.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, "نظر رد شد");
    }

    public async Task DeleteAsync(int reviewId)
    {
        var review = await _db.Reviews.FindAsync(reviewId);
        if (review == null) return;
        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
    }

    public async Task<MyReviewsPageDto> GetMyReviewsAsync(
        int userId, string? statusFilter = null, string? search = null,
        int page = 1, int pageSize = 10)
    {
        var baseQuery = _db.Reviews.Where(r => r.UserId == userId);

        var all = await baseQuery.ToListAsync();
        var approvedCount = all.Count(r => r.IsApproved);
        var pendingCount = all.Count(r => !r.IsApproved && string.IsNullOrEmpty(r.RejectionReason));
        var rejectedCount = all.Count(r => !r.IsApproved && !string.IsNullOrEmpty(r.RejectionReason));
        var avgRating = all.Any() ? all.Average(r => (double)r.Rating) : (double?)null;

        var query = baseQuery;
        switch (statusFilter)
        {
            case "approved": query = query.Where(r => r.IsApproved); break;
            case "pending": query = query.Where(r => !r.IsApproved && string.IsNullOrEmpty(r.RejectionReason)); break;
            case "rejected": query = query.Where(r => !r.IsApproved && !string.IsNullOrEmpty(r.RejectionReason)); break;
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(r =>
                r.Product.Name.Contains(s) ||
                (r.Title != null && r.Title.Contains(s)) ||
                (r.Comment != null && r.Comment.Contains(s)));
        }

        var total = await query.CountAsync();
        page = Math.Max(1, page);

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new MyReviewListItemDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product.Name,
                ProductSlug = r.Product.Slug,
                ProductImage = r.Product.Images.Where(i => i.IsMain).Select(i => i.Path).FirstOrDefault()
                              ?? r.Product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Path).FirstOrDefault(),
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                IsApproved = r.IsApproved,
                RejectionReason = r.RejectionReason,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                HelpfulCount = r.HelpfulCount,
                NotHelpfulCount = r.NotHelpfulCount,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                ApprovedAt = r.ApprovedAt
            })
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            var age = now - item.CreatedAt;
            item.CanEdit = !item.IsApproved && age.TotalHours <= EditWindowHours;
            item.CanDelete = age.TotalHours <= EditWindowHours ||
                (!item.IsApproved && !string.IsNullOrEmpty(item.RejectionReason));
        }

        return new MyReviewsPageDto
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize,
            StatusFilter = statusFilter,
            Search = search,
            ApprovedCount = approvedCount,
            PendingCount = pendingCount,
            RejectedCount = rejectedCount,
            AvgRating = avgRating
        };
    }

    public async Task<MyReviewEditDto?> GetForEditAsync(int reviewId, int userId)
    {
        var review = await _db.Reviews.Include(r => r.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);
        if (review == null) return null;

        var age = DateTime.UtcNow - review.CreatedAt;
        var canEdit = !review.IsApproved && age.TotalHours <= EditWindowHours;
        var remaining = Math.Max(0, EditWindowHours - (int)age.TotalHours);

        return new MyReviewEditDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            ProductName = review.Product.Name,
            ProductSlug = review.Product.Slug,
            ProductImage = review.Product.Images.Where(i => i.IsMain).Select(i => i.Path).FirstOrDefault()
                          ?? review.Product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Path).FirstOrDefault(),
            Rating = review.Rating,
            Title = review.Title,
            Comment = review.Comment,
            IsApproved = review.IsApproved,
            RejectionReason = review.RejectionReason,
            CreatedAt = review.CreatedAt,
            CanStillEdit = canEdit,
            RemainingHours = remaining
        };
    }

    public async Task<(bool Ok, string Message)> UpdateAsync(MyReviewEditDto dto, int userId)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r =>
            r.Id == dto.Id && r.UserId == userId);
        if (review == null) return (false, "نظر یافت نشد یا متعلق به شما نیست");

        if (review.IsApproved)
            return (false, "نظر تأییدشده قابل ویرایش نیست");

        var age = DateTime.UtcNow - review.CreatedAt;
        if (age.TotalHours > EditWindowHours)
            return (false, $"مهلت ویرایش ({EditWindowHours} ساعت) به پایان رسیده");

        review.Rating = Math.Clamp(dto.Rating, 1, 5);
        review.Title = dto.Title?.Trim();
        review.Comment = dto.Comment?.Trim();
        review.IsApproved = false;
        review.RejectionReason = null;
        review.ApprovedAt = null;
        review.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, "نظر شما بروزرسانی شد و مجدداً در صف تأیید قرار گرفت");
    }
}