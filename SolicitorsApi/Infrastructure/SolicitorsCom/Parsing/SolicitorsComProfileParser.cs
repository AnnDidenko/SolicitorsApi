using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Parsing;

public class SolicitorsComProfileParser : ISolicitorProfileParser
{
    public SolicitorProfile Parse(string html, Uri sourceUri)
    {
        var jsonLd = TryParseJsonLd(html);
        var profileUrl = HtmlParsing.MatchAttribute(html, @"<link\s+rel=""canonical""\s+href=""(?<value>[^""]+)""")
            ?? jsonLd?.Url
            ?? sourceUri.ToString();
        var name = HtmlParsing.MatchValue(html, @"<h1[^>]*>(?<value>.*?)</h1>")
            ?? jsonLd?.Name
            ?? string.Empty;

        return new SolicitorProfile
        {
            Name = name,
            Slug = HtmlParsing.ToSlugFromHref(profileUrl),
            ProfileUrl = profileUrl,
            ContactDetails = new SolicitorContactDetails
            {
                Phone = HtmlParsing.MatchAttribute(html, @"href=""tel:(?<value>[^""]+)""") ?? jsonLd?.Telephone,
                EmailUrl = BuildAbsoluteUrl(sourceUri, HtmlParsing.MatchAttribute(html, @"href=""(?<value>[^""]*enquiry-form\.asp[^""]*)""")),
                WebsiteUrl = HtmlParsing.MatchAttribute(html, @"<a[^>]+href=""(?<value>https?://[^""]+)""[^>]*class=""website"""),
                Address = ParsePrimaryAddress(html) ?? jsonLd?.Address
            },
            Offices = ParseOffices(html, jsonLd),
            AreasOfLaw = ParseAreasOfLaw(html, jsonLd),
            Review = ParseReview(html)
        };
    }

    private static ReviewSummary? ParseReview(string html)
    {
        var countText = HtmlParsing.MatchAttribute(html, @"Total\s+number\s+of\s+reviews\s*:\s*(?<value>\d+)");
        var scoreText = HtmlParsing.MatchAttribute(html, @"Average\s+review\s+score\s*:\s*(?<value>[0-9]+(?:\.[0-9]+)?)");

        if (countText is null && scoreText is null)
        {
            return null;
        }

        return new ReviewSummary
        {
            Score = decimal.TryParse(scoreText, NumberStyles.Number, CultureInfo.InvariantCulture, out var score)
                ? score
                : null,
            Count = int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count)
                ? count
                : null
        };
    }

    private static IReadOnlyList<SolicitorOffice> ParseOffices(string html, JsonLdProfile? jsonLd)
    {
        var offices = HtmlParsing.Matches(
                html,
                @"<div\s+class=""office-item""[^>]*>(?<value>.*?)(?=<div\s+class=""office-item""|<h2>|</body>)")
            .Select(match => ParseOffice(match.Groups["value"].Value))
            .Where(office =>
                !string.IsNullOrWhiteSpace(office.Address) ||
                !string.IsNullOrWhiteSpace(office.Phone))
            .ToArray();

        if (offices.Length > 0 || jsonLd?.Offices.Count is not > 0)
        {
            return offices;
        }

        return jsonLd.Offices;
    }

    private static SolicitorOffice ParseOffice(string officeHtml)
    {
        var scoreText = HtmlParsing.MatchAttribute(officeHtml, @"<div\s+class=""office-rating""[^>]*>(?<value>.*?)</div>");
        var name = HtmlParsing.MatchAttribute(officeHtml, @"title=""(?<value>.*?)\s+Office\s+Review\s+Score");

        return new SolicitorOffice
        {
            Name = name,
            Address = HtmlParsing.MatchValue(officeHtml, @"<address[^>]*>(?<value>.*?)</address>"),
            Phone = HtmlParsing.MatchAttribute(officeHtml, @"href=""tel:(?<value>[^""]+)"""),
            Review = decimal.TryParse(scoreText, NumberStyles.Number, CultureInfo.InvariantCulture, out var score)
                ? new ReviewSummary { Score = score }
                : null
        };
    }

    private static IReadOnlyList<AreaOfLaw> ParseAreasOfLaw(string html, JsonLdProfile? jsonLd)
    {
        var tagCloudHtml = HtmlParsing.MatchAttribute(
            html,
            @"<ul\s+class=""tag-cloud""[^>]*>(?<value>.*?)</ul>");

        var areas = string.IsNullOrWhiteSpace(tagCloudHtml)
            ? []
            : HtmlParsing.Matches(tagCloudHtml, @"<a[^>]+href=""(?<href>[^""]+)""[^>]*>(?<name>.*?)</a>")
                .Select(match =>
                {
                    var name = HtmlParsing.NormalizeText(match.Groups["name"].Value);

                    return new AreaOfLaw
                    {
                        Name = name,
                        Slug = ParseAreaSlug(match.Groups["href"].Value, name)
                    };
                })
                .Where(area => !string.IsNullOrWhiteSpace(area.Name))
                .ToArray();

        if (areas.Length > 0 || jsonLd?.AreasOfLaw.Count is not > 0)
        {
            return areas;
        }

        return jsonLd.AreasOfLaw;
    }

    private static string? ParsePrimaryAddress(string html)
    {
        return HtmlParsing.MatchValue(html, @"<a[^>]+class=""link-map""[^>]*>.*?<address[^>]*>(?<value>.*?)</address>");
    }

    private static string ParseAreaSlug(string href, string fallbackName)
    {
        var hrefSlug = HtmlParsing.ToSlugFromHref(href).Split('+')[0];

        return string.IsNullOrWhiteSpace(hrefSlug)
            ? Regex.Replace(fallbackName.Trim().ToLowerInvariant(), "\\s+", "-")
            : hrefSlug;
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

    private static JsonLdProfile? TryParseJsonLd(string html)
    {
        var jsonText = HtmlParsing.MatchAttribute(
            html,
            @"<script[^>]+type=""application/ld\+json""[^>]*>(?<value>.*?)</script>");

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(jsonText);
            var root = document.RootElement;

            return new JsonLdProfile
            {
                Name = GetString(root, "name"),
                Url = GetString(root, "url"),
                Telephone = GetString(root, "telephone"),
                Address = FormatAddress(TryGetProperty(root, "address")),
                Offices = ParseJsonLdOffices(root),
                AreasOfLaw = ParseJsonLdAreasOfLaw(root)
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<SolicitorOffice> ParseJsonLdOffices(JsonElement root)
    {
        var department = TryGetProperty(root, "department");

        if (department is null || department.Value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return department.Value.EnumerateArray()
            .Select(item => new SolicitorOffice
            {
                Name = GetString(item, "name"),
                Phone = GetString(item, "telephone"),
                Address = FormatAddress(TryGetProperty(item, "address"))
            })
            .Where(office =>
                !string.IsNullOrWhiteSpace(office.Name) ||
                !string.IsNullOrWhiteSpace(office.Address) ||
                !string.IsNullOrWhiteSpace(office.Phone))
            .ToArray();
    }

    private static IReadOnlyList<AreaOfLaw> ParseJsonLdAreasOfLaw(JsonElement root)
    {
        var serviceType = TryGetProperty(root, "serviceType");

        if (serviceType is null || serviceType.Value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return serviceType.Value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item =>
            {
                var name = item.GetString() ?? string.Empty;

                return new AreaOfLaw
                {
                    Name = name,
                    Slug = Regex.Replace(name.Trim().ToLowerInvariant(), "\\s+", "-")
                };
            })
            .Where(area => !string.IsNullOrWhiteSpace(area.Name))
            .ToArray();
    }

    private static string? FormatAddress(JsonElement? address)
    {
        if (address is null || address.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var parts = new[]
        {
            GetString(address.Value, "streetAddress"),
            GetString(address.Value, "addressLocality"),
            GetString(address.Value, "addressRegion"),
            GetString(address.Value, "postalCode"),
            GetString(address.Value, "addressCountry")
        };

        var formatted = string.Join(", ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));

        return string.IsNullOrWhiteSpace(formatted) ? null : formatted;
    }

    private static JsonElement? TryGetProperty(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var property)
                ? property
                : null;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
    }

    private class JsonLdProfile
    {
        public string? Name { get; init; }

        public string? Url { get; init; }

        public string? Telephone { get; init; }

        public string? Address { get; init; }

        public IReadOnlyList<SolicitorOffice> Offices { get; init; } = [];

        public IReadOnlyList<AreaOfLaw> AreasOfLaw { get; init; } = [];
    }
}
