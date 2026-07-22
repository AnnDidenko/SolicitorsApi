using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Ports;

public interface ISolicitorSearchGateway
{
    Task<SolicitorSearchData> SearchAsync(
        IReadOnlyList<string> locations,
        AreaOfLaw? areaOfLaw,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, SolicitorProfile>> GetProfilesAsync(
        IReadOnlyList<Solicitor> solicitors,
        int maxConcurrency,
        CancellationToken cancellationToken);
}
