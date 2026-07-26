using SolicitorsApi.Api.Contracts;
using SolicitorsApi.Application.Commands;
using Domain = SolicitorsApi.Domain;

namespace SolicitorsApi.Api.Mappers;

internal static class SolicitorSearchResponseMappingExtensions
{
    public static SolicitorSearchResponse ToResponse(this SolicitorSearchResult result)
    {
        return new SolicitorSearchResponse
        {
            SearchedAt = result.SearchedAt,
            Locations = result.Locations,
            AreaOfLaw = result.AreaOfLaw?.Name,
            Filters = new SolicitorSearchFilters
            {
                MinimumReviewScore = result.Filters.MinimumReviewScore
            },
            Sort = result.Sort.ToResponse(),
            Page = result.Paging.Page,
            PageSize = result.Paging.PageSize,
            TotalCount = result.Paging.TotalCount,
            Solicitors = result.Paging.Items.Select(ToResponse).ToArray(),
            LocationResults = result.LocationResults.Select(ToResponse).ToArray(),
            Report = result.Report.ToResponse(),
            Failures = result.Failures.Select(ToResponse).ToArray(),
            Cache = result.Cache?.ToResponse()
        };
    }

    private static SolicitorSearchCacheInfo ToResponse(this SolicitorSearchCacheMetadata metadata)
    {
        return new SolicitorSearchCacheInfo
        {
            Status = metadata.Status.ToString(),
            UsedFallback = metadata.UsedFallback,
            FetchedAt = metadata.FetchedAt,
            ServedAt = metadata.ServedAt,
            ExpiresAt = metadata.ExpiresAt
        };
    }

    private static SortOption ToResponse(this Domain.SolicitorSearchSort sort)
    {
        return new SortOption
        {
            Field = sort.Field.ToString(),
            Direction = sort.Direction.ToString()
        };
    }

    private static SolicitorSearchResultItem ToResponse(this Domain.Solicitor solicitor)
    {
        return new SolicitorSearchResultItem
        {
            Name = solicitor.Name,
            Location = solicitor.Location,
            City = solicitor.City,
            ProfileSlug = solicitor.ProfileSlug,
            ProfileUrl = solicitor.ProfileUrl,
            ContactDetails = solicitor.ContactDetails.ToResponse(),
            Review = solicitor.Review.ToResponse()
        };
    }

    private static LocationSearchResult ToResponse(this Domain.LocationSearchResult locationResult)
    {
        return new LocationSearchResult
        {
            Location = locationResult.Location,
            Count = locationResult.Count
        };
    }

    private static ScrapeFailure ToResponse(this Domain.ScrapeFailure failure)
    {
        return new ScrapeFailure
        {
            Location = failure.Location,
            Code = failure.Code,
            Message = failure.Message
        };
    }

    private static SolicitorSearchReport ToResponse(this Domain.SolicitorSearchReport report)
    {
        return new SolicitorSearchReport
        {
            TotalSolicitors = report.TotalSolicitors,
            CountsByLocation = report.CountsByLocation,
            CountsByAreaOfLaw = report.CountsByAreaOfLaw,
            LocationsWithNoResults = report.LocationsWithNoResults,
            ContactCompleteness = report.ContactCompleteness,
            ReviewScoreSummary = report.ReviewScoreSummary.ToResponse()
        };
    }

    private static ReviewScoreSummary ToResponse(this Domain.ReviewScoreSummary summary)
    {
        return new ReviewScoreSummary
        {
            Minimum = summary.Minimum,
            Maximum = summary.Maximum,
            Average = summary.Average
        };
    }

    internal static SolicitorContactDetails ToResponse(this Domain.SolicitorContactDetails contactDetails)
    {
        return new SolicitorContactDetails
        {
            Phone = contactDetails.Phone,
            EmailUrl = contactDetails.EmailUrl,
            WebsiteUrl = contactDetails.WebsiteUrl,
            Address = contactDetails.Address
        };
    }

    internal static ReviewSummary? ToResponse(this Domain.ReviewSummary? review)
    {
        return review is null
            ? null
            : new ReviewSummary
            {
                Score = review.Score,
                Count = review.Count
            };
    }
}
