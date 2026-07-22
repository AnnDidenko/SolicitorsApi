namespace SolicitorsApi.Api.Contracts;

public sealed record ReviewSummary
{
    public decimal? Score { get; init; }

    public int? Count { get; init; }
}
