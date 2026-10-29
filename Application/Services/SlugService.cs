using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EShop.Application.Interfaces;

namespace EShop.Application.Services;

public class SlugService : ISlugService
{
    private static readonly Regex InvalidChars =
        new(@"[^\p{L}\p{Nd}\-]+", RegexOptions.Compiled);

    private static readonly Regex MultipleDashes =
        new(@"-{2,}", RegexOptions.Compiled);

    public string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        // یکسان‌سازی حروف عربی → فارسی
        var normalized = input.Trim()
            .Replace("ي", "ی")
            .Replace("ك", "ک")
            .Replace('\u200c', '-');

        // حذف اعراب و علائم ترکیبی
        normalized = normalized.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        normalized = sb.ToString().Normalize(NormalizationForm.FormC);

        normalized = normalized.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace('_', '-');

        normalized = InvalidChars.Replace(normalized, "");
        normalized = MultipleDashes.Replace(normalized, "-").Trim('-');

        return normalized;
    }

    public async Task<string> GenerateUniqueAsync(string input, Func<string, Task<bool>> existsCheck)
    {
        var baseSlug = Generate(input);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "item";

        var slug = baseSlug;
        var i = 2;
        while (await existsCheck(slug))
        {
            slug = $"{baseSlug}-{i++}";
            if (i > 1000) break;
        }
        return slug;
    }
}