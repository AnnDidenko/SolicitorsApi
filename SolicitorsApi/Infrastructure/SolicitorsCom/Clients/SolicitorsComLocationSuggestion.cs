using System.Text.Json.Serialization;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Clients;

internal class SolicitorsComLocationSuggestion
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
