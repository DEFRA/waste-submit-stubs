using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WasteSubmitStubs.Datasets;

namespace WasteSubmitStubs.Test.CommonDataApi;

public class ApprovedSubmissionsWithObligationsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Route = "/epr-common-data-api/api/submissions/v1/pom/approved-submissions/";

    private readonly HttpClient _client;

    public ApprovedSubmissionsWithObligationsTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private async Task<JsonElement[]> GetRows(int year)
    {
        using var response = await _client.GetAsync(Route + year, Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadAsStringAsync(Token);
        return JsonSerializer.Deserialize<JsonElement>(json).EnumerateArray().ToArray();
    }

    [Fact]
    public async Task FixturesYear_ReturnsAFlatArrayWithTheUpstreamFieldNamesInOrder()
    {
        var rows = await GetRows(2023);

        Assert.Equal(15, rows.Length);
        string[] expected =
        [
            "submissionPeriod", "packagingMaterial", "packagingMaterialWeight", "organisationId", "submitterId",
            "submitterType", "numberOfDaysObligated", "packagingMaterialWeightKg", "obligationStatus", "leaverCode",
            "joinerDate", "leaverDate"
        ];
        Assert.All(rows, row => Assert.Equal(expected, row.EnumerateObject().Select(property => property.Name)));
    }

    [Fact]
    public async Task FixturesYear_KeepsThePackagingApiFixtureIdsAndWeights()
    {
        var rows = await GetRows(2023);

        var directRegistrant = rows.Where(row => row.GetProperty("submitterId").GetGuid() == ApprovedSubmissionsFixtures.DirectRegistrantId).ToArray();
        Assert.Equal(3, directRegistrant.Length);
        Assert.All(directRegistrant, row =>
        {
            Assert.Equal("DirectRegistrant", row.GetProperty("submitterType").GetString());
            Assert.Equal(ApprovedSubmissionsFixtures.DirectRegistrantId, row.GetProperty("organisationId").GetGuid());
            Assert.Equal("2023", row.GetProperty("submissionPeriod").GetString());
        });

        var plastic = directRegistrant.Single(row => row.GetProperty("packagingMaterial").GetString() == "PL");
        Assert.Equal(412350, plastic.GetProperty("packagingMaterialWeightKg").GetInt64());
        Assert.Equal(412, plastic.GetProperty("packagingMaterialWeight").GetInt32());
        Assert.Equal(366, plastic.GetProperty("numberOfDaysObligated").GetInt16());
        Assert.Equal(JsonValueKind.Null, plastic.GetProperty("leaverCode").ValueKind);

        var scheme = rows.Where(row => row.GetProperty("submitterId").GetGuid() == ApprovedSubmissionsFixtures.ComplianceSchemeId).ToArray();
        Assert.Equal(12, scheme.Length);
        Assert.All(scheme, row => Assert.Equal("ComplianceScheme", row.GetProperty("submitterType").GetString()));
        Assert.Equal(4, scheme.Select(row => row.GetProperty("organisationId").GetGuid()).Distinct().Count());

        Assert.DoesNotContain(rows, row => row.GetProperty("submitterId").GetGuid() == ApprovedSubmissionsFixtures.NoSubmissionsId);
    }

    [Fact]
    public async Task FixturesYear_CarriesOneLeaverOrJoinerEventPerProducerInTheRelevantYear()
    {
        var rows = await GetRows(2023);

        JsonElement For(Guid producerId) => rows.First(row => row.GetProperty("organisationId").GetGuid() == producerId);

        var joiner = For(ApprovedSubmissionsFixtures.JoinerId);
        Assert.Equal("03", joiner.GetProperty("leaverCode").GetString());
        Assert.Equal("2024-07-05T00:00:00", joiner.GetProperty("joinerDate").GetString());
        Assert.Equal(JsonValueKind.Null, joiner.GetProperty("leaverDate").ValueKind);
        Assert.Equal(180, joiner.GetProperty("numberOfDaysObligated").GetInt16());

        var leaver = For(ApprovedSubmissionsFixtures.LeaverId);
        Assert.Equal("13", leaver.GetProperty("leaverCode").GetString());
        Assert.Equal("2024-09-30T00:00:00", leaver.GetProperty("leaverDate").GetString());
        Assert.Equal(274, leaver.GetProperty("numberOfDaysObligated").GetInt16());

        var rejoiner = For(ApprovedSubmissionsFixtures.RejoinerId);
        Assert.Equal("01", rejoiner.GetProperty("leaverCode").GetString());
        Assert.Equal("2024-10-01T00:00:00", rejoiner.GetProperty("joinerDate").GetString());
    }

    [Fact]
    public async Task FixturesYear_IsOrderedBySubmitterProducerAndMaterial()
    {
        var rows = await GetRows(2023);

        var keys = rows.Select(row => string.Join('|',
            row.GetProperty("submitterId").GetString(),
            row.GetProperty("organisationId").GetString(),
            row.GetProperty("packagingMaterial").GetString())).ToList();

        Assert.Equal(keys.Order(StringComparer.OrdinalIgnoreCase), keys);
    }

    [Fact]
    public async Task SameYear_ReturnsTheSameBytesEveryTime()
    {
        var first = await _client.GetByteArrayAsync(Route + 2023, Token);
        var second = await _client.GetByteArrayAsync(Route + 2023, Token);

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(2019)]
    [InlineData(2030)]
    public async Task UnconfiguredYear_Returns204(int year)
    {
        using var response = await _client.GetAsync(Route + year, Token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData(2021, HttpStatusCode.InternalServerError)]
    [InlineData(2022, HttpStatusCode.GatewayTimeout)]
    public async Task FaultYear_ReturnsItsReservedStatus(int year, HttpStatusCode expected)
    {
        using var response = await _client.GetAsync(Route + year, Token);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task NonNumericYear_Returns400()
    {
        using var response = await _client.GetAsync(Route + "twenty", Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
