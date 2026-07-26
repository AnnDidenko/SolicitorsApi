using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using SolicitorsApi.Application.Cache;
using SolicitorsApi.Application.Ports;

namespace SolicitorsApi.Infrastructure.Cache;

public sealed class MemorySolicitorSearchCache : ISolicitorSearchCache
{
    private readonly ConcurrentDictionary<string, SolicitorListCacheEntry> _segments = new(StringComparer.Ordinal);
    private readonly SolicitorListCacheKeyBuilder _keyBuilder = new();
    private readonly SolicitorSearchCacheOptions _options;

    public MemorySolicitorSearchCache(IOptions<SolicitorSearchCacheOptions> options)
    {
        _options = options.Value;
    }

    public Task<SolicitorListCacheEntry?> GetListSegmentAsync(
        string location,
        string? areaOfLawSlug,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return Task.FromResult<SolicitorListCacheEntry?>(null);
        }

        var key = BuildKey(location, areaOfLawSlug);

        if (!_segments.TryGetValue(key, out var entry))
        {
            return Task.FromResult<SolicitorListCacheEntry?>(null);
        }

        if (entry.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _segments.TryRemove(key, out _);

            return Task.FromResult<SolicitorListCacheEntry?>(null);
        }

        return Task.FromResult<SolicitorListCacheEntry?>(entry);
    }

    public Task StoreListSegmentAsync(
        SolicitorListCacheEntry entry,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return Task.CompletedTask;
        }

        var fetchedAt = entry.FetchedAt == default ? DateTimeOffset.UtcNow : entry.FetchedAt;
        var cacheEntry = entry with
        {
            FetchedAt = fetchedAt,
            ExpiresAt = fetchedAt.Add(_options.ListTimeToLive)
        };

        _segments[BuildKey(entry.Location, entry.AreaOfLawSlug)] = cacheEntry;
        PruneIfNeeded();

        return Task.CompletedTask;
    }

    private string BuildKey(string location, string? areaOfLawSlug)
    {
        return _keyBuilder.Build(
            location,
            string.IsNullOrWhiteSpace(areaOfLawSlug)
                ? null
                : new SolicitorsApi.Domain.AreaOfLaw { Slug = areaOfLawSlug });
    }

    private void PruneIfNeeded()
    {
        var maxEntries = Math.Max(1, _options.MaxEntries);

        if (_segments.Count <= maxEntries)
        {
            return;
        }

        foreach (var key in _segments
            .OrderBy(pair => pair.Value.ExpiresAt)
            .Take(_segments.Count - maxEntries)
            .Select(pair => pair.Key))
        {
            _segments.TryRemove(key, out _);
        }
    }
}
