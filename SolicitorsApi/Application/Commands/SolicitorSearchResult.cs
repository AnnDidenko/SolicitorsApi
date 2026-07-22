using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Commands;

public class SolicitorSearchResult
{
    public DateTimeOffset SearchedAt { get; init; }

    public IReadOnlyList<string> Locations { get; init; } = [];

    public bool UsedDefaultLocations { get; init; }

    public AreaOfLaw? AreaOfLaw { get; init; }

    public SolicitorSearchFilters Filters { get; init; } = new();

    public SolicitorSearchSort Sort { get; init; } = new();

    public PagedSearchResult<Solicitor> Paging { get; init; } = new();

    public IReadOnlyList<LocationSearchResult> LocationResults { get; init; } = [];

    public SolicitorSearchReport Report { get; init; } = new();

    public IReadOnlyList<ScrapeFailure> Failures { get; init; } = [];
}
