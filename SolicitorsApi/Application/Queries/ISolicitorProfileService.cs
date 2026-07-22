using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Queries;

public interface ISolicitorProfileService
{
    Task<ApplicationResult<SolicitorProfile>> GetAsync(
        string slug,
        CancellationToken cancellationToken);
}
