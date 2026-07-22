using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Ports;

public interface ISolicitorProfileGateway
{
    Task<SolicitorProfile?> GetProfileAsync(string slug, CancellationToken cancellationToken);
}
