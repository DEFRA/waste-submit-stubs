namespace WasteSubmitStubs.Admin;

/// <summary>What one stubbed service reports on <c>/admin/info</c>: its routes, datasets and latency settings.</summary>
public interface IStubInfo
{
    /// <summary>The real service's repo name, which is also its route prefix.</summary>
    string Service { get; }

    object Describe();
}

/// <summary>
/// Read-only admin routes. They expose synthetic data and settings only, and need no auth, as the stubbed calls
/// have none.
/// </summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdmin(this IEndpointRouteBuilder app)
    {
        // The stub's version and every stub's settings, for a perf run's evidence.
        app.MapGet("/admin/info", (IEnumerable<IStubInfo> stubs) => Results.Ok(new
        {
            service = "waste-submit-stubs",
            version = Environment.GetEnvironmentVariable("SERVICE_VERSION") ?? "local",
            stubs = stubs.ToDictionary(stub => stub.Service, stub => stub.Describe())
        }));

        return app;
    }
}
