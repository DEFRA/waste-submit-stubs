# Adding a stubbed service

Use this when a Submit Data service in CDP starts calling a service that perf-test shouldn't reach, whether it runs
in Azure or elsewhere.

## 1. Check a stub is the right answer

- **Does the call stay in CDP?** If the real service runs in CDP `perf-test` and can take the load, call it.
- **Does another team stub it already?** epr-azure-stub, owned by the Manage Obligations team, stubs some Azure
  services. Reuse a stub whose data and behaviour fit, rather than copying it.

## 2. Create the folder

Everything for one stubbed service lives under one folder and one route prefix, the real service's repo name:

```
src/WasteSubmitStubs/Stubs/<Service>/
  <Service>Endpoints.cs        Add<Service>Stub and Map<Service>Stub, MapGroup("/<repo-name>")
  <Service>Options.cs          bound from "<Service>"
  <Area>/<Route>.cs            one file per route
tests/WasteSubmitStubs.Test/<Service>/
samples/<repo-name>/
```

`Stubs/CommonDataApi/` is the worked example.

In `<Service>Endpoints.cs`:

- `Add<Service>Stub(IServiceCollection)` binds and validates the options, registers anything the routes share, and
  registers an `IStubInfo` so the service shows on `/admin/info`.
- `Map<Service>Stub(IEndpointRouteBuilder)` maps a group at `/<repo-name>`, maps each route, then
  `group.MapNotStubbed("<repo-name>")` last.

Then call both from `Program.cs`, next to the existing services:

```csharp
builder.Services.AddCommonDataApiStub();
builder.Services.AddYourServiceStub();   // new

app.MapCommonDataApiStub();
app.MapYourServiceStub();                // new
```

## 3. Add its routes

Follow [adding-a-route.md](adding-a-route.md) for each one.

## 4. Point the consumer at it

The consumer changes only its base URL, to `https://waste-submit-stubs.<environment>.cdp-int.defra.cloud/<repo-name>`.
Set it in that consumer's `cdp-app-config` env file for the environment, and in its local compose with
`http://waste-submit-stubs:8085/<repo-name>`.

The call stays inside CDP, so it needs no Squid proxy allow-list entry.

## 5. Update the docs

Add the service to the README's "Stubbed services" section, with its routes, settings and what each case returns.
