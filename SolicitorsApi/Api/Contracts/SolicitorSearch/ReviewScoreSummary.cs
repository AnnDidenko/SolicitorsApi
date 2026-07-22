namespace SolicitorsApi.Api.Contracts;

public sealed record ReviewScoreSummary
{
    public decimal? Minimum { get; init; }

    public decimal? Maximum { get; init; }

    public decimal? Average { get; init; }
}
