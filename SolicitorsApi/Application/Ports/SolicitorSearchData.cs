using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Ports;

public class SolicitorSearchData
{
    public IReadOnlyList<Solicitor> Solicitors { get; init; } = [];

    public IReadOnlyList<LocationSearchResult> LocationResults { get; init; } = [];

    public IReadOnlyList<ScrapeFailure> Failures { get; init; } = [];
}
