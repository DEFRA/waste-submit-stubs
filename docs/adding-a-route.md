# Adding a route to a stubbed service

Use this when a consumer starts calling another route on a service that's already stubbed, for example a second
epr-common-data-api call. A perf run that calls a route that isn't stubbed gets a `501`, and the stub logs
`Route not stubbed for {Service}: {Method} {Path}`. That log line is usually how you find out which route to add.

## 1. Record the real route

In `samples/<service>/<route-name>.md`, write down:

- the method and path, exactly as the consumer calls it, below the base URL;
- the status codes the real route returns, and when;
- the response shape: field names, types, nulls, ordering, and an example with synthetic values;
- the sources: repo, file and commit for the controller, the model and anything that shapes the values (a stored
  procedure, for example), and the consumer's model.

Capture a live response when the route is deployed somewhere with synthetic data. Until then, take it from the code
and say so in the file.

## 2. Add the route

Under `src/WasteSubmitStubs/Stubs/<Service>/`, add a file for the route, in a folder named after the real controller
or area (`Submissions/` for epr-common-data-api's `SubmissionsController`).

- **Keep the real path,** relative to the service's prefix. The group adds the prefix, so map
  `/api/submissions/v1/...`, not `/epr-common-data-api/api/...`.
- **Copy the response shape, not the logic.** Return rows that look like the real ones, without re-implementing the
  real service's rules.
- **Name the sources in the XML doc,** with the same repo, file and commit as the sample.
- **Keep it stateless.** Build responses from config and a seed, with IDs from `DeterministicGuid`, so replicas and
  restarts agree. If the response is large, build it once at startup and serve the bytes.
- **Match the real status codes** for the cases a consumer handles: no data, a bad parameter, a failure.

Then map it in the service's `Map<Service>Stub` method, before `MapNotStubbed`:

```csharp
var group = app.MapGroup(Prefix);
group.MapApprovedSubmissionsWithObligations();
group.MapYourNewRoute();          // new
group.MapNotStubbed(Service);     // always last
```

## 3. Add settings, if it needs any

Add an options class for the route under the service's options (`CommonDataApiOptions`), so its settings are under
`<Service>:<RouteName>` and can be set by environment variable (`CommonDataApi__YourRoute__LatencyMs`).

- If the route needs simulated latency, add `LatencyMs` and `LatencyJitterMs` and call `SimulatedLatency.Wait`, as
  `ApprovedSubmissionsWithObligations` does.
- Validate the settings at startup, so a bad value stops the stub instead of failing a perf run.
- Report them from the service's `IStubInfo`, so they show on `/admin/info`.

## 4. Add a datasets route, if a consumer's tests need its data

If the perf suite needs to know which IDs to call, add a read-only
`GET /admin/datasets/<service>/<route-name>/...` that returns them, next to the existing one in the service's
endpoints file.

## 5. Test it

In `tests/WasteSubmitStubs.Test/<Service>/`, add HTTP-level tests with `WebApplicationFactory<Program>`:

- the success response's field names, types and an exact value from fixed data;
- each status code the route returns;
- that the route no longer returns `501`;
- latency, if the route has it.

Run the build and tests as the Dockerfile does:

```bash
dotnet build tests/WasteSubmitStubs.Test --warnaserror
dotnet test --test-modules tests/WasteSubmitStubs.Test/bin/Debug/net10.0/WasteSubmitStubs.Test.dll --no-build
```

## 6. Update the README

Add the route to the service's table in `README.md`, with its settings.
