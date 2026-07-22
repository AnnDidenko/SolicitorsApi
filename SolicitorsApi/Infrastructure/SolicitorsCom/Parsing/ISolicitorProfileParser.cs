using SolicitorsApi.Domain;

namespace SolicitorsApi.Infrastructure.SolicitorsCom.Parsing;

public interface ISolicitorProfileParser
{
    SolicitorProfile Parse(string html, Uri sourceUri);
}
