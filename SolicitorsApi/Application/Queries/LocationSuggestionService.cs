using SolicitorsApi.Application.Ports;

namespace SolicitorsApi.Application.Queries;

public class LocationSuggestionService : ILocationSuggestionService
{
    private readonly ILocationSuggestionGateway _locationSuggestionGateway;

    public LocationSuggestionService(ILocationSuggestionGateway locationSuggestionGateway)
    {
        _locationSuggestionGateway = locationSuggestionGateway;
    }

    public async Task<ApplicationResult<IReadOnlyList<LocationSuggestionResult>>> GetAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var suggestions = await _locationSuggestionGateway.GetSuggestionsAsync(query.Trim(), cancellationToken);

        return ApplicationResult<IReadOnlyList<LocationSuggestionResult>>.Ok(suggestions);
    }
}
