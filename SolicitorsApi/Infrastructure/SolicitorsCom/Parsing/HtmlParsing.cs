using System.Net;
using System.Text.RegularExpressions;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Parsing;

internal static class HtmlParsing
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    public static string? MatchValue(string html, string pattern, string groupName = "value")
    {
        var match = Regex.Match(
            html,
            pattern,
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            RegexTimeout);

        return match.Success
            ? NormalizeText(match.Groups[groupName].Value)
            : null;
    }

    public static IReadOnlyList<Match> Matches(string html, string pattern)
    {
        return Regex.Matches(
                html,
                pattern,
                RegexOptions.IgnoreCase | RegexOptions.Singleline,
                RegexTimeout)
            .Cast<Match>()
            .ToArray();
    }

    public static string? MatchAttribute(string html, string pattern, string groupName = "value")
    {
        var match = Regex.Match(
            html,
            pattern,
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            RegexTimeout);

        return match.Success
            ? WebUtility.HtmlDecode(match.Groups[groupName].Value).Trim()
            : null;
    }

    public static string NormalizeText(string value)
    {
        var withoutTags = Regex.Replace(
            value,
            "<.*?>",
            " ",
            RegexOptions.IgnoreCase | RegexOptions.Singleline,
            RegexTimeout);
        var decoded = WebUtility.HtmlDecode(withoutTags);

        return Regex.Replace(decoded, "\\s+", " ", RegexOptions.None, RegexTimeout).Trim();
    }

    public static string ToSlugFromHref(string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return string.Empty;
        }

        var withoutQuery = href.Split('?', '#')[0];
        var fileName = withoutQuery.Trim('/').Split('/').LastOrDefault() ?? string.Empty;

        return fileName.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^5]
            : fileName;
    }
}
