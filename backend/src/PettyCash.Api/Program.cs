using Asp.Versioning;
using PettyCash.Api;
using PettyCash.Api.DependencyInjection;
using PettyCash.Api.Endpoints;
using PettyCash.Application.DependencyInjection;
using PettyCash.Infrastructure.DependencyInjection;
using PettyCash.Infrastructure.SharePoint.Configuration;
using PettyCash.Infrastructure.SharePoint.Health;

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
builder.Services.AddAuthenticationFoundation(builder.Configuration, builder.Environment);

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

// --- Health checks: use the same backend selection used by Infrastructure repository wiring.
// If SharePoint is enabled, probe SharePoint/Graph; otherwise probe Postgres.
var sharePointEnabled = builder.Configuration.GetValue<bool>($"{SharePointFoundationOptions.SectionName}:Enabled");
var healthChecks = builder.Services.AddHealthChecks();
if (sharePointEnabled)
{
    healthChecks.AddCheck<SharePointGraphHealthCheck>(name: "sharepoint-graph");
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("PettyCashDev")
        ?? throw new InvalidOperationException("Missing connection string 'PettyCashDev'.");
    healthChecks.AddNpgSql(connectionString, name: "postgres");
}

// --- CORS: development-only policy for the Vite SPA dev server.
// Registered conditionally so production builds never register or apply a permissive policy.
// Allow any header/method because the SPA sends Content-Type + Authorization + preflight OPTIONS.
// AllowCredentials() is intentionally omitted: the SPA uses bearer tokens, not cookies, and
// the CORS spec prohibits combining AllowAnyOrigin with AllowCredentials.
const string DevCorsPolicyName = "DevSpa";
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(DevCorsPolicyName, policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:5173",   // Vite default port
                    "http://localhost:5174")   // Vite fallback port
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });
}

var app = builder.Build();

app.UseExceptionHandler();

// UseCors must precede UseAuthentication/UseAuthorization so preflight OPTIONS requests
// are short-circuited by CORS middleware before hitting the auth pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCors(DevCorsPolicyName);
}

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");

app.MapSettlementsEndpoints();
app.MapCategoryMappingsEndpoints();

app.Run();

// Exposed for WebApplicationFactory-based integration tests in a future sprint.
public partial class Program
{
}
