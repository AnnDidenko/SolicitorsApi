namespace SolicitorsApi.Application.Queries;

public interface IConveyancingSearchDefaultsService
{
    Task<ApplicationResult<ConveyancingSearchDefaults>> GetAsync(CancellationToken cancellationToken);
}
