import { Fragment, useEffect, useMemo, useState } from "react";
import {
  getDefaults,
  getLocationSuggestions,
  getSolicitorProfile,
  searchSolicitors
} from "./api";
import type {
  AreaOfLawOption,
  LocationSuggestion,
  SolicitorProfileResponse,
  SolicitorSearchDefaultsResponse,
  SolicitorSearchResultItem,
  SolicitorSearchResponse
} from "./types";

const defaultSortFields = ["SolicitorName", "City", "Location", "ReviewScore", "ReviewCount"];
const defaultSortDirections = ["Ascending", "Descending"];

function App() {
  const [defaults, setDefaults] = useState<SolicitorSearchDefaultsResponse | null>(null);
  const [locations, setLocations] = useState<string[]>([]);
  const [locationInput, setLocationInput] = useState("");
  const [isLocationInputFocused, setIsLocationInputFocused] = useState(false);
  const [suggestions, setSuggestions] = useState<LocationSuggestion[]>([]);
  const [areaOfLaw, setAreaOfLaw] = useState("");
  const [minimumReviewScore, setMinimumReviewScore] = useState("");
  const [sortField, setSortField] = useState("SolicitorName");
  const [sortDirection, setSortDirection] = useState("Ascending");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchResult, setSearchResult] = useState<SolicitorSearchResponse | null>(null);
  const [selectedProfile, setSelectedProfile] = useState<SolicitorProfileResponse | null>(null);
  const [selectedProfileSlug, setSelectedProfileSlug] = useState<string | null>(null);
  const [isSearching, setIsSearching] = useState(false);
  const [isLoadingProfile, setIsLoadingProfile] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getDefaults()
      .then((response) => {
        setDefaults(response);
        setPageSize(response.defaultPageSize || 10);
      })
      .catch((exception: Error) => setError(exception.message));
  }, []);

  useEffect(() => {
    const trimmed = locationInput.trim();

    if (trimmed.length < 3) {
      setSuggestions([]);
      return;
    }

    const timeoutId = window.setTimeout(() => {
      getLocationSuggestions(trimmed)
        .then(setSuggestions)
        .catch(() => setSuggestions([]));
    }, 250);

    return () => window.clearTimeout(timeoutId);
  }, [locationInput]);

  const totalPages = useMemo(() => {
    if (!searchResult) {
      return 1;
    }

    return Math.max(1, Math.ceil(searchResult.totalCount / searchResult.pageSize));
  }, [searchResult]);

  const locationSuggestionOptions = useMemo(() => {
    const trimmed = locationInput.trim();
    const defaultLocations = defaults?.defaultLocations ?? [];
    const defaultMatches = trimmed
      ? defaultLocations.filter((location) => location.toLowerCase().includes(trimmed.toLowerCase()))
      : defaultLocations;
    const upstreamSuggestions = trimmed.length >= 3
      ? suggestions.map((suggestion) => suggestion.title)
      : [];
    const options = [...upstreamSuggestions, ...defaultMatches];

    return options.filter((option, index) =>
      options.findIndex((candidate) => candidate.toLowerCase() === option.toLowerCase()) === index &&
      !locations.some((location) => location.toLowerCase() === option.toLowerCase()));
  }, [defaults, locationInput, locations, suggestions]);

  async function runSearch(nextPage = page) {
    setIsSearching(true);
    setError(null);
    setSelectedProfile(null);
    setSelectedProfileSlug(null);

    try {
      const response = await searchSolicitors({
        locations,
        areaOfLaw: areaOfLaw || undefined,
        minimumReviewScore: minimumReviewScore ? Number(minimumReviewScore) : undefined,
        sort: {
          field: sortField,
          direction: sortDirection
        },
        page: nextPage,
        pageSize
      });

      setPage(nextPage);
      setSearchResult(response);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Search failed.");
    } finally {
      setIsSearching(false);
    }
  }

  function addLocation(value: string) {
    const trimmed = value.trim();

    if (!trimmed) {
      return;
    }

    setLocations((current) =>
      current.some((location) => location.toLowerCase() === trimmed.toLowerCase())
        ? current
        : [...current, trimmed]
    );
    setLocationInput("");
    setSuggestions([]);
    setIsLocationInputFocused(false);
    setPage(1);
  }

  function removeLocation(value: string) {
    setLocations((current) => current.filter((location) => location !== value));
    setPage(1);
  }

  function changeSortField(value: string) {
    setSortField(value);
    setPage(1);

    if (value === "ReviewScore" || value === "ReviewCount") {
      setSortDirection("Descending");
    }
  }

  async function openProfile(solicitor: SolicitorSearchResultItem) {
    const slug = solicitor.profileSlug || slugFromUrl(solicitor.profileUrl);

    if (!slug) {
      setError("This search result does not include a profile slug.");
      return;
    }

    if (selectedProfileSlug === slug) {
      setSelectedProfileSlug(null);
      setSelectedProfile(null);
      return;
    }

    setSelectedProfileSlug(slug);
    setSelectedProfile(null);
    setIsLoadingProfile(true);
    setError(null);

    try {
      const profile = await getSolicitorProfile(slug);
      setSelectedProfile(profile);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : "Profile request failed.");
    } finally {
      setIsLoadingProfile(false);
    }
  }

  return (
    <main className="app-shell">
      <section className="workspace">
        <aside className="controls" aria-label="Search controls">
          <div className="brand-block">
            <p>InfoTrack task</p>
            <h1>Solicitor search</h1>
          </div>

          <div className="field">
            <label htmlFor="location">Locations</label>
            <div className="location-input-row">
              <input
                id="location"
                value={locationInput}
                autoComplete="off"
                onChange={(event) => setLocationInput(event.target.value)}
                onFocus={() => setIsLocationInputFocused(true)}
                onClick={() => setIsLocationInputFocused(true)}
                onBlur={() => window.setTimeout(() => setIsLocationInputFocused(false), 120)}
                onKeyDown={(event) => {
                  if (event.key === "Enter") {
                    event.preventDefault();
                    addLocation(locationInput);
                  }
                }}
                placeholder="Type a city"
              />
              <button type="button" onClick={() => addLocation(locationInput)}>
                Add
              </button>
              {isLocationInputFocused && locationSuggestionOptions.length > 0 && (
                <div className="suggestion-menu" role="listbox" aria-label="Location suggestions">
                  {locationSuggestionOptions.map((location) => (
                    <button
                      key={location}
                      type="button"
                      role="option"
                      onMouseDown={(event) => event.preventDefault()}
                      onClick={() => addLocation(location)}
                    >
                      {location}
                    </button>
                  ))}
                </div>
              )}
            </div>
            <div className="chips" aria-live="polite">
              {locations.map((location) => (
                <button key={location} type="button" className="chip" onClick={() => removeLocation(location)}>
                  {location}
                </button>
              ))}
            </div>
          </div>

          <div className="field">
            <label htmlFor="areaOfLaw">Area of law</label>
            <select id="areaOfLaw" value={areaOfLaw} onChange={(event) => setAreaOfLaw(event.target.value)}>
              <option value="">Any area</option>
              {(defaults?.areaOfLawOptions ?? []).map((option: AreaOfLawOption) => (
                <option key={option.slug || option.name} value={option.slug || option.name}>
                  {option.name}
                </option>
              ))}
            </select>
          </div>

          <div className="field-grid">
            <div className="field">
              <label htmlFor="minimumReviewScore">Minimum review</label>
              <input
                id="minimumReviewScore"
                type="number"
                min="0"
                max="5"
                step="0.1"
                value={minimumReviewScore}
                onChange={(event) => setMinimumReviewScore(event.target.value)}
              />
            </div>
            <div className="field">
              <label htmlFor="pageSize">Page size</label>
              <select
                id="pageSize"
                value={pageSize}
                onChange={(event) => {
                  setPageSize(Number(event.target.value));
                  setPage(1);
                }}
              >
                {[10, 15, 25, 50].map((size) => (
                  <option key={size} value={size}>
                    {size}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="field-grid">
            <div className="field">
              <label htmlFor="sortField">Sort by</label>
              <select id="sortField" value={sortField} onChange={(event) => changeSortField(event.target.value)}>
                {(defaults?.sortFields.length ? defaults.sortFields : defaultSortFields).map((field) => (
                  <option key={field} value={field}>
                    {formatToken(field)}
                  </option>
                ))}
              </select>
            </div>
            <div className="field">
              <label htmlFor="sortDirection">Direction</label>
              <select
                id="sortDirection"
                value={sortDirection}
                onChange={(event) => setSortDirection(event.target.value)}
              >
                {(defaults?.sortDirections.length ? defaults.sortDirections : defaultSortDirections).map((direction) => (
                  <option key={direction} value={direction}>
                    {formatSortDirection(direction, sortField)}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <button className="primary-action" type="button" disabled={isSearching} onClick={() => runSearch(1)}>
            {isSearching ? "Searching" : "Search"}
          </button>
        </aside>

        <section className="results-area" aria-label="Solicitor results">
          <header className="results-header">
            <div>
              <p>{searchResult ? new Date(searchResult.searchedAt).toLocaleString() : "Ready"}</p>
              <h2>{searchResult ? `${searchResult.totalCount} solicitors` : "Results"}</h2>
            </div>
            {searchResult && (
              <div className="pager">
                <button type="button" disabled={page <= 1 || isSearching} onClick={() => runSearch(page - 1)}>
                  Prev
                </button>
                <span>
                  {page} / {totalPages}
                </span>
                <button type="button" disabled={page >= totalPages || isSearching} onClick={() => runSearch(page + 1)}>
                  Next
                </button>
              </div>
            )}
          </header>

          {error && <div className="error-panel">{error}</div>}
          {searchResult?.failures.length ? (
            <div className="notice-panel">
              {searchResult.failures.map((failure) => failure.message).join("\n")}
            </div>
          ) : null}

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Solicitor</th>
                  <th>Location</th>
                  <th>Phone</th>
                  <th>Review</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {(searchResult?.solicitors ?? []).map((solicitor) => {
                  const slug = solicitor.profileSlug || slugFromUrl(solicitor.profileUrl);
                  const isExpanded = Boolean(slug && selectedProfileSlug === slug);

                  return (
                    <Fragment key={`${solicitor.name}-${slug ?? solicitor.profileUrl ?? solicitor.location}`}>
                      <tr className={isExpanded ? "result-row expanded" : "result-row"}>
                      <td>
                        <strong>{solicitor.name}</strong>
                        <span>{solicitor.contactDetails.websiteUrl ?? solicitor.profileUrl}</span>
                      </td>
                      <td>{solicitor.city || solicitor.location || "-"}</td>
                      <td>{solicitor.contactDetails.phone || "-"}</td>
                      <td>{formatReview(solicitor.review)}</td>
                      <td>
                        <button type="button" onClick={() => openProfile(solicitor)}>
                          {isExpanded ? "Hide" : "Details"}
                        </button>
                      </td>
                    </tr>
                      {isExpanded && (
                        <tr className="detail-row">
                          <td colSpan={5}>
                            <ProfileDetails profile={selectedProfile} loading={isLoadingProfile} />
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  );
                })}
              </tbody>
            </table>
          </div>
        </section>
      </section>
    </main>
  );
}

type ProfilePanelProps = {
  profile: SolicitorProfileResponse | null;
  loading: boolean;
};

function ProfileDetails({ profile, loading }: ProfilePanelProps) {
  if (loading) {
    return <div className="inline-profile muted">Loading profile</div>;
  }

  if (!profile) {
    return <div className="inline-profile muted">Profile details are unavailable</div>;
  }

  return (
    <div className="inline-profile">
      <div>
        <p>{profile.slug}</p>
        <h3>{profile.name}</h3>
        <div className="profile-metric">{formatReview(profile.review)}</div>
        <dl>
          <dt>Phone</dt>
          <dd>{profile.contactDetails.phone || "-"}</dd>
          <dt>Website</dt>
          <dd>{profile.contactDetails.websiteUrl || "-"}</dd>
          <dt>Address</dt>
          <dd>{profile.contactDetails.address || "-"}</dd>
        </dl>
        <div className="tag-list">
          {profile.areasOfLaw.map((area) => (
            <span key={area}>{area}</span>
          ))}
        </div>
      </div>
      {profile.offices.length > 0 && (
        <div className="office-list">
          {profile.offices.map((office) => (
            <section key={`${office.name}-${office.address}`}>
              <strong>{office.name || "Office"}</strong>
              <span>{office.address || office.phone || "-"}</span>
            </section>
          ))}
        </div>
      )}
    </div>
  );
}

function formatReview(review?: { score?: number | null; count?: number | null } | null) {
  if (!review?.score) {
    return "-";
  }

  return `${review.score.toFixed(1)} (${review.count ?? 0})`;
}

function formatToken(value: string) {
  return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function formatSortDirection(direction: string, field: string) {
  if (field === "ReviewScore" || field === "ReviewCount") {
    return direction === "Descending" ? "Highest first" : "Lowest first";
  }

  return direction === "Descending" ? "Z to A" : "A to Z";
}

function slugFromUrl(url?: string | null) {
  if (!url) {
    return null;
  }

  const lastSegment = url.split("/").filter(Boolean).at(-1);

  return lastSegment?.replace(/\.html$/i, "") || null;
}

export default App;
