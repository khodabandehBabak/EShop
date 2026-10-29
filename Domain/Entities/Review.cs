using Domain.Common;

namespace EShop.Domain.Entities;

public class Review : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }

    public bool IsApproved { get; set; } = false;
    public bool IsVerifiedPurchase { get; set; } = false;
    public string? RejectionReason { get; set; }

    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public ICollection<ReviewVote> Votes { get; set; } = new List<ReviewVote>();
}