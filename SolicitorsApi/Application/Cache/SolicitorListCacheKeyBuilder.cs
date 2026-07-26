using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Cache;

public sealed class SolicitorListCacheKeyBuilder
{
    public string Build(string location, AreaOfLaw? areaOfLaw)
    {
        return string.Join(
            "|",
            NormalizePart(location),
            NormalizePart(areaOfLaw?.Slug));
    }

    public IReadOnlyList<string> BuildMany(
        IReadOnlyList<string> locations,
        AreaOfLaw? areaOfLaw)
    {
        return locations
            .Select(location => Build(location, areaOfLaw))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string NormalizePart(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant();
    }
}
