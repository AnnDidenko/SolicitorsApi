namespace SolicitorsApi.Domain;

public sealed record SolicitorSearchFilters
{
    public AreaOfLaw? AreaOfLaw { get; init; }

    public decimal? MinimumReviewScore { get; init; }
}
