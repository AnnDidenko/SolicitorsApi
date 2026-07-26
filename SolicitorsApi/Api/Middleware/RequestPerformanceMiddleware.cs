using System.Diagnostics;
using SolicitorsApi.Application.Ports;

namespace SolicitorsApi.Api.Middleware;

public sealed class RequestPerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISearchPerformanceMetrics _metrics;

    public RequestPerformanceMiddleware(
        RequestDelegate next,
        ISearchPerformanceMetrics metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        catch
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordRequest(
                GetRoute(context),
                context.Response.StatusCode,
                GetFailureCategory(context.Response.StatusCode),
                stopwatch.Elapsed);
        }
    }

    private static string GetRoute(HttpContext context)
    {
        return context.GetEndpoint() is { } endpoint
            ? endpoint.DisplayName ?? context.Request.Path.Value ?? string.Empty
            : context.Request.Path.Value ?? string.Empty;
    }

    private static string GetFailureCategory(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 400 => "none",
            StatusCodes.Status400BadRequest => "validation",
            StatusCodes.Status404NotFound => "notFound",
            StatusCodes.Status424FailedDependency => "failedDependency",
            >= 500 => "serverError",
            _ => "other"
        };
    }
}
