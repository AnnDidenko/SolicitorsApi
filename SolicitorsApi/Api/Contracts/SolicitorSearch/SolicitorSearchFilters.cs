namespace SolicitorsApi.Api.Contracts;

public sealed record SolicitorSearchFilters
{
    public decimal? MinimumReviewScore { get; init; }
}
