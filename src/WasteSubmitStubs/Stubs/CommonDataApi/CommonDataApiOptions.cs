using WasteSubmitStubs.Datasets;

namespace WasteSubmitStubs.Stubs.CommonDataApi;

/// <summary>Settings for the epr-common-data-api stub, bound from the <c>CommonDataApi</c> section.</summary>
public sealed class CommonDataApiOptions
{
    public const string SectionName = "CommonDataApi";

    public ApprovedSubmissionsOptions ApprovedSubmissions { get; init; } = new();
}

/// <summary>Settings for <c>GET api/submissions/v1/pom/approved-submissions/{periodYear}</c>.</summary>
public sealed class ApprovedSubmissionsOptions
{
    /// <summary>
    /// What each period year returns, keyed by year. A year that isn't listed returns <c>204</c>, as the real
    /// route does when a year has no approved submissions.
    /// </summary>
    public Dictionary<int, YearOptions> Years { get; init; } = [];

    /// <summary>Named size settings that <see cref="DatasetProfile.Generated"/> years refer to.</summary>
    public Dictionary<string, GeneratedSizes> Sizes { get; init; } = [];

    /// <summary>
    /// A fixed delay before every response, simulating the upstream's database time. Set with
    /// <c>CommonDataApi__ApprovedSubmissions__LatencyMs</c>. Presets from PRD: None 0, Typical 7443, Worst 35020.
    /// </summary>
    public int LatencyMs { get; init; }

    /// <summary>
    /// An even random 0 to N ms added to <see cref="LatencyMs"/>. Set with
    /// <c>CommonDataApi__ApprovedSubmissions__LatencyJitterMs</c>. Presets: None 0, Typical 6754, Worst 0.
    /// </summary>
    public int LatencyJitterMs { get; init; }
}

public sealed class YearOptions
{
    public DatasetProfile Profile { get; init; }

    /// <summary>For a <see cref="DatasetProfile.Generated"/> year: the name of its entry in <c>Sizes</c>.</summary>
    public string? Sizes { get; init; }
}

public enum DatasetProfile
{
    /// <summary>The small hand-written dataset, for exact-value checks.</summary>
    Fixtures,

    /// <summary>Synthetic rows at production size, from a named entry in <c>Sizes</c>.</summary>
    Generated,

    /// <summary>No rows, so the route returns <c>204</c>.</summary>
    Empty,

    /// <summary>The route returns <c>500</c>, as the real one does when the database call fails.</summary>
    ServerError,

    /// <summary>The route returns <c>504</c>, as the real one does when the database call times out.</summary>
    GatewayTimeout
}
