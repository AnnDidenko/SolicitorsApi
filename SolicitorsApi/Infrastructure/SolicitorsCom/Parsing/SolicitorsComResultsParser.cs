using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Parsing;

public class SolicitorsComResultsParser : ISolicitorResultsParser
{
    public SolicitorResultsParseResult Parse(string html, Uri sourceUri, string? location = null)
    {
        var solicitors = HtmlParsing.Matches(
                html,
                @"<div\s+class=""result-item""[^>]*>(?<value>.*?)(?=<div\s+class=""result-item""|</main>|</body>)")
            .Select(match => ParseSolicitor(match.Groups["value"].Value, sourceUri, location))
            .Where(solicitor => !string.IsNullOrWhiteSpace(solicitor.Name))
            .ToArray();

        return new SolicitorResultsParseResult
        {
            Solicitors = solicitors,
            AreaOfLawOptions = ParseAreaOfLawOptions(html)
        };
    }

    public IReadOnlyList<AreaOfLaw> ParseAreaOfLawOptions(string html)
    {
        var areaSelectHtml = HtmlParsing.MatchAttribute(
            html,
            @"<select[^>]+name=""did""[^>]*>(?<value>.*?)</select>");
        var optionsHtml = string.IsNullOrWhiteSpace(areaSelectHtml)
            ? html
            : areaSelectHtml;

        return HtmlParsing.Matches(
                optionsHtml,
                @"<option\s+value=""(?<id>[^""]+)""[^>]*>(?<name>.*?)</option>")
            .Select(match =>
            {
                var name = HtmlParsing.NormalizeText(match.Groups["name"].Value);

                return new AreaOfLaw
                {
                    Name = name,
                    Slug = ToSlug(name),
                    SiteId = match.Groups["id"].Value.Trim()
                };
            })
            .Where(area => !string.IsNullOrWhiteSpace(area.Name))
            .ToArray();
    }

    private static Solicitor ParseSolicitor(string itemHtml, Uri sourceUri, string? location)
    {
        var name = ParseName(itemHtml);
        var profileHref = HtmlParsing.MatchAttribute(
            itemHtml,
            @"<a[^>]+href=""(?<value>[^""]+\.html)""[^>]*class=""link-map""");
        var address = HtmlParsing.MatchValue(itemHtml, @"<address[^>]*>(?<value>.*?)</address>");
        var phone = HtmlParsing.MatchAttribute(itemHtml, @"href=""tel:(?<value>[^""]+)""");
        var emailUrl = HtmlParsing.MatchAttribute(itemHtml, @"href=""(?<value>[^""]*enquiry-form\.asp[^""]*)""");
        var websiteUrl = HtmlParsing.MatchAttribute(
            itemHtml,
            @"<a[^>]+href=""(?<value>https?://[^""]+)""[^>]*>\s*(?:<i[^>]*></i>\s*)?Website");
        var profileUrl = BuildAbsoluteUrl(sourceUri, profileHref);

        return new Solicitor
        {
            Name = name ?? string.Empty,
            Location = location ?? address,
            City = location,
            ProfileSlug = HtmlParsing.ToSlugFromHref(profileHref),
            ProfileUrl = profileUrl,
            ContactDetails = new SolicitorContactDetails
            {
                Phone = phone,
                EmailUrl = BuildAbsoluteUrl(sourceUri, emailUrl),
                WebsiteUrl = websiteUrl,
                Address = address
            },
            Review = ParseReview(itemHtml)
        };
    }

    private static string? ParseName(string itemHtml)
    {
        var h2 = HtmlParsing.MatchAttribute(itemHtml, @"<span\s+class=""h2""[^>]*>(?<value>.*?)</span>");

        if (string.IsNullOrWhiteSpace(h2))
        {
            return null;
        }

        var withoutReviewCount = Regex.Replace(h2, @"\(\d+\)", string.Empty);

        return HtmlParsing.NormalizeText(withoutReviewCount);
    }

    private static ReviewSummary? ParseReview(string html)
    {
        var reviewBlock = HtmlParsing.MatchAttribute(
            html,
            @"<span\s+class=""rev-results""[^>]*>(?<value>.*?)</span>");

        if (string.IsNullOrWhiteSpace(reviewBlock))
        {
            return null;
        }

        var score = CountStars(reviewBlock);
        var countText = HtmlParsing.MatchAttribute(reviewBlock, @"\((?<value>\d+)\)");

        return new ReviewSummary
        {
            Score = score,
            Count = int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
                ? count
                : null
        };
    }

    private static decimal? CountStars(string html)
    {
        var full = HtmlParsing.Matches(html, @"star-full").Count;
        var half = HtmlParsing.Matches(html, @"star-half").Count;
        var score = full + (half * 0.5m);

        return score == 0 ? null : score;
    }

    private static string? BuildAbsoluteUrl(Uri sourceUri, string? href)
    {
        if (string.IsNullOrWhiteSpace(href))
        {
            return null;
        }

        return Uri.TryCreate(href, UriKind.Absolute, out var absoluteUri)
            ? absoluteUri.ToString()
            : new Uri(sourceUri, href).ToString();
    }

    private static string ToSlug(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var previousWasSeparator = false;

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (!previousWasSeparator)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
