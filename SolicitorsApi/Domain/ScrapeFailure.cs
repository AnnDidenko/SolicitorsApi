namespace SolicitorsApi.Domain;

public sealed record ScrapeFailure
{
    public string? Location { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}
