export type SortOption = {
  field: string;
  direction: string;
};

export type AreaOfLawOption = {
  name: string;
  slug: string;
  siteId: string;
};

export type SolicitorSearchDefaultsResponse = {
  defaultLocations: string[];
  areaOfLawOptions: AreaOfLawOption[];
  sortFields: string[];
  sortDirections: string[];
  defaultPageSize: number;
};

export type SolicitorSearchRequest = {
  locations: string[];
  areaOfLaw?: string;
  minimumReviewScore?: number;
  sort?: SortOption;
  page: number;
  pageSize: number;
};

export type ContactDetails = {
  phone?: string | null;
  emailUrl?: string | null;
  websiteUrl?: string | null;
  address?: string | null;
};

export type ReviewSummary = {
  score?: number | null;
  count?: number | null;
};

export type SolicitorSearchResultItem = {
  name: string;
  location?: string | null;
  city?: string | null;
  profileSlug?: string | null;
  profileUrl?: string | null;
  contactDetails: ContactDetails;
  review?: ReviewSummary | null;
};

export type LocationSearchResult = {
  location: string;
  count: number;
};

export type ScrapeFailure = {
  location?: string | null;
  code: string;
  message: string;
};

export type SolicitorSearchReport = {
  totalSolicitors: number;
  countsByLocation: Record<string, number>;
  countsByAreaOfLaw: Record<string, number>;
  locationsWithNoResults: string[];
  contactCompleteness: Record<string, number>;
  reviewScoreSummary: {
    minimum?: number | null;
    maximum?: number | null;
    average?: number | null;
  };
};

export type SolicitorSearchResponse = {
  searchedAt: string;
  locations: string[];
  areaOfLaw?: string | null;
  filters: {
    minimumReviewScore?: number | null;
  };
  sort: SortOption;
  page: number;
  pageSize: number;
  totalCount: number;
  solicitors: SolicitorSearchResultItem[];
  locationResults: LocationSearchResult[];
  report: SolicitorSearchReport;
  failures: ScrapeFailure[];
};

export type LocationSuggestion = {
  title: string;
  text: string;
};

export type SolicitorOffice = {
  name?: string | null;
  address?: string | null;
  phone?: string | null;
  review?: ReviewSummary | null;
};

export type SolicitorProfileResponse = {
  name: string;
  slug: string;
  profileUrl?: string | null;
  contactDetails: ContactDetails;
  offices: SolicitorOffice[];
  areasOfLaw: string[];
  review?: ReviewSummary | null;
};
