namespace SolicitorsApi.Application.Queries;

public interface ILocationSuggestionService
{
    Task<ApplicationResult<IReadOnlyList<LocationSuggestionResult>>> GetAsync(
        string query,
        CancellationToken cancellationToken);
}
