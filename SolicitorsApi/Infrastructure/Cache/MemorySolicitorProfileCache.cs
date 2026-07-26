using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using SolicitorsApi.Application.Cache;
using SolicitorsApi.Application.Ports;

namespace SolicitorsApi.Infrastructure.Cache;

public sealed class MemorySolicitorProfileCache : ISolicitorProfileCache
{
    private readonly ConcurrentDictionary<string, SolicitorProfileCacheRecord> _records = new(StringComparer.Ordinal);
    private readonly SolicitorSearchCacheOptions _options;

    public MemorySolicitorProfileCache(IOptions<SolicitorSearchCacheOptions> options)
    {
        _options = options.Value;
    }

    public Task<IReadOnlyDictionary<string, SolicitorProfileCacheRecord>> GetBySourceIdentitiesAsync(
        IReadOnlyCollection<string> sourceIdentities,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return Task.FromResult<IReadOnlyDictionary<string, SolicitorProfileCacheRecord>>(
                new Dictionary<string, SolicitorProfileCacheRecord>());
        }

        var now = DateTimeOffset.UtcNow;
        var records = new Dictionary<string, SolicitorProfileCacheRecord>(StringComparer.Ordinal);

        foreach (var sourceIdentity in sourceIdentities)
        {
            if (!_records.TryGetValue(sourceIdentity, out var record))
            {
                continue;
            }

            if (record.ExpiresAt <= now)
            {
                _records.TryRemove(sourceIdentity, out _);

                continue;
            }

            records[sourceIdentity] = record;
        }

        return Task.FromResult<IReadOnlyDictionary<string, SolicitorProfileCacheRecord>>(records);
    }

    public Task UpsertDiscoveredSolicitorsAsync(
        IReadOnlyList<SolicitorProfileCacheRecord> records,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return Task.CompletedTask;
        }

        foreach (var record in records)
        {
            _records.AddOrUpdate(
                record.SourceIdentity,
                NormalizeDiscoveredRecord(record),
                (_, existing) => NormalizeDiscoveredRecord(record) with
                {
                    Profile = existing.Profile,
                    ProfileFetchedAt = existing.ProfileFetchedAt
                });
        }

        PruneIfNeeded();

        return Task.CompletedTask;
    }

    public Task UpsertProfileDetailsAsync(
        SolicitorProfileCacheRecord record,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return Task.CompletedTask;
        }

        _records[record.SourceIdentity] = NormalizeProfileRecord(record);
        PruneIfNeeded();

        return Task.CompletedTask;
    }

    private SolicitorProfileCacheRecord NormalizeDiscoveredRecord(SolicitorProfileCacheRecord record)
    {
        var lastSeenAt = record.LastSeenAt == default ? DateTimeOffset.UtcNow : record.LastSeenAt;

        return record with
        {
            LastSeenAt = lastSeenAt,
            ExpiresAt = lastSeenAt.Add(_options.ProfileTimeToLive)
        };
    }

    private SolicitorProfileCacheRecord NormalizeProfileRecord(SolicitorProfileCacheRecord record)
    {
        var fetchedAt = record.ProfileFetchedAt ?? DateTimeOffset.UtcNow;

        return record with
        {
            LastSeenAt = record.LastSeenAt == default ? fetchedAt : record.LastSeenAt,
            ProfileFetchedAt = fetchedAt,
            ExpiresAt = fetchedAt.Add(_options.ProfileTimeToLive)
        };
    }

    private void PruneIfNeeded()
    {
        var maxEntries = Math.Max(1, _options.MaxEntries);

        if (_records.Count <= maxEntries)
        {
            return;
        }

        foreach (var key in _records
            .OrderBy(pair => pair.Value.ExpiresAt)
            .Take(_records.Count - maxEntries)
            .Select(pair => pair.Key))
        {
            _records.TryRemove(key, out _);
        }
    }
}
