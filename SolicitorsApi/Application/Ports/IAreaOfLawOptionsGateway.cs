using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Ports;

public interface IAreaOfLawOptionsGateway
{
    Task<IReadOnlyList<AreaOfLaw>> GetAreaOfLawOptionsAsync(CancellationToken cancellationToken);
}
