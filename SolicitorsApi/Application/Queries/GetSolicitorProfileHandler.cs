using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Queries;

public class GetSolicitorProfileHandler
{
    private readonly ISolicitorProfileService _solicitorProfileService;

    public GetSolicitorProfileHandler(ISolicitorProfileService solicitorProfileService)
    {
        _solicitorProfileService = solicitorProfileService;
    }

    public async Task<ApplicationResult<SolicitorProfile>> HandleAsync(
        GetSolicitorProfileQuery query,
        CancellationToken cancellationToken)
    {
        return await _solicitorProfileService.GetAsync(query.Slug, cancellationToken);
    }
}
