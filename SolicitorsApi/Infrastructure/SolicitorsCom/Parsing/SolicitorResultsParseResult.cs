using SolicitorsApi.Domain;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Parsing;

public class SolicitorResultsParseResult
{
    public IReadOnlyList<Solicitor> Solicitors { get; init; } = [];

    public IReadOnlyList<AreaOfLaw> AreaOfLawOptions { get; init; } = [];
}
