using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WasteSubmitStubs.Latency;

namespace WasteSubmitStubs.Test;

public class LatencyTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Route = "/epr-common-data-api/api/submissions/v1/pom/approved-submissions/";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private WebApplicationFactory<Program> WithLatency(int latencyMs, int jitterMs) => factory.WithWebHostBuilder(builder =>
        builder.UseSetting("CommonDataApi:ApprovedSubmissions:LatencyMs", latencyMs.ToString())
               .UseSetting("CommonDataApi:ApprovedSubmissions:LatencyJitterMs", jitterMs.ToString()));

    private static async Task<(HttpStatusCode Status, TimeSpan Elapsed)> Time(HttpClient client, string path)
    {
        var stopwatch = Stopwatch.StartNew();
        using var response = await client.GetAsync(path, Token);
        await response.Content.ReadAsByteArrayAsync(Token);
        return (response.StatusCode, stopwatch.Elapsed);
    }

    [Fact]
    public async Task Latency_DefaultsToZero()
    {
        var info = JsonSerializer.Deserialize<JsonElement>(await factory.CreateClient().GetStringAsync("/admin/info", Token));
        var settings = info.GetProperty("stubs").GetProperty("epr-common-data-api").GetProperty("approvedSubmissions");

        Assert.Equal(0, settings.GetProperty("latencyMs").GetInt32());
        Assert.Equal(0, settings.GetProperty("latencyJitterMs").GetInt32());
    }

    [Theory]
    [InlineData(2023, HttpStatusCode.OK)]
    [InlineData(2019, HttpStatusCode.NoContent)]
    [InlineData(2021, HttpStatusCode.InternalServerError)]
    [InlineData(2022, HttpStatusCode.GatewayTimeout)]
    public async Task Latency_DelaysEveryOutcomeThatReachesTheDatabase(int year, HttpStatusCode expected)
    {
        using var slow = WithLatency(400, 0);
        var client = slow.CreateClient();

        var (status, elapsed) = await Time(client, Route + year);

        Assert.Equal(expected, status);
        Assert.True(elapsed >= TimeSpan.FromMilliseconds(400), $"took {elapsed.TotalMilliseconds} ms");
    }

    [Fact]
    public async Task Latency_DoesNotDelayARequestRejectedBeforeTheDatabase()
    {
        using var slow = WithLatency(5000, 0);
        var client = slow.CreateClient();

        var (status, elapsed) = await Time(client, Route + "twenty");

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.True(elapsed < TimeSpan.FromSeconds(2), $"took {elapsed.TotalMilliseconds} ms");
    }

    [Fact]
    public async Task Latency_IsReportedOnAdminInfo()
    {
        using var typical = WithLatency(7443, 6754);
        var info = JsonSerializer.Deserialize<JsonElement>(await typical.CreateClient().GetStringAsync("/admin/info", Token));
        var settings = info.GetProperty("stubs").GetProperty("epr-common-data-api").GetProperty("approvedSubmissions");

        Assert.Equal(7443, settings.GetProperty("latencyMs").GetInt32());
        Assert.Equal(6754, settings.GetProperty("latencyJitterMs").GetInt32());
    }

    [Fact]
    public void NegativeLatency_FailsAtStartup()
    {
        using var broken = WithLatency(-1, 0);

        var error = Assert.ThrowsAny<Exception>(() => broken.CreateClient());
        Assert.Contains("LatencyMs", error.ToString());
    }

    [Fact]
    public void Jitter_StaysWithinTheFixedPartPlusZeroToN()
    {
        var random = new Random(1);
        var delays = Enumerable.Range(0, 10_000).Select(_ => SimulatedLatency.Next(7443, 6754, random).TotalMilliseconds).ToList();

        Assert.InRange(delays.Min(), 7443, 7443 + 100);
        Assert.InRange(delays.Max(), 7443 + 6754 - 100, 7443 + 6754);
        Assert.InRange(delays.Average(), 10820 - 100, 10820 + 100);
    }

    [Fact]
    public void NoJitter_GivesExactlyTheFixedPart()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(35020), SimulatedLatency.Next(35020, 0, new Random(1)));
        Assert.Equal(TimeSpan.Zero, SimulatedLatency.Next(0, 0, new Random(1)));
    }
}
