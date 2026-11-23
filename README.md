# waste-submit-stubs

Stubs of the services Submit Data's CDP services call, for performance testing in CDP `perf-test` and for the local
stack. Owned by the Submit Data team.

Each stubbed service has its own route group, prefixed with the real service's repo name, and keeps the real path
under it. A consuming service only changes its base URL.

## Stubbed services

### epr-common-data-api

Base URL for a consumer: `http://<stub host>:8085/epr-common-data-api`.

| Route | Status |
| --- | --- |
| `GET /api/submissions/v1/pom/approved-submissions/{periodYear}` | Stubbed |
| Anything else under `/epr-common-data-api` | `501`, with the method and path logged |

What each period year returns is set under `CommonDataApi:ApprovedSubmissions:Years` in `appsettings.json`:

| Year | Profile | Returns |
| --- | --- | --- |
| 2021 | `ServerError` | `500` |
| 2022 | `GatewayTimeout` | `504` |
| 2023 | `Fixtures` | The small synthetic dataset, for exact-value checks |
| 2024 | `Generated`, sizes `Prd2024` | About 32,000 rows and 11 MB, sized from PRD's 2024 counts |
| 2025 to 2027 | `Generated`, sizes `Prd2025` | About 32,000 rows and 11 MB, sized from PRD's 2025 counts |
| Any other year | | `204`, as the real route does for a year with no data |

Generated years are built when the stub starts (about 2 seconds) and serve the same bytes every time. The IDs are
name-based UUIDs and the values come from a generator seeded by the year, so every replica and every restart give the
same data. The sizes, under `CommonDataApi:ApprovedSubmissions:Sizes`, are production counts only, measured in PRD
Synapse on 2026-11-19 and recorded on SUB-445. To add a year, add it under `Years` with an existing or new `Sizes`
entry.

#### Simulated upstream latency

Every approved-submissions response that would reach the database (200, 204, 500 and 504) waits first. A `400` for a
non-numeric year doesn't. Both settings default to 0.

| Environment variable | Effect |
| --- | --- |
| `CommonDataApi__ApprovedSubmissions__LatencyMs` | A fixed delay before each response |
| `CommonDataApi__ApprovedSubmissions__LatencyJitterMs` | An even random 0 to N ms on top |

Presets from PRD's `batch_log` (`sp_GetApprovedSubmissionsMyc`, 90 runs over 90 days). They're database time only, so
the real call will be slower:

| Preset | `LatencyMs` | `LatencyJitterMs` | Gives |
| --- | --- | --- | --- |
| None | 0 | 0 | The consumer's own cost |
| Typical | 7443 | 6754 | 7.4 to 14.2 s, averaging 10.8 s |
| Worst | 35020 | 0 | Every call as slow as PRD's slowest |

The response shape, and where it was copied from, is in
[samples/epr-common-data-api](samples/epr-common-data-api/approved-submissions-with-obligations.md).

All data is synthetic.

## Admin routes

Read-only, with synthetic data and settings only.

| Route | Returns |
| --- | --- |
| `GET /admin/info` | The stub's version (`SERVICE_VERSION`), and each stub's years, dataset profiles, sizes and latency settings, for a perf run's evidence |
| `GET /admin/datasets/epr-common-data-api/approved-submissions/{year}` | That year's profile, row count, response size in bytes, `submitterIds`, and each submitter's type, producer count and row count, so the perf suite can discover its test data |
| `GET /health` | The CDP health check |

## Run it

```bash
dotnet run --project src/WasteSubmitStubs
```

It listens on `http://localhost:8085`. `GET /health` is the CDP health check.

Or in Docker, which also runs the tests while building the image:

```bash
COMMON_DATA_API_LATENCY_MS=7443 COMMON_DATA_API_LATENCY_JITTER_MS=6754 docker compose up --build
```

Leave out the two variables for no latency.

## Extend it

- A new route on a service that's already stubbed: [docs/adding-a-route.md](docs/adding-a-route.md).
- A new stubbed service: [docs/adding-a-stubbed-service.md](docs/adding-a-stubbed-service.md).

## Test it

```bash
dotnet build tests/WasteSubmitStubs.Test --warnaserror
dotnet test --test-modules tests/WasteSubmitStubs.Test/bin/Debug/net10.0/WasteSubmitStubs.Test.dll --no-build
```

## Licence

THIS INFORMATION IS LICENSED UNDER THE CONDITIONS OF THE OPEN GOVERNMENT LICENCE found at:

<http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3>
