using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Cache;

public sealed class SolicitorIdentityKeyBuilder
{
    public string Build(Solicitor solicitor)
    {
        if (!string.IsNullOrWhiteSpace(solicitor.ProfileSlug))
        {
            return $"slug:{NormalizePart(solicitor.ProfileSlug)}";
        }

        if (!string.IsNullOrWhiteSpace(solicitor.ProfileUrl))
        {
            return $"url:{NormalizeProfileUrl(solicitor.ProfileUrl)}";
        }

        return string.Join(
            "|",
            "fallback",
            NormalizePart(solicitor.Name),
            NormalizePart(solicitor.ContactDetails.WebsiteUrl),
            NormalizePart(solicitor.ContactDetails.Phone),
            NormalizePart(solicitor.Location));
    }

    private static string NormalizePart(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(" ", value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                .ToLowerInvariant();
    }

    private static string NormalizeProfileUrl(string? value)
    {
        var normalized = NormalizePart(value);

        if (!Uri.TryCreate(normalized, UriKind.RelativeOrAbsolute, out var uri))
        {
            return normalized;
        }

        if (!uri.IsAbsoluteUri)
        {
            return normalized.StartsWith('/')
                ? normalized
                : $"/{normalized}";
        }

        return string.IsNullOrWhiteSpace(uri.Query)
            ? uri.AbsolutePath.ToLowerInvariant()
            : $"{uri.AbsolutePath}{uri.Query}".ToLowerInvariant();
    }
}
