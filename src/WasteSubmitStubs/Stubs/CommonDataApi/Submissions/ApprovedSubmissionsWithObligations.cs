using Microsoft.Extensions.Options;
using WasteSubmitStubs.Latency;

namespace WasteSubmitStubs.Stubs.CommonDataApi.Submissions;

/// <summary>
/// Stubs <c>GET api/submissions/v1/pom/approved-submissions/{periodYear}</c>.
/// </summary>
/// <remarks>
/// Copied from, not re-implemented:
/// <list type="bullet">
/// <item>Route and status codes: epr-common-data-api, <c>src/EPR.CommonDataService.Api/Controllers/SubmissionsController.cs</c>,
/// <c>GetApprovedSubmissionsWithObligations</c>, branch <c>SUB-410-approved-submissions-with-obligations</c> at <c>1373146</c>.</item>
/// <item>Row shape: <c>src/EPR.CommonDataService.Data/Entities/ApprovedSubmissionWithObligationsEntity.cs</c> on the same branch, and
/// the final <c>select</c> of epr-data-sqldb <c>dbo/Stored Procedures/sp_GetApprovedSubmissionsMycWithObligations.sql</c>, branch
/// <c>SUB-410-myc-with-obligations</c> at <c>94a7780</c>.</item>
/// <item>Sample: <c>samples/epr-common-data-api/approved-submissions-with-obligations.md</c>.</item>
/// </list>
/// </remarks>
public static class ApprovedSubmissionsWithObligations
{
    public const string Route = "/api/submissions/v1/pom/approved-submissions/{periodYear}";

    public static RouteGroupBuilder MapApprovedSubmissionsWithObligations(this RouteGroupBuilder group)
    {
        group.MapGet(Route, async (string periodYear, ApprovedSubmissionsCatalogue catalogue,
            IOptions<CommonDataApiOptions> options, HttpContext context) =>
        {
            // The real route binds an int; model binding rejects anything else before the database is called.
            if (!int.TryParse(periodYear, out var year))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["periodYear"] = [$"The value '{periodYear}' is not valid."]
                });
            }

            // Every outcome that reaches the database waits, as the real call does.
            var settings = options.Value.ApprovedSubmissions;
            await SimulatedLatency.Wait(settings.LatencyMs, settings.LatencyJitterMs, context.RequestAborted);

            var response = catalogue.ForYear(year);
            return response.Profile switch
            {
                DatasetProfile.ServerError => Results.Json(
                    "An error occurred while accessing the database.", statusCode: StatusCodes.Status500InternalServerError),
                DatasetProfile.GatewayTimeout => Results.Json(
                    "The operation has timed out.", statusCode: StatusCodes.Status504GatewayTimeout),
                _ when response.Body is null => Results.NoContent(),
                _ => Results.Bytes(response.Body, "application/json; charset=utf-8")
            };
        });

        return group;
    }
}
