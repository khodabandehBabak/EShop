// ============ Mega Menu (3-level) ============
public class MegaMenuCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Icon { get; set; }
    public string? ImageUrl { get; set; }
    public int ProductCount { get; set; }
    public List<MegaMenuChildDto> Children { get; set; } = new();
}

public class MegaMenuChildDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int ProductCount { get; set; }
    // ✅ جدید: نوه‌ها
    public List<MegaMenuGrandChildDto> Children { get; set; } = new();
}

public class MegaMenuGrandChildDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int ProductCount { get; set; }
}