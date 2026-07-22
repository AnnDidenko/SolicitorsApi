using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using SolicitorsApi.Domain;
using SolicitorsApi.Infrastructure.SolicitorsCom.Configuration;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Routing;

public class SolicitorsComUrlBuilder
{
    private readonly SolicitorsComOptions _options;
    private readonly Uri _baseUri;

    public SolicitorsComUrlBuilder(IOptions<SolicitorsComOptions> options)
    {
        _options = options.Value;
        _baseUri = new Uri(_options.BaseUrl, UriKind.Absolute);
    }

    public Uri BuildConveyancingUri()
    {
        return BuildRelativeUri(_options.ConveyancingPath);
    }

    public Uri BuildHomeUri()
    {
        return BuildRelativeUri("/");
    }

    public Uri BuildPrepareSearchUri()
    {
        return BuildRelativeUri(_options.PrepareSearchPath);
    }

    public Uri BuildCitySearchUri(string location)
    {
        var citySlug = ToSlug(location);

        return BuildRelativeUri($"{citySlug}-solicitors.html");
    }

    public Uri BuildAreaOfLawSearchUri(AreaOfLaw areaOfLaw, string location)
    {
        var areaSlug = string.IsNullOrWhiteSpace(areaOfLaw.Slug)
            ? ToSlug(areaOfLaw.Name)
            : ToSlug(areaOfLaw.Slug);
        var citySlug = ToSlug(location);

        return BuildRelativeUri($"{areaSlug}+{citySlug}.html");
    }

    public Uri BuildLocationSuggestionUri(string query)
    {
        var builder = new UriBuilder(BuildRelativeUri(_options.AutocompletePath));
        var escapedQuery = Uri.EscapeDataString(query.Trim());

        builder.Query = $"ajax=1&q={escapedQuery}";

        return builder.Uri;
    }

    public Uri BuildProfileUri(string slugOrUrl)
    {
        if (string.IsNullOrWhiteSpace(slugOrUrl))
        {
            throw new ArgumentException("Profile slug or URL is required.", nameof(slugOrUrl));
        }

        var trimmed = slugOrUrl.Trim();

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri))
        {
            EnsureSolicitorsComHost(absoluteUri);

            return absoluteUri;
        }

        var path = trimmed.TrimStart('/');

        if (!path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            path = $"{path}.html";
        }

        return BuildRelativeUri(path);
    }

    private Uri BuildRelativeUri(string path)
    {
        var normalizedPath = path.TrimStart('/');
        var uri = new Uri(_baseUri, normalizedPath);

        EnsureSolicitorsComHost(uri);

        return uri;
    }

    private void EnsureSolicitorsComHost(Uri uri)
    {
        if (!string.Equals(uri.Scheme, _baseUri.Scheme, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, _baseUri.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only configured solicitors.com URLs can be requested.");
        }
    }

    private static string ToSlug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A value is required to build a solicitors.com URL.", nameof(value));
        }

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
