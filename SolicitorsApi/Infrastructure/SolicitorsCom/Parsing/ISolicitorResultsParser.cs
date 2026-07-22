using SolicitorsApi.Domain;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Parsing;

public interface ISolicitorResultsParser
{
    SolicitorResultsParseResult Parse(string html, Uri sourceUri, string? location = null);

    IReadOnlyList<AreaOfLaw> ParseAreaOfLawOptions(string html);
}
