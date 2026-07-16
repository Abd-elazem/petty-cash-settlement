using Asp.Versioning;
using PettyCash.Api;
using PettyCash.Api.Development;
using PettyCash.Api.Endpoints;
using PettyCash.Application.Abstractions;
using PettyCash.Application.DependencyInjection;
using PettyCash.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// --- Logging: structured JSON console output, no extra package (Microsoft.Extensions.Logging.Console
// ships with Microsoft.NET.Sdk.Web) — matches ARCHITECTURE.md §11's "structured (JSON)" requirement
// without adding a Serilog dependency this sprint, given how much package-version friction Sprint 4 hit.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
});
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddDebug();
}

// --- Application + Infrastructure DI. Api never registers a repository, handler, or DbContext
// itself — it only calls the two composition-root entry points each layer already exposes.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// --- ICurrentUserContext is deliberately NOT registered by AddInfrastructure() (D-025) —
// it needs real authentication, which doesn't exist yet. Registering a fixed development
// stand-in here, gated strictly to the Development environment, closes the DI-validation
// gap that was blocking `dotnet run` without pulling authentication into this sprint's
// scope. Swapping this for a real JWT/Entra-backed implementation later is a registration
// change in this file only — Application and Domain are untouched either way.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<ICurrentUserContext, DevelopmentCurrentUserContext>();
}

// --- Problem Details + global exception handling (Sprint 5 deliverable). Current ASP.NET Core
// 8/9 best practice: IExceptionHandler + AddProblemDetails(), no third-party library needed.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// --- API versioning foundation. URL-segment reader matches the /api/v1 base path already
// documented in ARCHITECTURE.md §9 (written at Milestone 0.1, before any API existed).
// No endpoints exist yet to version — this just establishes the convention so the first real
// endpoint, whenever it's added, has somewhere correct to live.
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

// --- OpenAPI: built-in Microsoft.AspNetCore.OpenApi (current .NET 9 default, not Swashbuckle —
// Swashbuckle stopped being the template default starting .NET 9). Per-API-version OpenAPI
// documents aren't attempted here: per Microsoft's own .NET Blog, wiring Asp.Versioning to the
// new built-in OpenAPI generator wasn't supported until .NET 10 / Asp.Versioning 10 — attempting
// it on .NET 9 would mean hand-building glue Microsoft hadn't shipped yet for no current benefit
// (there are no endpoints to document per-version yet regardless).
builder.Services.AddOpenApi();

// --- Health check: real Postgres connectivity check, not just "the process is running."
var connectionString = builder.Configuration.GetConnectionString("PettyCashDev")
    ?? throw new InvalidOperationException("Missing connection string 'PettyCashDev'.");
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

app.MapSettlementsEndpoints();

app.Run();

// Exposed for WebApplicationFactory-based integration tests in a future sprint.
public partial class Program
{
}
