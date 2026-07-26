namespace SolicitorsApi.Application.Ports;

public sealed class NoOpSearchPerformanceMetrics : ISearchPerformanceMetrics
{
    public static NoOpSearchPerformanceMetrics Instance { get; } = new();

    private NoOpSearchPerformanceMetrics()
    {
    }

    public void RecordRequest(
        string route,
        int statusCode,
        string failureCategory,
        TimeSpan elapsed)
    {
    }

    public void RecordSearch(
        string status,
        TimeSpan elapsed)
    {
    }

    public void RecordListFetch(
        int count,
        string status,
        TimeSpan elapsed)
    {
    }

    public void RecordProfileEnrichment(
        int fetchCount,
        int cacheHitCount,
        int cacheMissCount,
        string status,
        TimeSpan elapsed)
    {
    }

    public void RecordFallback(
        string stage,
        string result)
    {
    }
}
