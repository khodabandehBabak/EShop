namespace EShop.Domain.Entities;

public class ReviewVote
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public Review Review { get; set; } = null!;
    public int UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public bool IsHelpful { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}