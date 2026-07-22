using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Commands;

public class RunConveyancingSolicitorSearchCommand
{
    public IReadOnlyList<string>? Locations { get; init; }

    public string? AreaOfLaw { get; init; }

    public decimal? MinimumReviewScore { get; init; }

    public SolicitorSearchSortRequest? Sort { get; init; }

    public PagedSearchRequest Paging { get; init; } = new();
}
