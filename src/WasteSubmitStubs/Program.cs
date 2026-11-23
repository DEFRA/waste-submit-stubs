using WasteSubmitStubs.Admin;
using WasteSubmitStubs.Logging;
using WasteSubmitStubs.Stubs.CommonDataApi;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.ConfigureLoggingAndTracing();
builder.Services.AddHealthChecks();
builder.Services.AddCommonDataApiStub();

var app = builder.Build();
app.MapHealthChecks("/health");
app.MapCommonDataApiStub();
app.MapAdmin();
app.Run();

namespace WasteSubmitStubs
{
    public partial class Program;
}
