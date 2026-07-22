using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Queries;

public interface IAreaOfLawOptionsProvider
{
    Task<IReadOnlyList<AreaOfLaw>> GetAsync(CancellationToken cancellationToken);
}
