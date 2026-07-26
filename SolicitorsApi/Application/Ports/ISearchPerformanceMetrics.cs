namespace SolicitorsApi.Application.Ports;

public interface ISearchPerformanceMetrics
{
    void RecordRequest(
        string route,
        int statusCode,
        string failureCategory,
        TimeSpan elapsed);

    void RecordSearch(
        string status,
        TimeSpan elapsed);

    void RecordListFetch(
        int count,
        string status,
        TimeSpan elapsed);

    void RecordProfileEnrichment(
        int fetchCount,
        int cacheHitCount,
        int cacheMissCount,
        string status,
        TimeSpan elapsed);

    void RecordFallback(
        string stage,
        string result);
}
