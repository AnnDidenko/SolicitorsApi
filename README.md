# SolicitorsApi

.NET backend and React SPA for the InfoTrack solicitor search task.

The application searches solicitors.com by location and optional area of law, supports location suggestions, profile lookup, sorting, paging, review filtering, and user-readable API errors.

## Prerequisites

- .NET 10 SDK
- Node.js and npm
- HTTPS development certificate trusted locally:

```powershell
dotnet dev-certs https --trust
```

## Run The Application

Start the API:

```powershell
dotnet run --project .\SolicitorsApi --launch-profile https
```

The API listens on:

- `https://localhost:7034`
- `http://localhost:5156`

Start the SPA in another terminal:

```powershell
cd .\ClientApp
npm install
npm run dev
```

Open:

```text
http://localhost:5173
```

The Vite dev server proxies `/api` requests to `https://localhost:7034` by default. To point the SPA at a different API URL, set:

```powershell
$env:VITE_API_BASE_URL="https://localhost:7034"
npm run dev
```

## Debugging

In Visual Studio or Rider, use the `https` launch profile from [launchSettings.json](SolicitorsApi/Properties/launchSettings.json).

For VS Code, attach to the running `SolicitorsApi` process or create a standard `.NET Core Launch (web)` configuration targeting [SolicitorsApi.csproj](SolicitorsApi/SolicitorsApi.csproj).

When debugging the full app:

1. Start/debug the API first.
2. Start `npm run dev` from [ClientApp](ClientApp).
3. Browse to `http://localhost:5173`.

## Configuration

Configuration lives in [appsettings.json](SolicitorsApi/appsettings.json).

`SolicitorsCom` controls the upstream website integration:

- `BaseUrl`
- `ConveyancingPath`
- `AutocompletePath`
- `PrepareSearchPath`
- `TimeoutSeconds`

`SolicitorSearch` controls application search behavior:

- `DefaultLocations`
- `MaxLocations`
- `DefaultPageSize`
- `ProfileFetchConcurrency`

`SolicitorSearchCache` controls the temporary in-memory solicitor search cache:

- `Enabled`
- `ListTimeToLiveHours`
- `ProfileTimeToLiveHours`
- `MaxEntries`

The first cache implementation is intentionally in-memory only. It does not add EF Core persistence, database tables, migrations, or database setup, and cached entries are lost when the API process restarts. The application cache ports keep the adapter replaceable for a future table-backed implementation. Scheduled refresh, user search history, and background change detection are deferred to later OpenSpec changes.

Cache fallback does not add authentication or per-user cache isolation. Cache keys are derived from normalized search segments and normalized solicitor source identities, not raw caller-provided URLs. The cache stores parsed solicitor/list/profile data rather than raw HTML, and cache metadata is included in search responses so callers can see when fallback data was used.

Area-of-law options are not hardcoded. They are scraped from solicitors.com and use the site's own `did` option values for area searches.

Development CORS allows the Vite dev server through [appsettings.Development.json](SolicitorsApi/appsettings.Development.json).

## API Endpoints

Defaults:

```http
GET /api/solicitors/conveyancing/defaults
```

Search:

```http
POST /api/solicitors/conveyancing/search
Content-Type: application/json

{
  "locations": ["London"],
  "areaOfLaw": "Family",
  "minimumReviewScore": 4,
  "sort": {
    "field": "ReviewScore",
    "direction": "Descending"
  },
  "page": 1,
  "pageSize": 10
}
```

Location suggestions:

```http
GET /api/solicitors/locations/suggestions?query=Lon
```

Solicitor profile:

```http
GET /api/solicitors/{slug}
```

OpenAPI JSON is available in Development at:

```text
https://localhost:7034/openapi/v1.json
```

## Error Handling And Logs

The API returns user-readable errors:

- `400 Bad Request` for validation errors, including unknown cities.
- `424 Failed Dependency` when solicitors.com cannot be reached or a required scrape fails.
- `500 Internal Server Error` for unexpected failures, without leaking internal exception details.

Logs are written through the default ASP.NET Core logging providers. In local development, check the API terminal/debug output. No file log sink is configured.

## Build And Test

Backend build:

```powershell
dotnet build
```

Backend tests:

```powershell
dotnet test
```

SPA build:

```powershell
cd .\ClientApp
npm run build
```

Run `dotnet test` and `dotnet build` separately if you see a transient file-lock error from compiling the same project concurrently.
