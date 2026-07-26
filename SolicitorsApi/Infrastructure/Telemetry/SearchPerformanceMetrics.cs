using System.Diagnostics.Metrics;
using SolicitorsApi.Application.Ports;

namespace SolicitorsApi.Infrastructure.Telemetry;

public sealed class SearchPerformanceMetrics : ISearchPerformanceMetrics
{
    public const string MeterName = "SolicitorsApi.Search";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "solicitors_api_request_duration_ms",
        "ms");
    private static readonly Histogram<double> SearchDuration = Meter.CreateHistogram<double>(
        "solicitor_search_duration_ms",
        "ms");
    private static readonly Histogram<double> ListFetchDuration = Meter.CreateHistogram<double>(
        "solicitor_search_list_fetch_duration_ms",
        "ms");
    private static readonly Histogram<double> ProfileEnrichmentDuration = Meter.CreateHistogram<double>(
        "solicitor_search_profile_enrichment_duration_ms",
        "ms");
    private static readonly Counter<long> ListFetchCount = Meter.CreateCounter<long>(
        "solicitor_search_list_fetch_count");
    private static readonly Counter<long> ProfileFetchCount = Meter.CreateCounter<long>(
        "solicitor_search_profile_fetch_count");
    private static readonly Counter<long> ProfileCacheHitCount = Meter.CreateCounter<long>(
        "solicitor_search_profile_cache_hit_count");
    private static readonly Counter<long> ProfileCacheMissCount = Meter.CreateCounter<long>(
        "solicitor_search_profile_cache_miss_count");
    private static readonly Counter<long> FallbackCount = Meter.CreateCounter<long>(
        "solicitor_search_fallback_count");

    private readonly ILogger<SearchPerformanceMetrics> _logger;

    public SearchPerformanceMetrics(ILogger<SearchPerformanceMetrics> logger)
    {
        _logger = logger;
    }

    public void RecordRequest(
        string route,
        int statusCode,
        string failureCategory,
        TimeSpan elapsed)
    {
        RequestDuration.Record(
            elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("route", route),
            new KeyValuePair<string, object?>("status_code", statusCode),
            new KeyValuePair<string, object?>("failure_category", failureCategory));

        _logger.LogInformation(
            "API request completed. Route: {Route}; StatusCode: {StatusCode}; FailureCategory: {FailureCategory}; DurationMs: {DurationMs}",
            route,
            statusCode,
            failureCategory,
            elapsed.TotalMilliseconds);
    }

    public void RecordSearch(
        string status,
        TimeSpan elapsed)
    {
        SearchDuration.Record(
            elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("status", status));

        _logger.LogInformation(
            "Solicitor search completed. Status: {Status}; DurationMs: {DurationMs}",
            status,
            elapsed.TotalMilliseconds);
    }

    public void RecordListFetch(
        int count,
        string status,
        TimeSpan elapsed)
    {
        ListFetchCount.Add(
            count,
            new KeyValuePair<string, object?>("status", status));
        ListFetchDuration.Record(
            elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("status", status));

        _logger.LogInformation(
            "Solicitor list fetch completed. Status: {Status}; FetchCount: {FetchCount}; DurationMs: {DurationMs}",
            status,
            count,
            elapsed.TotalMilliseconds);
    }

    public void RecordProfileEnrichment(
        int fetchCount,
        int cacheHitCount,
        int cacheMissCount,
        string status,
        TimeSpan elapsed)
    {
        ProfileFetchCount.Add(
            fetchCount,
            new KeyValuePair<string, object?>("status", status));
        ProfileCacheHitCount.Add(cacheHitCount);
        ProfileCacheMissCount.Add(cacheMissCount);
        ProfileEnrichmentDuration.Record(
            elapsed.TotalMilliseconds,
            new KeyValuePair<string, object?>("status", status));

        _logger.LogInformation(
            "Solicitor profile enrichment completed. Status: {Status}; FetchCount: {FetchCount}; CacheHits: {CacheHits}; CacheMisses: {CacheMisses}; DurationMs: {DurationMs}",
            status,
            fetchCount,
            cacheHitCount,
            cacheMissCount,
            elapsed.TotalMilliseconds);
    }

    public void RecordFallback(
        string stage,
        string result)
    {
        FallbackCount.Add(
            1,
            new KeyValuePair<string, object?>("stage", stage),
            new KeyValuePair<string, object?>("result", result));

        _logger.LogInformation(
            "Solicitor search fallback recorded. Stage: {Stage}; Result: {Result}",
            stage,
            result);
    }
}
