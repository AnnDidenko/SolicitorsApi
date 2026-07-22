using System.Text.Json;
using SolicitorsApi.Application.Ports;
using SolicitorsApi.Application.Queries;
using SolicitorsApi.Infrastructure.SolicitorsCom.Routing;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Clients;

public class SolicitorsComLocationSuggestionClient : ILocationSuggestionGateway
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly SolicitorsComUrlBuilder _urlBuilder;

    public SolicitorsComLocationSuggestionClient(
        HttpClient httpClient,
        SolicitorsComUrlBuilder urlBuilder)
    {
        _httpClient = httpClient;
        _urlBuilder = urlBuilder;
    }

    public async Task<IReadOnlyList<LocationSuggestionResult>> GetSuggestionsAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var requestUri = _urlBuilder.BuildLocationSuggestionUri(query);
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var suggestions = await JsonSerializer.DeserializeAsync<IReadOnlyList<SolicitorsComLocationSuggestion>>(
            stream,
            JsonSerializerOptions,
            cancellationToken);

        return suggestions?
            .Select(suggestion => new LocationSuggestionResult
            {
                Title = suggestion.Title ?? string.Empty,
                Text = suggestion.Text ?? string.Empty
            })
            .Where(suggestion => !string.IsNullOrWhiteSpace(suggestion.Title))
            .ToArray() ?? [];
    }
}
