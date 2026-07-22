using SolicitorsApi.Application.Ports;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Queries;

public class SolicitorProfileService : ISolicitorProfileService
{
    private readonly ISolicitorProfileGateway _solicitorProfileGateway;

    public SolicitorProfileService(ISolicitorProfileGateway solicitorProfileGateway)
    {
        _solicitorProfileGateway = solicitorProfileGateway;
    }

    public async Task<ApplicationResult<SolicitorProfile>> GetAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var profile = await _solicitorProfileGateway.GetProfileAsync(slug.Trim(), cancellationToken);

        return profile is null
            ? ApplicationResult<SolicitorProfile>.NotFound("Solicitor profile was not found.")
            : ApplicationResult<SolicitorProfile>.Ok(profile);
    }
}
