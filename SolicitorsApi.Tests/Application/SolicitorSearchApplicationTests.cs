using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using SolicitorsApi.Application;
using SolicitorsApi.Application.Cache;
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
    public async Task SearchCommand_CanBeConstructedWithCachePorts()
    {
        var gateway = new FakeSolicitorSearchGateway();
        var handler = CreateHandler(
            gateway,
            new FakeSolicitorSearchCache(),
            new FakeSolicitorProfileCache());

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand { Locations = ["London"] },
            CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
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
    public async Task SearchCommand_ValidationFailureBypassesCache()
    {
        var searchCache = new FakeSolicitorSearchCache();
        var profileCache = new FakeSolicitorProfileCache();
        var handler = CreateHandler(
            new FakeSolicitorSearchGateway(),
            searchCache,
            profileCache);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London", "Leeds", "Bristol"]
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(searchCache.ReadCount, Is.Zero);
            Assert.That(searchCache.StoredSegments, Is.Empty);
            Assert.That(profileCache.ReadCount, Is.Zero);
            Assert.That(profileCache.DiscoveredRecords, Is.Empty);
        });
    }

    [Test]
    public async Task SearchCommand_ReturnsValidationErrorWhenAllCitiesAreUnknown()
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
    public async Task SearchCommand_SearchesValidCitiesAndReportsUnknownCities()
    {
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Solicitors = [Solicitor("London Firm", "London")],
                LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }]
            }
        };
        var handler = CreateHandler(gateway);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London", "Atlantis"]
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(gateway.SearchedLocations, Is.EqualTo(new[] { "London" }));
            Assert.That(result.Value!.Locations, Is.EqualTo(new[] { "London" }));
            Assert.That(result.Value.Failures.Single().Code, Is.EqualTo("locationNotFound"));
            Assert.That(result.Value.Failures.Single().Location, Is.EqualTo("Atlantis"));
            Assert.That(result.Value.LocationResults.Select(location => location.Location), Is.EquivalentTo(new[] { "London", "Atlantis" }));
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
    public async Task SearchCommand_ScrapeFailureReturnsCachedSegmentsWhenAllLocationsAreCached()
    {
        var fetchedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var searchCache = new FakeSolicitorSearchCache
        {
            Segments =
            {
                ["London|"] = new SolicitorListCacheEntry
                {
                    Location = "London",
                    Solicitors = [Solicitor("Cached London Firm", "London")],
                    LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }],
                    FetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                }
            }
        };
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
        var handler = CreateHandler(gateway, searchCache);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand { Locations = ["London"] },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Paging.Items.Single().Name, Is.EqualTo("Cached London Firm"));
            Assert.That(gateway.SearchCount, Is.Zero);
            Assert.That(result.Value.Cache!.Status, Is.EqualTo(SolicitorSearchCacheStatus.Fresh));
            Assert.That(result.Value.Cache.UsedFallback, Is.False);
            Assert.That(result.Value.Cache.FetchedAt, Is.EqualTo(fetchedAt));
            Assert.That(result.Value.Paging.TotalCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task SearchCommand_ScrapeFailureWithoutCachedSegmentReturnsFailedDependency()
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
        var handler = CreateHandler(gateway, new FakeSolicitorSearchCache());

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand { Locations = ["London"] },
            CancellationToken.None);

        Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status424FailedDependency));
    }

    [Test]
    public async Task SearchCommand_ScrapeFailureWithExpiredCachedSegmentReturnsFailedDependency()
    {
        var fetchedAt = DateTimeOffset.UtcNow.AddHours(-25);
        var searchCache = new FakeSolicitorSearchCache
        {
            Segments =
            {
                ["London|"] = new SolicitorListCacheEntry
                {
                    Location = "London",
                    Solicitors = [Solicitor("Expired London Firm", "London")],
                    LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }],
                    FetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                }
            }
        };
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
        var handler = CreateHandler(gateway, searchCache);

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
    public async Task SearchCommand_UsesListReviewDataForReviewFilteringAndSorting()
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
            Assert.That(gateway.ProfileFetchCount, Is.Zero);
            Assert.That(result.Value!.Paging.Items.Select(solicitor => solicitor.Name), Is.EqualTo(new[] { "B Firm" }));
            Assert.That(result.Value.Paging.Items.Single().Review!.Score, Is.EqualTo(5m));
            Assert.That(result.Value.Paging.TotalCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task SearchCommand_ReusesCachedProfileDetailsForKnownSolicitors()
    {
        var fetchedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var profileCache = new FakeSolicitorProfileCache
        {
            Records =
            {
                ["slug:a-firm"] = new SolicitorProfileCacheRecord
                {
                    SourceIdentity = "slug:a-firm",
                    Solicitor = Solicitor("A Firm", "London", reviewScore: 1m, reviewCount: 1, profileSlug: "a-firm"),
                    Profile = Profile("A Firm", "a-firm", 4.8m, 20),
                    LastSeenAt = fetchedAt,
                    ProfileFetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                }
            }
        };
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Solicitors = [Solicitor("A Firm", "London", reviewScore: 1m, reviewCount: 1, profileSlug: "a-firm")],
                LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }]
            }
        };
        var handler = CreateHandler(gateway, profileCache: profileCache);

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
            Assert.That(gateway.ProfileFetchCount, Is.Zero);
            Assert.That(result.Value!.Paging.Items.Single().Review!.Score, Is.EqualTo(4.8m));
        });
    }

    [Test]
    public async Task SearchCommand_FetchesProfilesOnlyForNewMissingOrStaleRecords()
    {
        var fetchedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var expiredAt = DateTimeOffset.UtcNow.AddHours(-25);
        var profileCache = new FakeSolicitorProfileCache
        {
            Records =
            {
                ["slug:cached-firm"] = new SolicitorProfileCacheRecord
                {
                    SourceIdentity = "slug:cached-firm",
                    Solicitor = Solicitor("Cached Firm", "London", profileSlug: "cached-firm"),
                    Profile = Profile("Cached Firm", "cached-firm", 4.8m, 20),
                    LastSeenAt = fetchedAt,
                    ProfileFetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                },
                ["slug:stale-firm"] = new SolicitorProfileCacheRecord
                {
                    SourceIdentity = "slug:stale-firm",
                    Solicitor = Solicitor("Stale Firm", "London", profileSlug: "stale-firm"),
                    Profile = Profile("Stale Firm", "stale-firm", 4.4m, 8),
                    LastSeenAt = expiredAt,
                    ProfileFetchedAt = expiredAt,
                    ExpiresAt = expiredAt.AddHours(24)
                },
                ["slug:missing-profile-firm"] = new SolicitorProfileCacheRecord
                {
                    SourceIdentity = "slug:missing-profile-firm",
                    Solicitor = Solicitor("Missing Profile Firm", "London", profileSlug: "missing-profile-firm"),
                    LastSeenAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                }
            }
        };
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Solicitors =
                [
                    Solicitor("Cached Firm", "London", profileSlug: "cached-firm"),
                    Solicitor("Stale Firm", "London", profileSlug: "stale-firm"),
                    Solicitor("Missing Profile Firm", "London", profileSlug: "missing-profile-firm"),
                    Solicitor("New Firm", "London", profileSlug: "new-firm")
                ],
                LocationResults = [new LocationSearchResult { Location = "London", Count = 4 }]
            },
            Profiles = new Dictionary<string, SolicitorProfile>(StringComparer.OrdinalIgnoreCase)
            {
                ["stale-firm"] = Profile("Stale Firm", "stale-firm", 4.5m, 9),
                ["missing-profile-firm"] = Profile("Missing Profile Firm", "missing-profile-firm", 4.6m, 10),
                ["new-firm"] = Profile("New Firm", "new-firm", 4.7m, 11)
            }
        };
        var handler = CreateHandler(gateway, profileCache: profileCache);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London"],
                MinimumReviewScore = 4
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(gateway.ProfileFetchCount, Is.EqualTo(1));
            Assert.That(gateway.LastProfileFetchSolicitors.Select(solicitor => solicitor.Name), Is.EqualTo(new[] { "Stale Firm", "Missing Profile Firm", "New Firm" }));
            Assert.That(result.Value!.Paging.TotalCount, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task SearchCommand_ProfileCacheMissingReviewDataIsMissAndFetchFailureReturnsFailedDependency()
    {
        var fetchedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var profileCache = new FakeSolicitorProfileCache
        {
            Records =
            {
                ["slug:a-firm"] = new SolicitorProfileCacheRecord
                {
                    SourceIdentity = "slug:a-firm",
                    Solicitor = Solicitor("A Firm", "London", profileSlug: "a-firm"),
                    Profile = new SolicitorProfile
                    {
                        Name = "A Firm",
                        Slug = "a-firm"
                    },
                    LastSeenAt = fetchedAt,
                    ProfileFetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                }
            }
        };
        var gateway = new FakeSolicitorSearchGateway
        {
            ThrowOnProfileFetch = true,
            SearchData = new SolicitorSearchData
            {
                Solicitors = [Solicitor("A Firm", "London", profileSlug: "a-firm")],
                LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }]
            }
        };
        var handler = CreateHandler(gateway, profileCache: profileCache);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London"],
                MinimumReviewScore = 4
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.StatusCode, Is.EqualTo(StatusCodes.Status424FailedDependency));
            Assert.That(result.Errors.Single().Code, Is.EqualTo("profileEnrichmentFailed"));
            Assert.That(gateway.ProfileFetchCount, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task SearchCommand_ProfileEnrichmentFailureReturnsCachedMatchingProfiles()
    {
        var fetchedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var profileCache = new FakeSolicitorProfileCache
        {
            Records =
            {
                ["slug:a-firm"] = new SolicitorProfileCacheRecord
                {
                    SourceIdentity = "slug:a-firm",
                    Solicitor = Solicitor("A Firm", "London", profileSlug: "a-firm"),
                    Profile = Profile("A Firm", "a-firm", 4.8m, 20),
                    LastSeenAt = fetchedAt,
                    ProfileFetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                }
            }
        };
        var gateway = new FakeSolicitorSearchGateway
        {
            ThrowOnProfileFetch = true,
            SearchData = new SolicitorSearchData
            {
                Solicitors = [Solicitor("A Firm", "London", profileSlug: "a-firm")],
                LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }]
            }
        };
        var handler = CreateHandler(gateway, profileCache: profileCache);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London"],
                MinimumReviewScore = 4
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(gateway.ProfileFetchCount, Is.Zero);
            Assert.That(result.Value!.Paging.Items.Single().Review!.Score, Is.EqualTo(4.8m));
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
    public async Task SearchCommand_SuccessfulLiveSearchWritesListAndDiscoveredProfileCache()
    {
        var searchCache = new FakeSolicitorSearchCache();
        var profileCache = new FakeSolicitorProfileCache();
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Solicitors =
                [
                    Solicitor("A Firm", "London", profileSlug: "a-firm"),
                    Solicitor("B Firm", "Birmingham", profileSlug: "b-firm")
                ],
                LocationResults =
                [
                    new LocationSearchResult { Location = "London", Count = 1 },
                    new LocationSearchResult { Location = "Birmingham", Count = 1 }
                ]
            }
        };
        var handler = CreateHandler(gateway, searchCache, profileCache);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London", "Birmingham"]
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(searchCache.StoredSegments.Select(segment => segment.Location), Is.EqualTo(new[] { "London", "Birmingham" }));
            Assert.That(searchCache.StoredSegments.Single(segment => segment.Location == "London").Solicitors.Single().Name, Is.EqualTo("A Firm"));
            Assert.That(profileCache.DiscoveredRecords.Select(record => record.SourceIdentity), Is.EquivalentTo(new[] { "slug:a-firm", "slug:b-firm" }));
            Assert.That(result.Value!.Cache!.Status, Is.EqualTo(SolicitorSearchCacheStatus.Fresh));
            Assert.That(result.Value.Cache.UsedFallback, Is.False);
        });
    }

    [Test]
    public async Task SearchCommand_UsesFreshListCacheForRepeatedReviewSortSearch()
    {
        var fetchedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var searchCache = new FakeSolicitorSearchCache
        {
            Segments =
            {
                ["London|"] = new SolicitorListCacheEntry
                {
                    Location = "London",
                    Solicitors =
                    [
                        Solicitor("A Firm", "London", reviewScore: 4.1m, reviewCount: 10, profileSlug: "a-firm"),
                        Solicitor("B Firm", "London", reviewScore: 4.9m, reviewCount: 20, profileSlug: "b-firm")
                    ],
                    LocationResults = [new LocationSearchResult { Location = "London", Count = 2 }],
                    FetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                }
            }
        };
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Solicitors = [Solicitor("Live Firm", "London", reviewScore: 5m, reviewCount: 1, profileSlug: "live-firm")],
                LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }]
            }
        };
        var handler = CreateHandler(gateway, searchCache);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London"],
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
            Assert.That(gateway.SearchCount, Is.Zero);
            Assert.That(gateway.ProfileFetchCount, Is.Zero);
            Assert.That(result.Value!.Paging.Items.Select(solicitor => solicitor.Name), Is.EqualTo(new[] { "B Firm", "A Firm" }));
            Assert.That(result.Value.Cache!.UsedFallback, Is.False);
            Assert.That(result.Value.Cache.FetchedAt, Is.EqualTo(fetchedAt));
        });
    }

    [Test]
    public async Task SearchCommand_ReusesCachedLondonSegmentWhenExpandedSearchHasLiveBirmingham()
    {
        var fetchedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var searchCache = new FakeSolicitorSearchCache
        {
            Segments =
            {
                ["London|"] = new SolicitorListCacheEntry
                {
                    Location = "London",
                    Solicitors = [Solicitor("Cached London Firm", "London")],
                    LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }],
                    FetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                }
            }
        };
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Solicitors = [Solicitor("Live Birmingham Firm", "Birmingham")],
                LocationResults =
                [
                    new LocationSearchResult { Location = "London", Count = 0 },
                    new LocationSearchResult { Location = "Birmingham", Count = 1 }
                ],
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
        var handler = CreateHandler(gateway, searchCache);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand { Locations = ["London", "Birmingham"] },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Paging.Items.Select(solicitor => solicitor.Name), Is.EquivalentTo(new[] { "Cached London Firm", "Live Birmingham Firm" }));
            Assert.That(result.Value.LocationResults.Select(location => location.Location), Is.EquivalentTo(new[] { "London", "Birmingham" }));
            Assert.That(result.Value.Cache!.UsedFallback, Is.True);
        });
    }

    [Test]
    public async Task SearchCommand_ReusesCachedProfileDetailsForAreaFilteredKnownSolicitor()
    {
        var fetchedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var profileCache = new FakeSolicitorProfileCache
        {
            Records =
            {
                ["slug:family-firm"] = new SolicitorProfileCacheRecord
                {
                    SourceIdentity = "slug:family-firm",
                    Solicitor = Solicitor("Family Firm", "London", profileSlug: "family-firm"),
                    Profile = Profile("Family Firm", "family-firm", 4.8m, 20),
                    LastSeenAt = fetchedAt,
                    ProfileFetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                }
            }
        };
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Solicitors = [Solicitor("Family Firm", "London", profileSlug: "family-firm")],
                LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }]
            }
        };
        var handler = CreateHandler(gateway, profileCache: profileCache);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London"],
                AreaOfLaw = "Conveyancing",
                MinimumReviewScore = 4
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(gateway.ProfileFetchCount, Is.Zero);
            Assert.That(gateway.SearchedAreaOfLaw?.Slug, Is.EqualTo("conveyancing"));
            Assert.That(result.Value!.Paging.Items.Single().Review!.Score, Is.EqualTo(4.8m));
        });
    }

    [Test]
    public async Task SearchCommand_ShapesFilteredSortedReportAndPagedResultAfterLiveCacheAssembly()
    {
        var fetchedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var searchCache = new FakeSolicitorSearchCache
        {
            Segments =
            {
                ["London|"] = new SolicitorListCacheEntry
                {
                    Location = "London",
                    Solicitors =
                    [
                        Solicitor("Cached Top Firm", "London", reviewScore: 5m, reviewCount: 20),
                        Solicitor("Cached Low Firm", "London", reviewScore: 2m, reviewCount: 2)
                    ],
                    LocationResults = [new LocationSearchResult { Location = "London", Count = 2 }],
                    FetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                }
            }
        };
        var gateway = new FakeSolicitorSearchGateway
        {
            SearchData = new SolicitorSearchData
            {
                Solicitors =
                [
                    Solicitor("Live Middle Firm", "Birmingham", reviewScore: 4m, reviewCount: 12),
                    Solicitor("Live Low Firm", "Birmingham", reviewScore: 3m, reviewCount: 3)
                ],
                LocationResults =
                [
                    new LocationSearchResult { Location = "London", Count = 0 },
                    new LocationSearchResult { Location = "Birmingham", Count = 2 }
                ],
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
        var handler = CreateHandler(gateway, searchCache);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London", "Birmingham"],
                MinimumReviewScore = 4,
                Sort = new SolicitorSearchSortRequest
                {
                    Field = nameof(SolicitorSearchSortField.ReviewScore),
                    Direction = nameof(SortDirection.Descending)
                },
                Paging = new PagedSearchRequest
                {
                    Page = 1,
                    PageSize = 1
                }
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Paging.TotalCount, Is.EqualTo(2));
            Assert.That(result.Value.Paging.Items.Single().Name, Is.EqualTo("Cached Top Firm"));
            Assert.That(result.Value.Report.TotalSolicitors, Is.EqualTo(2));
            Assert.That(result.Value.Report.CountsByLocation.Keys, Is.EquivalentTo(new[] { "London", "Birmingham" }));
        });
    }

    [Test]
    public async Task SearchCommand_RecordsSearchListProfileAndFallbackMetricsWithoutPayloads()
    {
        var metrics = new FakeSearchPerformanceMetrics();
        var fetchedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var searchCache = new FakeSolicitorSearchCache
        {
            Segments =
            {
                ["London|"] = new SolicitorListCacheEntry
                {
                    Location = "London",
                    Solicitors = [Solicitor("Cached London Firm", "London", profileSlug: "cached-london-firm")],
                    LocationResults = [new LocationSearchResult { Location = "London", Count = 1 }],
                    FetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                }
            }
        };
        var profileCache = new FakeSolicitorProfileCache
        {
            Records =
            {
                ["slug:cached-london-firm"] = new SolicitorProfileCacheRecord
                {
                    SourceIdentity = "slug:cached-london-firm",
                    Solicitor = Solicitor("Cached London Firm", "London", profileSlug: "cached-london-firm"),
                    Profile = Profile("Cached London Firm", "cached-london-firm", 4.9m, 10),
                    LastSeenAt = fetchedAt,
                    ProfileFetchedAt = fetchedAt,
                    ExpiresAt = fetchedAt.AddHours(24)
                }
            }
        };
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
        var handler = CreateHandler(gateway, searchCache, profileCache, metrics);

        var result = await handler.HandleAsync(
            new RunConveyancingSolicitorSearchCommand
            {
                Locations = ["London"],
                MinimumReviewScore = 4
            },
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(metrics.Searches.Single().Status, Is.EqualTo("success"));
            Assert.That(metrics.ListFetches, Is.Empty);
            Assert.That(metrics.ProfileEnrichments.Single().FetchCount, Is.Zero);
            Assert.That(metrics.ProfileEnrichments.Single().CacheHitCount, Is.EqualTo(1));
            Assert.That(metrics.ProfileEnrichments.Single().CacheMissCount, Is.Zero);
            Assert.That(metrics.Fallbacks, Is.Empty);
            Assert.That(metrics.AllText, Does.Not.Contain("Cached London Firm"));
            Assert.That(metrics.AllText, Does.Not.Contain("020 0000"));
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

    private static RunConveyancingSolicitorSearchHandler CreateHandler(
        FakeSolicitorSearchGateway gateway,
        ISolicitorSearchCache? searchCache = null,
        ISolicitorProfileCache? profileCache = null,
        ISearchPerformanceMetrics? metrics = null)
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
            new SolicitorSearchScrapeService(gateway, metrics),
            new SolicitorSearchProfileEnricher(gateway, sorter, options, profileCache, metrics: metrics),
            new SolicitorSearchFilter(),
            sorter,
            new SolicitorSearchResultFactory(
                new SolicitorSearchPager(),
                new SolicitorSearchReportBuilder()),
            searchCache,
            profileCache,
            metrics);
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

        public bool ThrowOnProfileFetch { get; init; }

        public int SearchCount { get; private set; }

        public int ProfileFetchCount { get; private set; }

        public IReadOnlyList<Solicitor> LastProfileFetchSolicitors { get; private set; } = [];

        public Task<SolicitorSearchData> SearchAsync(
            IReadOnlyList<string> locations,
            AreaOfLaw? areaOfLaw,
            CancellationToken cancellationToken)
        {
            SearchCount++;
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
            LastProfileFetchSolicitors = solicitors;

            if (ThrowOnProfileFetch)
            {
                throw new HttpRequestException("Profile fetch failed.");
            }

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
                "Birmingham",
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

    private class FakeSolicitorSearchCache : ISolicitorSearchCache
    {
        public Dictionary<string, SolicitorListCacheEntry> Segments { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<SolicitorListCacheEntry> StoredSegments { get; } = [];

        public int ReadCount { get; private set; }

        public Task<SolicitorListCacheEntry?> GetListSegmentAsync(
            string location,
            string? areaOfLawSlug,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            Segments.TryGetValue($"{location}|{areaOfLawSlug}", out var entry);

            return Task.FromResult(entry);
        }

        public Task StoreListSegmentAsync(
            SolicitorListCacheEntry entry,
            CancellationToken cancellationToken)
        {
            StoredSegments.Add(entry);

            return Task.CompletedTask;
        }
    }

    private class FakeSolicitorProfileCache : ISolicitorProfileCache
    {
        public Dictionary<string, SolicitorProfileCacheRecord> Records { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<SolicitorProfileCacheRecord> DiscoveredRecords { get; } = [];

        public List<SolicitorProfileCacheRecord> ProfileRecords { get; } = [];

        public int ReadCount { get; private set; }

        public Task<IReadOnlyDictionary<string, SolicitorProfileCacheRecord>> GetBySourceIdentitiesAsync(
            IReadOnlyCollection<string> sourceIdentities,
            CancellationToken cancellationToken)
        {
            ReadCount++;
            var matches = sourceIdentities
                .Where(Records.ContainsKey)
                .ToDictionary(identity => identity, identity => Records[identity], StringComparer.OrdinalIgnoreCase);

            return Task.FromResult<IReadOnlyDictionary<string, SolicitorProfileCacheRecord>>(matches);
        }

        public Task UpsertDiscoveredSolicitorsAsync(
            IReadOnlyList<SolicitorProfileCacheRecord> records,
            CancellationToken cancellationToken)
        {
            DiscoveredRecords.AddRange(records);

            return Task.CompletedTask;
        }

        public Task UpsertProfileDetailsAsync(
            SolicitorProfileCacheRecord record,
            CancellationToken cancellationToken)
        {
            ProfileRecords.Add(record);
            Records[record.SourceIdentity] = record;

            return Task.CompletedTask;
        }
    }

    private class FakeSearchPerformanceMetrics : ISearchPerformanceMetrics
    {
        public List<RequestMetric> Requests { get; } = [];

        public List<SearchMetric> Searches { get; } = [];

        public List<ListFetchMetric> ListFetches { get; } = [];

        public List<ProfileEnrichmentMetric> ProfileEnrichments { get; } = [];

        public List<FallbackMetric> Fallbacks { get; } = [];

        public string AllText => string.Join(
            "|",
            Requests.Select(request => $"{request.Route}:{request.StatusCode}:{request.FailureCategory}")
                .Concat(Searches.Select(search => search.Status))
                .Concat(ListFetches.Select(fetch => $"{fetch.Count}:{fetch.Status}"))
                .Concat(ProfileEnrichments.Select(profile => $"{profile.FetchCount}:{profile.CacheHitCount}:{profile.CacheMissCount}:{profile.Status}"))
                .Concat(Fallbacks.Select(fallback => $"{fallback.Stage}:{fallback.Result}")));

        public void RecordRequest(
            string route,
            int statusCode,
            string failureCategory,
            TimeSpan elapsed)
        {
            Requests.Add(new RequestMetric(route, statusCode, failureCategory));
        }

        public void RecordSearch(
            string status,
            TimeSpan elapsed)
        {
            Searches.Add(new SearchMetric(status));
        }

        public void RecordListFetch(
            int count,
            string status,
            TimeSpan elapsed)
        {
            ListFetches.Add(new ListFetchMetric(count, status));
        }

        public void RecordProfileEnrichment(
            int fetchCount,
            int cacheHitCount,
            int cacheMissCount,
            string status,
            TimeSpan elapsed)
        {
            ProfileEnrichments.Add(new ProfileEnrichmentMetric(fetchCount, cacheHitCount, cacheMissCount, status));
        }

        public void RecordFallback(
            string stage,
            string result)
        {
            Fallbacks.Add(new FallbackMetric(stage, result));
        }

        public readonly record struct RequestMetric(
            string Route,
            int StatusCode,
            string FailureCategory);

        public readonly record struct SearchMetric(string Status);

        public readonly record struct ListFetchMetric(
            int Count,
            string Status);

        public readonly record struct ProfileEnrichmentMetric(
            int FetchCount,
            int CacheHitCount,
            int CacheMissCount,
            string Status);

        public readonly record struct FallbackMetric(
            string Stage,
            string Result);
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
