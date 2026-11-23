using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WasteSubmitStubs.Test;

public class PlatformTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Health_Returns200()
    {
        using var response = await _client.GetAsync("/health", Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/epr-common-data-api/api/submissions/v1/pom/something-new/2025")]
    [InlineData("POST", "/epr-common-data-api/api/submissions/v1/pom/approved-submissions/2025")]
    [InlineData("GET", "/epr-common-data-api/api/organisations/12345")]
    public async Task UnstubbedRouteUnderAStubbedService_Returns501WithTheMethodAndPath(string method, string path)
    {
        using var response = await _client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path), Token);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        var body = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(Token));
        Assert.Equal("epr-common-data-api", body.GetProperty("service").GetString());
        Assert.Equal(method, body.GetProperty("method").GetString());
        Assert.Equal(path, body.GetProperty("path").GetString());
    }

    [Fact]
    public async Task PathOutsideAnyStubbedService_Returns404()
    {
        using var response = await _client.GetAsync("/some-other-service/anything", Token);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
