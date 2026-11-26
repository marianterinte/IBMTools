// Spec: Documentation/dev-prism-ai-specs/04_ARCHITECTURE.md and 11_API_AND_EVENTS.md.
// Composition root for the loopback API and packaged React application.
using DevPrism.Api.Features;
using DevPrism.Api.Local;
using DevPrism.Api.Mcp;
using DevPrism.Api.Projects;
using DevPrism.Api.Sync;
using DevPrism.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
// OAuth callback queries can contain a one-time code. Do not log request URLs.
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 64 * 1024;
    options.ListenLocalhost(LocalWorkspace.Port, endpoint => endpoint.UseHttps());
});
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi("v1");
builder.Services.AddDbContext<DevPrismDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DevPrism")));
builder.Services.AddHostedService<SyncRunRecovery>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<McpSessionState>();
builder.Services.AddSingleton<FigmaCredentials>();
builder.Services.AddSingleton<McpAuthorization>();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(LocalWorkspace.EnforceBoundary);
app.UseStaticFiles();

var api = app.MapGroup("/api/v1");
api.MapProjects();
api.MapSourceSelections();
api.MapMcp();
api.MapFigmaCredentials();
api.MapPost("/mcp/figma-desktop/check", FigmaDesktopCheck.Run)
    .Produces<FigmaDesktopCheck.Observation>().ProducesProblem(409);
McpOperations.MapCallbacks(app);
api.MapFeatures();
api.MapDecisions();
api.MapSyncRuns();
app.MapOpenApi("/api/openapi/{documentName}.json");
api.MapGet("/health", LocalHealth.Check).Produces<LocalHealth.Status>().Produces<LocalHealth.Status>(503);

app.MapFallback(static async context =>
{
    var request = context.Request;
    var index = context.RequestServices.GetRequiredService<IWebHostEnvironment>()
        .WebRootFileProvider.GetFileInfo("index.html");

    // Keep browser navigation separate from API errors and missing static assets.
    if ((!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
        || request.Path.StartsWithSegments("/api")
        || request.Path.StartsWithSegments("/media")
        || !request.GetTypedHeaders().Accept.Any(header =>
            header.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase) && header.Quality != 0)
        || !index.Exists)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }

    context.Response.ContentType = "text/html; charset=utf-8";
    context.Response.Headers.CacheControl = "no-cache";
    if (!HttpMethods.IsHead(request.Method))
    {
        await context.Response.SendFileAsync(index, context.RequestAborted);
    }
});

app.Run();
