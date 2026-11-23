# waste-submit-stubs

Stubs of the services Submit Data's CDP services call, so they can be performance tested in CDP `perf-test`, and run
in the local stack, without reaching Azure. Owned by the Submit Data team. Read `README.md` first.

## Rules

- **One stubbed service, one folder,** under `src/WasteSubmitStubs/Stubs/`, with its own route group prefixed with the
  real service's repo name. See [docs/adding-a-stubbed-service.md](docs/adding-a-stubbed-service.md).
- **Keep the real path under the prefix,** so a consumer only changes its base URL.
- **Copy the response shape, not the logic.** Return rows that look like the real ones; never re-implement the real
  service's rules, stored procedures or calculations.
- **Every route names its sources** (repo, file and commit) in its XML doc and in `samples/<repo-name>/`. See
  [docs/adding-a-route.md](docs/adding-a-route.md).
- **No mutable state.** Responses come from config and a seed, with IDs from `DeterministicGuid`. Several replicas
  must serve the same data, and a restart must not change it.
- **Anything not stubbed returns `501`** through `MapNotStubbed`, mapped last in each group.
- **Synthetic data only.** Never put production rows, IDs, names or personal data in fixtures, samples, config or
  tests. Production counts (how many rows, submitters and so on) are fine for sizing.
- **Admin routes are read-only** and expose synthetic data and settings only.

## Repositories it relates to

Expect these next to this repo on disk:

| Repo | Why |
| --- | --- |
| `waste-submit-reg-packaging-api` | Calls the epr-common-data-api stub. Its `CommonDataApiApprovedSubmissionsProvider` and `ApprovedSubmissionWithObligationsRow` are what the stub's response must satisfy |
| `epr-common-data-api` | The real service. Copy routes and response shapes from it |
| `epr-data-sqldb` | The stored procedures behind epr-common-data-api's routes, which decide column values, nulls and order |
| `waste-submit-performance-tests` | The k6 suite that runs against the stub in perf-test and reads `/admin/datasets` and `/admin/info` |
| `cdp-app-config` | The stub's per-environment settings, and each consumer's base URL |
| `epr-azure-stub` | The Manage Obligations team's stub, which this one is modelled on |

## Build and test

```bash
dotnet build tests/WasteSubmitStubs.Test --warnaserror
dotnet test --test-modules tests/WasteSubmitStubs.Test/bin/Debug/net10.0/WasteSubmitStubs.Test.dll --no-build
docker compose up --build
```

The Docker build runs the same build and tests, so a failing test fails the image.
