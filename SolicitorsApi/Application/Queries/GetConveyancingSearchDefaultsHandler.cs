namespace SolicitorsApi.Application.Queries;

public class GetConveyancingSearchDefaultsHandler
{
    private readonly IConveyancingSearchDefaultsService _defaultsService;

    public GetConveyancingSearchDefaultsHandler(IConveyancingSearchDefaultsService defaultsService)
    {
        _defaultsService = defaultsService;
    }

    public Task<ApplicationResult<ConveyancingSearchDefaults>> HandleAsync(
        GetConveyancingSearchDefaultsQuery query,
        CancellationToken cancellationToken)
    {
        return _defaultsService.GetAsync(cancellationToken);
    }
}
