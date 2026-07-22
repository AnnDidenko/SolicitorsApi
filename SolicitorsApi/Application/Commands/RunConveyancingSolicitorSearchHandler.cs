using SolicitorsApi.Application.Search;

namespace SolicitorsApi.Application.Commands;

public class RunConveyancingSolicitorSearchHandler
{
    private readonly ISolicitorSearchRequestValidator _validator;
    private readonly ISolicitorSearchRequestNormalizer _normalizer;
    private readonly ISolicitorSearchScrapeService _scrapeService;
    private readonly ISolicitorSearchProfileEnricher _profileEnricher;
    private readonly ISolicitorSearchFilter _searchFilter;
    private readonly ISolicitorSearchSorter _searchSorter;
    private readonly ISolicitorSearchResultFactory _resultFactory;

    public RunConveyancingSolicitorSearchHandler(
        ISolicitorSearchRequestValidator validator,
        ISolicitorSearchRequestNormalizer normalizer,
        ISolicitorSearchScrapeService scrapeService,
        ISolicitorSearchProfileEnricher profileEnricher,
        ISolicitorSearchFilter searchFilter,
        ISolicitorSearchSorter searchSorter,
        ISolicitorSearchResultFactory resultFactory)
    {
        _validator = validator;
        _normalizer = normalizer;
        _scrapeService = scrapeService;
        _profileEnricher = profileEnricher;
        _searchFilter = searchFilter;
        _searchSorter = searchSorter;
        _resultFactory = resultFactory;
    }

    public async Task<ApplicationResult<SolicitorSearchResult>> HandleAsync(
        RunConveyancingSolicitorSearchCommand command,
        CancellationToken cancellationToken)
    {
        var context = await _normalizer.NormalizeAsync(command, cancellationToken);
        var validationErrors = await _validator.ValidateAsync(command, context, cancellationToken);

        if (validationErrors.Count > 0)
        {
            return ApplicationResult<SolicitorSearchResult>.Validation(validationErrors);
        }

        var searchData = await _scrapeService.SearchAsync(context, cancellationToken);

        if (!searchData.IsSuccess)
        {
            return ApplicationResult<SolicitorSearchResult>.FailedDependency(searchData.Errors);
        }

        var solicitors = searchData.Value!.Solicitors;
        var enrichedSolicitors = await _profileEnricher.EnrichIfRequiredAsync(
            solicitors,
            command,
            context.Sort,
            cancellationToken);

        if (!enrichedSolicitors.IsSuccess)
        {
            return ApplicationResult<SolicitorSearchResult>.FailedDependency(enrichedSolicitors.Errors);
        }

        solicitors = _searchFilter.Apply(enrichedSolicitors.Value!, command.MinimumReviewScore);
        solicitors = _searchSorter.Apply(solicitors, context.Sort);

        return ApplicationResult<SolicitorSearchResult>.Ok(
            _resultFactory.Create(command, context, solicitors, searchData.Value));
    }
}
