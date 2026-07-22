namespace SolicitorsApi.Api.Contracts;

public sealed record LocationSuggestion
{
    public string Title { get; init; } = "";

    public string Text { get; init; } = "";
}
