using System.Globalization;
using System.Text;

namespace AutoVeritas.OffersService.Seeding;

public static class SlugGenerator
{
    /// <summary>
    /// Normalizes a display name into a stable kebab-case identity: lower-cased,
    /// diacritics stripped, everything non-alphanumeric collapsed to single dashes,
    /// capped at the Slug column length. Can return an empty string for names with
    /// no ASCII alphanumerics — callers must treat that as "explicit slug required".
    /// </summary>
    public static string From(string text, int maxLength = 160)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var previousDash = true;

        foreach (var ch in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(ch))
            {
                builder.Append(char.ToLowerInvariant(ch));
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return slug.Length <= maxLength ? slug : slug[..maxLength].Trim('-');
    }
}
