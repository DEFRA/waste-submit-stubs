using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WasteSubmitStubs.Datasets;

namespace WasteSubmitStubs.Test;

public class AdminTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Route = "/epr-common-data-api/api/submissions/v1/pom/approved-submissions/";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<JsonElement> GetJson(string path) =>
        JsonSerializer.Deserialize<JsonElement>(await factory.CreateClient().GetStringAsync(path, Token));

    [Fact]
    public async Task Datasets_ListsEverySubmitterTheRouteReturns()
    {
        var dataset = await GetJson("/admin/datasets/epr-common-data-api/approved-submissions/2025");
        var rows = JsonSerializer.Deserialize<JsonElement>(await factory.CreateClient().GetByteArrayAsync(Route + 2025, Token))
            .EnumerateArray().ToArray();

        Assert.Equal("Generated", dataset.GetProperty("profile").GetString());
        Assert.Equal("Prd2025", dataset.GetProperty("sizes").GetString());
        Assert.Equal(rows.Length, dataset.GetProperty("rowCount").GetInt32());

        var fromRoute = rows.Select(row => row.GetProperty("submitterId").GetGuid()).ToHashSet();
        var listed = dataset.GetProperty("submitterIds").EnumerateArray().Select(id => id.GetGuid()).ToList();
        Assert.Equal(fromRoute.Count, listed.Count);
        Assert.True(fromRoute.SetEquals(listed));

        var submitters = dataset.GetProperty("submitters").EnumerateArray().ToList();
        Assert.Equal(411, submitters.Count(s => s.GetProperty("type").GetString() == "DirectRegistrant"));
        Assert.Equal(2338, submitters.Max(s => s.GetProperty("producers").GetInt32()));
        Assert.Equal(rows.Length, submitters.Sum(s => s.GetProperty("rows").GetInt32()));
    }

    [Fact]
    public async Task Datasets_ReportsTheResponseSizeInBytes()
    {
        var dataset = await GetJson("/admin/datasets/epr-common-data-api/approved-submissions/2024");
        var body = await factory.CreateClient().GetByteArrayAsync(Route + 2024, Token);

        Assert.Equal(body.Length, dataset.GetProperty("bytes").GetInt32());
    }

    [Fact]
    public async Task Datasets_ForTheFixturesYear_ListsTheFixtureSubmitters()
    {
        var dataset = await GetJson("/admin/datasets/epr-common-data-api/approved-submissions/2023");
        var listed = dataset.GetProperty("submitterIds").EnumerateArray().Select(id => id.GetGuid()).ToList();

        Assert.Equal("Fixtures", dataset.GetProperty("profile").GetString());
        Assert.Equal([ApprovedSubmissionsFixtures.DirectRegistrantId, ApprovedSubmissionsFixtures.ComplianceSchemeId], listed);
    }

    [Theory]
    [InlineData(2019, "Empty")]
    [InlineData(2021, "ServerError")]
    public async Task Datasets_ForAYearWithNoRows_SaysSoWithNoSubmitters(int year, string profile)
    {
        var dataset = await GetJson($"/admin/datasets/epr-common-data-api/approved-submissions/{year}");

        Assert.Equal(profile, dataset.GetProperty("profile").GetString());
        Assert.Equal(0, dataset.GetProperty("rowCount").GetInt32());
        Assert.Empty(dataset.GetProperty("submitterIds").EnumerateArray());
    }

    [Fact]
    public async Task Info_ReportsTheVersionAndEveryConfiguredYear()
    {
        var info = await GetJson("/admin/info");

        Assert.Equal("waste-submit-stubs", info.GetProperty("service").GetString());
        Assert.False(string.IsNullOrEmpty(info.GetProperty("version").GetString()));

        var years = info.GetProperty("stubs").GetProperty("epr-common-data-api").GetProperty("approvedSubmissions")
            .GetProperty("years").EnumerateArray().ToList();
        Assert.Equal([2021, 2022, 2023, 2024, 2025, 2026, 2027], years.Select(year => year.GetProperty("year").GetInt32()));
        var y2025 = years.Single(year => year.GetProperty("year").GetInt32() == 2025);
        Assert.Equal(428, y2025.GetProperty("submitters").GetInt32());
        Assert.True(y2025.GetProperty("bytes").GetInt32() > 10_000_000);
    }

    [Fact]
    public async Task Datasets_ForANonNumericYear_Returns404()
    {
        using var response = await factory.CreateClient().GetAsync("/admin/datasets/epr-common-data-api/approved-submissions/abc", Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
