using SolicitorsApi.Application.Ports;
using SolicitorsApi.Application.Reports;
using SolicitorsApi.Application.Search;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Commands;

public class SolicitorSearchResultFactory : ISolicitorSearchResultFactory
{
    private readonly ISolicitorSearchPager _searchPager;
    private readonly ISolicitorSearchReportBuilder _reportBuilder;

    public SolicitorSearchResultFactory(
        ISolicitorSearchPager searchPager,
        ISolicitorSearchReportBuilder reportBuilder)
    {
        _searchPager = searchPager;
        _reportBuilder = reportBuilder;
    }

    public SolicitorSearchResult Create(
        RunConveyancingSolicitorSearchCommand command,
        SolicitorSearchExecutionContext context,
        IReadOnlyList<Solicitor> solicitors,
        SolicitorSearchData searchData,
        SolicitorSearchCacheMetadata? cacheMetadata = null)
    {
        var searchedAt = cacheMetadata?.ServedAt ?? DateTimeOffset.UtcNow;

        return new SolicitorSearchResult
        {
            SearchedAt = searchedAt,
            Locations = context.Locations,
            UsedDefaultLocations = context.UsedDefaultLocations,
            AreaOfLaw = context.AreaOfLaw,
            Filters = new SolicitorSearchFilters
            {
                AreaOfLaw = context.AreaOfLaw,
                MinimumReviewScore = command.MinimumReviewScore
            },
            Sort = context.Sort,
            Paging = _searchPager.Apply(
                solicitors,
                command.Paging.Page,
                command.Paging.PageSize),
            LocationResults = searchData.LocationResults
                .Concat(context.NonBlockingFailures.Select(failure => new LocationSearchResult
                {
                    Location = failure.Location ?? string.Empty,
                    Count = 0
                }))
                .ToArray(),
            Report = _reportBuilder.Build(solicitors, searchData.LocationResults),
            Failures = context.NonBlockingFailures.Concat(searchData.Failures).ToArray(),
            Cache = cacheMetadata
        };
    }
}
