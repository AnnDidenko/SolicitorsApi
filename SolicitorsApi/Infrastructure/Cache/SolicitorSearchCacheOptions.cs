namespace SolicitorsApi.Infrastructure.Cache;

public sealed class SolicitorSearchCacheOptions
{
    public const string SectionName = "SolicitorSearchCache";

    public bool Enabled { get; init; } = true;

    public int ListTimeToLiveHours { get; init; } = 24;

    public int ProfileTimeToLiveHours { get; init; } = 24;

    public int MaxEntries { get; init; } = 1000;

    public TimeSpan ListTimeToLive => TimeSpan.FromHours(ListTimeToLiveHours);

    public TimeSpan ProfileTimeToLive => TimeSpan.FromHours(ProfileTimeToLiveHours);
}
