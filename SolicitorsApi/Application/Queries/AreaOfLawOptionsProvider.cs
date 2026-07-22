using SolicitorsApi.Application.Ports;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Queries;

public class AreaOfLawOptionsProvider : IAreaOfLawOptionsProvider
{
    private readonly IAreaOfLawOptionsGateway _gateway;

    public AreaOfLawOptionsProvider(IAreaOfLawOptionsGateway gateway)
    {
        _gateway = gateway;
    }

    public Task<IReadOnlyList<AreaOfLaw>> GetAsync(CancellationToken cancellationToken)
    {
        return _gateway.GetAreaOfLawOptionsAsync(cancellationToken);
    }
}
