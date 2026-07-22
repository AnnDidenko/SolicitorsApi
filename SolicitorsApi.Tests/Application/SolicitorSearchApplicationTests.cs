using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using SolicitorsApi.Application;
using SolicitorsApi.Application.Commands;
using SolicitorsApi.Application.Ports;
using SolicitorsApi.Application.Queries;
using SolicitorsApi.Application.Reports;
using SolicitorsApi.Application.Search;
using SolicitorsApi.Domain;

namespace SolicitorsApi.Tests.Application;

[TestFixture]
public class SolicitorSearchApplicationTests
{
    [Test]
    public async Task DefaultsQuery_ReturnsDefaultLocationsAreaOfLawAndSortOptions()
    {
        var provider = new FakeAreaOfLawOptionsProvider();
        var handler = new GetConveyancingSearchDefaultsHandler(
            new ConveyancingSearchDefaultsService(Options.Create(CreateSettings()), provider));

        var result = await handler.HandleAsync(new GetConveyancingSearchDefaultsQuery(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.DefaultLocations, Contains.Item("London"));
            Assert.That(result.Value.AreaOfLawOptions.Select(option => option.Name), Contains.Item("Conveyancing"));
            Assert.That(provider.FetchCount, Is.EqualTo(1));
            Assert.That(result.Value.SortFields, Contains.Item(nameof(SolicitorSearchSortField.ReviewScore)));
            Assert.That(result.Value.DefaultPageSize, Is.EqualTo(10));
        });
    }

    [Test]
    public async Task SearchCommand_ExpandsEmptyLocationsToDefaults()
    {
        var gateway = new FakeSolicitorSearchGateway();
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(new RunConveyancingSolicitorSearchCommand(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(gateway.SearchedLocations, Is.EqualTo(new[] { "London", "Leeds" }));
            Assert.That(result.Value!.UsedDefaultLocations, Is.True);
        });
    }

    [Test]
    public async Task SearchCommand_TrimsDeduplicatesAndAttemptsNonDefaultLocations()
    {
        var gateway = new FakeSolicitorSearchGateway();
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = [" London ", "london", "Bristol"]
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(gateway.SearchedLocations, Is.EqualTo(new[] { "London", "Bristol" }));
        });
    }

    [Test]
    public async Task SearchCommand_ReturnsValidationErrorsForBusinessRules()
    {
        var handler = CreateHandler(new FakeSolicitorSearchGateway());

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London", "Leeds", "Bristol"],
                AreaOfLaw = "Unsupported"
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain("maxLocationsExceeded"));
            Assert.That(result.Errors.Select(error => error.Code), Does.Contain("unsupportedAreaOfLaw"));
        });
    }

    [Test]
    public async Task SearchCommand_ReturnsValidationErrorForUnknownCity()
    {
        var handler = CreateHandler(new FakeSolicitorSearchGateway());

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["Atlantis"]
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(result.Errors.Single().Code, Is.EqualTo("locationNotFound"));
            Assert.That(result.Errors.Single().Message, Is.EqualTo("City 'Atlantis' does not exist."));
        });
    }

    [Test]
    public void LocationSuggestionRequest_HasModelValidationForShortQuery()
    {
        var request = new SolicitorsApi.Api.Contracts.LocationSuggestionRequest { Query = "Lo" };

        var errors = ValidateModel(request);

        Assert.That(errors.Select(error => error.MemberNames.Single()), Does.Contain(nameof(request.Query)));
    }

    [Test]
    public async Task SearchCommand_ReturnsFailedDependencyWhenAnyRequiredScrapeFails()
    {
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Failures =
                [
                    new ScrapeFailure
                    {
                        Location = "London",
                        Code = "searchPageFailed",
                        Message = "Upstream failed."
                    }
                ]
            }
        };
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand { Locations = ["London"] },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status424FailedDependency));
            Assert.That(result.Errors.Single().Field, Is.EqualTo("London"));
        });
    }

    [Test]
    public async Task SearchCommand_EnrichesProfilesOnlyForReviewFilteringOrSorting()
    {
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Solicitors =
                [
                    Solicitor("A Firm", "London", reviewScore: 1m, reviewCount: 1, profileSlug: "a-firm"),
                    Solicitor("B Firm", "London", reviewScore: 5m, reviewCount: 50, profileSlug: "b-firm")
                ],
                LocationResults = [new LocationSearchResult { Location = "London", Count = 2 }]
            },
            Profiles = new Dictionary<string, SolicitorProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["a-firm"] = Profile("A Firm", "a-firm", 4.8m, 20),
                ["b-firm"] = Profile("B Firm", "b-firm", 3.2m, 4)
            }
        };
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London"],
                MinimumReviewScore = 4,
                Sort = new SolicitorSearchSortRequest
                {
                    Field = nameof(SolicitorSearchSortField.ReviewScore),
                    Direction = nameof(SortDirection.Descending)
                }
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(gateway.ProfileFetchCount, Is.EqualTo(1));
            Assert.That(result.Value!.Paging.Items.Select(solicitor => solicitor.Name), Is.EqualTo(new[] { "A Firm" }));
            Assert.That(result.Value.Paging.Items.Single().Review!.Score, Is.EqualTo(4.8m));
            Assert.That(result.Value.Paging.TotalCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task SearchCommand_DeduplicatesSolicitorsByProfileSlugBeforePaging()
    {
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Solicitors =
                [
                    Solicitor("A Firm", "London", profileSlug: "a-firm"),
                    Solicitor("A Firm", "London", profileSlug: "a-firm"),
                    Solicitor("B Firm", "London", profileSlug: "b-firm")
                ],
                LocationResults = [new LocationSearchResult { Location = "London", Count = 3 }]
            }
        };
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London"]
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Paging.Items.Select(solicitor => solicitor.Name), Is.EqualTo(new[] { "A Firm", "B Firm" }));
            Assert.That(result.Value.Paging.TotalCount, Is.EqualTo(2));
            Assert.That(result.Value.Report.TotalSolicitors, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task SearchCommand_DoesNotEnrichProfilesForNameSorting()
    {
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Solicitors = [Solicitor("A Firm", "London", profileSlug: "a-firm")],
                LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }]
            }
        };
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London"],
                Sort = new SolicitorSearchSortRequest
                {
                    Field = nameof(SolicitorSearchSortField.SolicitorName),
                    Direction = nameof(SortDirection.Ascending)
                }
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(gateway.ProfileFetchCount, Is.Zero);
        });
    }

    [Test]
    public async Task SearchCommand_KeepsAreaSpecificResultsWhenListRowsDoNotExposeAreaTags()
    {
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Solicitors = [Solicitor("Family Firm", "London", profileSlug: "family-firm")],
                LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }]
            }
        };
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London"],
                AreaOfLaw = "Conveyancing"
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Paging.TotalCount, Is.EqualTo(1));
            Assert.That(result.Value.Paging.Items.Single().Name, Is.EqualTo("Family Firm"));
            Assert.That(gateway.SearchedAreaOfLaw?.Slug, Is.EqualTo("conveyancing"));
        });
    }

    [Test]
    public void SearchFilter_FiltersByMinimumReviewScore()
    {
        var filter = new SolicitorSearchFilter();
        var area = new AreaOfLaw { Name = "Conveyancing", Slug = "conveyancing" };
        var solicitors = new[]
        {
            Solicitor("Match", "London", area, 4.5m),
            Solicitor("Wrong Score", "London", area, 3.5m),
            Solicitor("No Review", "London", area)
        };

        var result = filter.Apply(solicitors, 4m);

        Assert.That(result.Select(solicitor => solicitor.Name), Is.EqualTo(new[] { "Match" }));
    }

    [Test]
    public void SearchSorter_SortsBySupportedFields()
    {
        var sorter = new SolicitorSearchSorter();
        var solicitors = new[]
        {
            Solicitor("Charlie", "Leeds", reviewScore: 4m, reviewCount: 20),
            Solicitor("Alpha", "London", reviewScore: 5m, reviewCount: 5),
            Solicitor("Bravo", "Bristol", reviewScore: 3m, reviewCount: 40),
            Solicitor("Delta", "Cardiff", reviewScore: 4m, reviewCount: 200),
            Solicitor("No Review", "York")
        };

        Assert.Multiple(() =>
        {
            Assert.That(SortNames(sorter, solicitors, SolicitorSearchSortField.City), Is.EqualTo(new[] { "Bravo", "Delta", "Charlie", "Alpha", "No Review" }));
            Assert.That(SortNames(sorter, solicitors, SolicitorSearchSortField.Location), Is.EqualTo(new[] { "Bravo", "Delta", "Charlie", "Alpha", "No Review" }));
            Assert.That(SortNames(sorter, solicitors, SolicitorSearchSortField.SolicitorName), Is.EqualTo(new[] { "Alpha", "Bravo", "Charlie", "Delta", "No Review" }));
            Assert.That(SortNames(sorter, solicitors, SolicitorSearchSortField.ReviewScore, SortDirection.Descending), Is.EqualTo(new[] { "Alpha", "Delta", "Charlie", "Bravo", "No Review" }));
            Assert.That(SortNames(sorter, solicitors, SolicitorSearchSortField.ReviewScore), Is.EqualTo(new[] { "Bravo", "Delta", "Charlie", "Alpha", "No Review" }));
            Assert.That(SortNames(sorter, solicitors, SolicitorSearchSortField.ReviewCount, SortDirection.Descending), Is.EqualTo(new[] { "Delta", "Bravo", "Charlie", "Alpha", "No Review" }));
        });
    }

    [Test]
    public void Pager_ReturnsRequestedPageAndTotalCount()
    {
        var pager = new SolicitorSearchPager();
        var solicitors = Enumerable.Range(1, 25)
            .Select(index => Solicitor($"Firm {index}", "London"))
            .ToArray();

        var result = pager.Apply(solicitors, page: 2, pageSize: 10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Page, Is.EqualTo(2));
            Assert.That(result.PageSize, Is.EqualTo(10));
            Assert.That(result.TotalCount, Is.EqualTo(25));
            Assert.That(result.Items.First().Name, Is.EqualTo("Firm 11"));
            Assert.That(result.Items.Count, Is.EqualTo(10));
        });
    }

    [Test]
    public void PagedSortedContract_HasReusableDefaults()
    {
        var request = new SolicitorsApi.Api.Contracts.SolicitorSearchRequest();

        Assert.Multiple(() =>
        {
            Assert.That(request.Page, Is.EqualTo(1));
            Assert.That(request.PageSize, Is.EqualTo(10));
            Assert.That(request.Sort, Is.Null);
        });
    }

    [Test]
    public void SolicitorSearchRequest_HasModelValidationForApiShape()
    {
        var request = new SolicitorsApi.Api.Contracts.SolicitorSearchRequest
        {
            MinimumReviewScore = 6,
            Page = 0,
            PageSize = 0,
            Sort = new SolicitorsApi.Api.Contracts.SortOption
            {
                Field = "BadField",
                Direction = "Sideways"
            }
        };

        var errors = ValidateModel(request);

        Assert.Multiple(() =>
        {
            Assert.That(errors.Select(error => error.MemberNames.Single()), Does.Contain(nameof(request.MinimumReviewScore)));
            Assert.That(errors.Select(error => error.MemberNames.Single()), Does.Contain(nameof(request.Page)));
            Assert.That(errors.Select(error => error.MemberNames.Single()), Does.Contain(nameof(request.PageSize)));
            Assert.That(errors.Select(error => error.MemberNames.Single()), Does.Contain(nameof(request.Sort.Field)));
            Assert.That(errors.Select(error => error.MemberNames.Single()), Does.Contain(nameof(request.Sort.Direction)));
        });
    }

    private static RunConveyancingSolicitorSearchHandler CreateHandler(FakeSolicitorSearchGateway gateway)
    {
        var settings = new SolicitorSearchSettings
        {
            DefaultLocations = ["London", "Leeds"],
            MaxLocations = 2,
            DefaultPageSize = 10,
            ProfileFetchConcurrency = 2
        };
        var options = Options.Create(settings);
        var areaOfLawOptionsProvider = new FakeAreaOfLawOptionsProvider();
        var normalizer = new SolicitorSearchRequestNormalizer(options, areaOfLawOptionsProvider);
        var sorter = new SolicitorSearchSorter();

        return new RunConveyancingSolicitorSearchHandler(
            new SolicitorSearchRequestValidator(options, new FakeLocationSuggestionGateway()),
            normalizer,
            new SolicitorSearchScrapeService(gateway),
            new SolicitorSearchProfileEnricher(gateway, sorter, options),
            new SolicitorSearchFilter(),
            sorter,
            new SolicitorSearchResultFactory(
                new SolicitorSearchPager(),
                new SolicitorSearchReportBuilder()));
    }

    private static SolicitorSearchSettings CreateSettings()
    {
        return new SolicitorSearchSettings
        {
            DefaultLocations = ["London", "Leeds"],
            MaxLocations = 2,
            DefaultPageSize = 10,
            ProfileFetchConcurrency = 2
        };
    }

    private static IReadOnlyList<string> SortNames(
        SolicitorSearchSorter sorter,
        IReadOnlyList<Solicitor> solicitors,
        SolicitorSearchSortField field,
        SortDirection direction = SortDirection.Ascending)
    {
        return sorter.Apply(
                solicitors,
                new SolicitorSearchSort
                {
                    Field = field,
                    Direction = direction
                })
            .Select(solicitor => solicitor.Name)
            .ToArray();
    }

    private static Solicitor Solicitor(
        string name,
        string location,
        AreaOfLaw? areaOfLaw = null,
        decimal? reviewScore = null,
        int? reviewCount = null,
        string? profileSlug = null)
    {
        return new Solicitor
        {
            Name = name,
            Location = location,
            City = location,
            ProfileSlug = profileSlug,
            AreasOfLaw = areaOfLaw is null ? [] : [areaOfLaw],
            Review = reviewScore is null && reviewCount is null
                ? null
                : new ReviewSummary { Score = reviewScore, Count = reviewCount },
            ContactDetails = new SolicitorContactDetails
            {
                Phone = "020 0000",
                EmailUrl = "/enquiry-form.asp",
                WebsiteUrl = "https://example.test",
                Address = $"{location} office"
            }
        };
    }

    private static SolicitorProfile Profile(string name, string slug, decimal score, int count)
    {
        return new SolicitorProfile
        {
            Name = name,
            Slug = slug,
            AreasOfLaw = [new AreaOfLaw { Name = "Conveyancing", Slug = "conveyancing" }],
            Review = new ReviewSummary { Score = score, Count = count }
        };
    }

    private static IReadOnlyList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);

        Validator.TryValidateObject(model, context, results, validateAllProperties: true);

        foreach (var property in model.GetType().GetProperties())
        {
            var value = property.GetValue(model);

            if (value is null || value is string)
            {
                continue;
            }

            var nestedResults = new List<ValidationResult>();
            var nestedContext = new ValidationContext(value);

            Validator.TryValidateObject(value, nestedContext, nestedResults, validateAllProperties: true);
            results.AddRange(nestedResults);
        }

        return results;
    }

    private class FakeSolicitorSearchGateway : ISolicitorSearchGateway
    {
        public SolicitorSearchData SearchData { get; init; } = new();

        public IReadOnlyList<string> SearchedLocations { get; private set; } = [];

        public AreaOfLaw? SearchedAreaOfLaw { get; private set; }

        public IReadOnlyDictionary<string, SolicitorProfile> Profiles { get; init; } =
            new Dictionary<string, SolicitorProfile>(StringComparer.OrdinalIgnoreCase);

        public int ProfileFetchCount { get; private set; }

        public Task<SolicitorSearchData> SearchAsync(
            IReadOnlyList<string> locations,
            AreaOfLaw? areaOfLaw,
            CancellationToken cancellationToken)
        {
            SearchedLocations = locations;
            SearchedAreaOfLaw = areaOfLaw;

            return Task.FromResult(SearchData);
        }

        public Task<IReadOnlyDictionary<string, SolicitorProfile>> GetProfilesAsync(
            IReadOnlyList<Solicitor> solicitors,
            int maxConcurrency,
            CancellationToken cancellationToken)
        {
            ProfileFetchCount++;

            return Task.FromResult(Profiles);
        }
    }

    private class FakeLocationSuggestionGateway : ILocationSuggestionGateway
    {
        public Task<IReadOnlyList<LocationSuggestionResult>> GetSuggestionsAsync(
            string query,
            CancellationToken cancellationToken)
        {
            var locations = new[]
            {
                "Bristol",
                "Leeds",
                "London"
            };
            var suggestions = locations
                .Where(location => location.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .Select(location => new LocationSuggestionResult { Title = location })
                .ToArray();

            return Task.FromResult<IReadOnlyList<LocationSuggestionResult>>(suggestions);
        }
    }

    private class FakeAreaOfLawOptionsProvider : IAreaOfLawOptionsProvider
    {
        private readonly IReadOnlyList<AreaOfLaw> _areaOfLawOptions;

        public FakeAreaOfLawOptionsProvider()
            : this([new AreaOfLaw { Name = "Conveyancing", Slug = "conveyancing", SiteId = "192" }])
        {
        }

        public FakeAreaOfLawOptionsProvider(IReadOnlyList<AreaOfLaw> areaOfLawOptions)
        {
            _areaOfLawOptions = areaOfLawOptions;
        }

        public int FetchCount { get; private set; }

        public Task<IReadOnlyList<AreaOfLaw>> GetAsync(CancellationToken cancellationToken)
        {
            FetchCount++;

            return Task.FromResult(_areaOfLawOptions);
        }
    }
}
