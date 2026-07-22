using Microsoft.Extensions.Options;
using SolicitorsApi.Application.Ports;
using SolicitorsApi.Application.Search;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Application.Commands;

public class SolicitorSearchProfileEnricher : ISolicitorSearchProfileEnricher
{
    private readonly ISolicitorSearchGateway _solicitorSearchGateway;
    private readonly ISolicitorSearchSorter _searchSorter;
    private readonly SolicitorSearchSettings _settings;

    public SolicitorSearchProfileEnricher(
        ISolicitorSearchGateway solicitorSearchGateway,
        ISolicitorSearchSorter searchSorter,
        IOptions<SolicitorSearchSettings> options)
    {
        _solicitorSearchGateway = solicitorSearchGateway;
        _searchSorter = searchSorter;
        _settings = options.Value;
    }

    public async Task<ApplicationResult<IReadOnlyList<Solicitor>>> EnrichIfRequiredAsync(
        IReadOnlyList<Solicitor> solicitors,
        RunConveyancingSolicitorSearchCommand command,
        SolicitorSearchSort sort,
        CancellationToken cancellationToken)
    {
        if (!RequiresProfileReviewData(command, sort))
        {
            return ApplicationResult<IReadOnlyList<Solicitor>>.Ok(solicitors);
        }

        var profiles = await GetProfilesAsync(solicitors, cancellationToken);

        return profiles.IsSuccess
            ? ApplicationResult<IReadOnlyList<Solicitor>>.Ok(
                EnrichSolicitors(solicitors, profiles.Value ?? new Dictionary<string, SolicitorProfile>()))
            : ApplicationResult<IReadOnlyList<Solicitor>>.FailedDependency(profiles.Errors);
    }

    private bool RequiresProfileReviewData(
        RunConveyancingSolicitorSearchCommand command,
        SolicitorSearchSort sort)
    {
        return command.MinimumReviewScore.HasValue ||
            _searchSorter.RequiresProfileReviewData(sort);
    }

    private async Task<ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>>> GetProfilesAsync(
        IReadOnlyList<Solicitor> solicitors,
        CancellationToken cancellationToken)
    {
        try
        {
            var profiles = await _solicitorSearchGateway.GetProfilesAsync(
                solicitors,
                _settings.ProfileFetchConcurrency,
                cancellationToken);

            return ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>>.Ok(profiles);
        }
        catch (Exception exception) when (
            exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            return ApplicationResult<IReadOnlyDictionary<string, SolicitorProfile>>.FailedDependency(
                [new ApplicationError(
                    "profileEnrichmentFailed",
                    "Solicitor profile details could not be loaded. Please try again later.")]);
        }
    }

    private static IReadOnlyList<Solicitor> EnrichSolicitors(
        IReadOnlyList<Solicitor> solicitors,
        IReadOnlyDictionary<string, SolicitorProfile> profiles)
    {
        return solicitors.Select(solicitor =>
        {
            if (string.IsNullOrWhiteSpace(solicitor.ProfileSlug) ||
                !profiles.TryGetValue(solicitor.ProfileSlug, out var profile))
            {
                return solicitor;
            }

            return solicitor with
            {
                ContactDetails = profile.ContactDetails,
                AreasOfLaw = profile.AreasOfLaw,
                Review = profile.Review
            };
        }).ToArray();
    }
}
