using Microsoft.Extensions.Options;
using WasteSubmitStubs.Admin;
using WasteSubmitStubs.Fallback;
using WasteSubmitStubs.Stubs.CommonDataApi.Submissions;

namespace WasteSubmitStubs.Stubs.CommonDataApi;

/// <summary>
/// The epr-common-data-api stub. Every route keeps the real service's path under the
/// <c>/epr-common-data-api</c> prefix, so a consuming service only changes its base URL.
/// </summary>
public static class CommonDataApiEndpoints
{
    public const string Service = "epr-common-data-api";
    public const string Prefix = "/" + Service;

    public static IServiceCollection AddCommonDataApiStub(this IServiceCollection services)
    {
        services.AddOptions<CommonDataApiOptions>()
            .BindConfiguration(CommonDataApiOptions.SectionName)
            .Validate(options => options.ApprovedSubmissions.LatencyMs >= 0 && options.ApprovedSubmissions.LatencyJitterMs >= 0,
                "CommonDataApi:ApprovedSubmissions:LatencyMs and LatencyJitterMs must be 0 or more.")
            .ValidateOnStart();
        services.AddSingleton<ApprovedSubmissionsCatalogue>();
        services.AddSingleton<IStubInfo, CommonDataApiInfo>();
        return services;
    }

    public static IEndpointRouteBuilder MapCommonDataApiStub(this IEndpointRouteBuilder app)
    {
        // Build every year's response now, at startup, so no request pays for it.
        app.ServiceProvider.GetRequiredService<ApprovedSubmissionsCatalogue>();

        var group = app.MapGroup(Prefix);
        group.MapApprovedSubmissionsWithObligations();
        group.MapNotStubbed(Service);

        // Test data discovery for the perf suite: the submitters and size of one year.
        app.MapGet($"/admin/datasets/{Service}/approved-submissions/{{year:int}}", (int year, ApprovedSubmissionsCatalogue catalogue) =>
        {
            var response = catalogue.ForYear(year);
            return Results.Ok(new
            {
                service = Service,
                route = ApprovedSubmissionsWithObligations.Route,
                year,
                profile = response.Profile.ToString(),
                sizes = response.Sizes,
                rowCount = response.RowCount,
                bytes = response.Bytes,
                submitterIds = response.Submitters.Select(submitter => submitter.Id),
                submitters = response.Submitters
            });
        });

        return app;
    }

    private sealed class CommonDataApiInfo(IOptions<CommonDataApiOptions> options, ApprovedSubmissionsCatalogue catalogue) : IStubInfo
    {
        public string Service => CommonDataApiEndpoints.Service;

        public object Describe()
        {
            var settings = options.Value.ApprovedSubmissions;
            return new
            {
                approvedSubmissions = new
                {
                    route = ApprovedSubmissionsWithObligations.Route,
                    latencyMs = settings.LatencyMs,
                    latencyJitterMs = settings.LatencyJitterMs,
                    years = catalogue.Years.OrderBy(year => year.Year).Select(year => new
                    {
                        year = year.Year,
                        profile = year.Profile.ToString(),
                        sizes = year.Sizes,
                        rowCount = year.RowCount,
                        submitters = year.Submitters.Count,
                        bytes = year.Bytes
                    })
                }
            };
        }
    }
}
