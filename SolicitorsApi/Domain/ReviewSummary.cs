namespace SolicitorsApi.Domain;

public sealed record ReviewSummary
{
    public decimal? Score { get; init; }

    public int? Count { get; init; }
}
