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
        SolicitorSearchData searchData)
    {
        return new SolicitorSearchResult
        {
            SearchedAt = DateTimeOffset.UtcNow,
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
            LocationResults = searchData.LocationResults,
            Report = _reportBuilder.Build(solicitors, searchData.LocationResults),
            Failures = searchData.Failures
        };
    }
}
