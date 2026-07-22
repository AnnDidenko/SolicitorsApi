namespace SolicitorsApi.Api.Contracts;

public sealed record ScrapeFailure
{
    public string? Location { get; init; }

    public string Code { get; init; } = "";

    public string Message { get; init; } = "";
}
