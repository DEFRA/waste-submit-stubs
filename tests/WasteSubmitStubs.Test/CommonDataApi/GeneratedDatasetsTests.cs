using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WasteSubmitStubs.Datasets;

namespace WasteSubmitStubs.Test.CommonDataApi;

/// <summary>
/// Checks each generated year against the PRD Synapse counts measured on 2026-11-19 (recorded on SUB-445).
/// </summary>
public class GeneratedDatasetsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Route = "/epr-common-data-api/api/submissions/v1/pom/approved-submissions/";

    /// <summary>The sizing query's estimate: about 350 bytes a row.</summary>
    private const int BytesPerRow = 350;

    public sealed record PrdCounts(
        int Year, int Rows, int Submitters, int DirectRegistrants, int ComplianceSchemes, int DistinctProducers,
        int LargestScheme, double AverageScheme, int MaxRowsForOneSubmitter, int RowsWithLeaverCode);

    public static readonly TheoryData<PrdCounts> Prd = new()
    {
        new PrdCounts(2024, 31842, 465, 446, 19, 9219, 2410, 459.89, 8444, 114),
        new PrdCounts(2025, 32048, 428, 411, 17, 9179, 2338, 513, 8247, 1196)
    };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<(byte[] Body, JsonElement[] Rows)> Get(int year)
    {
        using var response = await factory.CreateClient().GetAsync(Route + year, Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsByteArrayAsync(Token);
        return (body, JsonSerializer.Deserialize<JsonElement>(body).EnumerateArray().ToArray());
    }

    private static void Within(double expected, double actual, double tolerance, string what) =>
        Assert.True(Math.Abs(actual - expected) <= expected * tolerance,
            $"{what}: expected {expected} within {tolerance:P0}, got {actual}");

    [Theory]
    [MemberData(nameof(Prd))]
    public async Task GeneratedYear_MatchesPrdRowsAndSize(PrdCounts prd)
    {
        var (body, rows) = await Get(prd.Year);

        Within(prd.Rows, rows.Length, 0.02, "rows");
        Within(prd.Rows * BytesPerRow, body.Length, 0.10, "bytes");
    }

    [Theory]
    [MemberData(nameof(Prd))]
    public async Task GeneratedYear_MatchesPrdSubmittersAndProducers(PrdCounts prd)
    {
        var (_, rows) = await Get(prd.Year);

        var bySubmitter = rows.GroupBy(row => (Id: row.GetProperty("submitterId").GetGuid(), Type: row.GetProperty("submitterType").GetString())).ToList();
        var schemes = bySubmitter.Where(group => group.Key.Type == "ComplianceScheme")
            .Select(group => group.Select(row => row.GetProperty("organisationId").GetGuid()).Distinct().Count())
            .ToList();

        Assert.Equal(prd.Submitters, bySubmitter.Count);
        Assert.Equal(prd.DirectRegistrants, bySubmitter.Count(group => group.Key.Type == "DirectRegistrant"));
        Assert.Equal(prd.ComplianceSchemes, schemes.Count);
        Assert.Equal(prd.LargestScheme, schemes.Max());
        Within(prd.AverageScheme, schemes.Average(), 0.01, "average producers per scheme");
        Within(prd.DistinctProducers, rows.Select(row => row.GetProperty("organisationId").GetGuid()).Distinct().Count(), 0.02, "distinct producers");
        Within(prd.MaxRowsForOneSubmitter, bySubmitter.Max(group => group.Count()), 0.05, "rows for the largest submitter");
    }

    [Theory]
    [MemberData(nameof(Prd))]
    public async Task GeneratedYear_MatchesPrdLeaverCodeShare(PrdCounts prd)
    {
        var (_, rows) = await Get(prd.Year);

        var withCode = rows.Where(row => row.GetProperty("leaverCode").ValueKind == JsonValueKind.String).ToList();
        var share = (double)withCode.Count / rows.Length;

        Assert.InRange(share, (double)prd.RowsWithLeaverCode / prd.Rows - 0.005, (double)prd.RowsWithLeaverCode / prd.Rows + 0.005);
        Assert.All(withCode, row =>
        {
            var date = row.GetProperty("joinerDate").ValueKind == JsonValueKind.String
                ? row.GetProperty("joinerDate").GetDateTime()
                : row.GetProperty("leaverDate").GetDateTime();
            Assert.Equal(prd.Year + 1, date.Year);
        });
    }

    [Fact]
    public async Task GeneratedYear_UsesOnlyTheMaterialsAndValuesTheRealRouteReturns()
    {
        var (_, rows) = await Get(2025);

        Assert.All(rows, row =>
        {
            Assert.Contains(row.GetProperty("packagingMaterial").GetString(), new[] { "AL", "FC", "GL", "PC", "PL", "ST", "WD" });
            Assert.Equal("2025", row.GetProperty("submissionPeriod").GetString());
            Assert.Equal(
                (int)Math.Round(row.GetProperty("packagingMaterialWeightKg").GetInt64() / 1000.0, MidpointRounding.AwayFromZero),
                row.GetProperty("packagingMaterialWeight").GetInt32());
        });
        Assert.Equal(7, rows.Select(row => row.GetProperty("packagingMaterial").GetString()).Distinct().Count());
    }

    [Fact]
    public async Task LaterYear_IsSizedLike2025WithItsOwnIds()
    {
        var (_, rows2025) = await Get(2025);
        var (_, rows2026) = await Get(2026);

        Assert.Equal(rows2025.Length, rows2026.Length, (a, b) => Math.Abs(a - b) <= a * 0.02);
        Assert.All(rows2026, row => Assert.Equal("2026", row.GetProperty("submissionPeriod").GetString()));
        var ids2025 = rows2025.Select(row => row.GetProperty("submitterId").GetGuid()).ToHashSet();
        Assert.DoesNotContain(rows2026, row => ids2025.Contains(row.GetProperty("submitterId").GetGuid()));
    }

    [Fact]
    public async Task GeneratedYear_IsTheSameOnEveryStart()
    {
        await using var another = new WebApplicationFactory<Program>();
        var first = await factory.CreateClient().GetByteArrayAsync(Route + 2025, Token);
        var second = await another.CreateClient().GetByteArrayAsync(Route + 2025, Token);

        Assert.Equal(first, second);
    }

    [Fact]
    public void SchemeSizes_SpreadTheOthersEvenlyAroundTheirAverage()
    {
        var sizes = ApprovedSubmissionsGenerator.SchemeSizes(new GeneratedSizes
        {
            ComplianceSchemes = 17, LargestSchemeProducers = 2338, ProducersPerSchemeAverage = 513
        });

        Assert.Equal(17, sizes.Count);
        Assert.Equal(2338, sizes[0]);
        Assert.Equal(50, sizes.Skip(1).Min());
        Assert.Equal(748, sizes.Skip(1).Max());
        Within(513, sizes.Average(), 0.01, "average");
    }

    [Fact]
    public void DeterministicGuid_MatchesTheRfcVersion5TestVector()
    {
        var dns = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

        Assert.Equal(Guid.Parse("2ed6657d-e927-568b-95e1-2665a8aea6a2"), DeterministicGuid.Create(dns, "www.example.com"));
    }

    [Fact]
    public void GeneratedYearWithUndefinedSizes_FailsAtStartup()
    {
        using var broken = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("CommonDataApi:ApprovedSubmissions:Years:2028:Profile", "Generated")
                   .UseSetting("CommonDataApi:ApprovedSubmissions:Years:2028:Sizes", "Missing"));

        var error = Assert.ThrowsAny<Exception>(() => broken.CreateClient());
        Assert.Contains("Years:2028", error.ToString());
    }
}
