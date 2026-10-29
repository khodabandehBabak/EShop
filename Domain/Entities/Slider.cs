using Domain.Common;
using EShop.Domain.Enums;

namespace EShop.Domain.Entities;

public class Slider : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? Description { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string? ImageMobileUrl { get; set; }
    public string? ProductImageUrl { get; set; }

    public string? BtnText { get; set; }
    public string? BtnLink { get; set; }
    public string? BgColor { get; set; }
    public string? TextColor { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public SliderPosition Position { get; set; } = SliderPosition.HeroMain;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Alt { get; set; }
}