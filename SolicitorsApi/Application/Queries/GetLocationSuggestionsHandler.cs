namespace SolicitorsApi.Application.Queries;

public class GetLocationSuggestionsHandler
{
    private readonly ILocationSuggestionService _locationSuggestionService;

    public GetLocationSuggestionsHandler(ILocationSuggestionService locationSuggestionService)
    {
        _locationSuggestionService = locationSuggestionService;
    }

    public async Task<ApplicationResult<IReadOnlyList<LocationSuggestionResult>>> HandleAsync(
        GetLocationSuggestionsQuery query,
        CancellationToken cancellationToken)
    {
        return await _locationSuggestionService.GetAsync(query.Query, cancellationToken);
    }
}
