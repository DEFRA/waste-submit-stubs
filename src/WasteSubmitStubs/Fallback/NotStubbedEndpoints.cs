namespace WasteSubmitStubs.Fallback;

/// <summary>
/// A catch-all for a stubbed service's routes that aren't stubbed yet. It returns <c>501</c> and logs the method
/// and path, so a run that calls a missing route fails loudly and shows what to add next.
/// </summary>
public static class NotStubbedEndpoints
{
    public static RouteGroupBuilder MapNotStubbed(this RouteGroupBuilder group, string serviceName)
    {
        group.Map("/{**path}", (HttpContext context, ILoggerFactory loggerFactory) =>
        {
            var method = context.Request.Method;
            var path = context.Request.Path.Value;

            loggerFactory.CreateLogger("WasteSubmitStubs.NotStubbed")
                .LogWarning("Route not stubbed for {Service}: {Method} {Path}", serviceName, method, path);

            return Results.Json(
                new { error = "Not stubbed", service = serviceName, method, path },
                statusCode: StatusCodes.Status501NotImplemented);
        });

        return group;
    }
}
