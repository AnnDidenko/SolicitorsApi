namespace SolicitorsApi.Api.Contracts;

public sealed record SolicitorSearchReport
{
    public int TotalSolicitors { get; init; }

    public IReadOnlyDictionary<string, int> CountsByLocation { get; init; } = new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> CountsByAreaOfLaw { get; init; } = new Dictionary<string, int>();

    public IReadOnlyList<string> LocationsWithNoResults { get; init; } = [];

    public IReadOnlyDictionary<string, int> ContactCompleteness { get; init; } = new Dictionary<string, int>();

    public ReviewScoreSummary ReviewScoreSummary { get; init; } = new();
}
