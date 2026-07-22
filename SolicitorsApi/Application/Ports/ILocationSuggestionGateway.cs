using SolicitorsApi.Application.Queries;

namespace SolicitorsApi.Application.Ports;

public interface ILocationSuggestionGateway
{
    Task<IReadOnlyList<LocationSuggestionResult>> GetSuggestionsAsync(
        string query,
        CancellationToken cancellationToken);
}
