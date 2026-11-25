// Spec: Documentation/dev-prism-ai-specs/04_ARCHITECTURE.md and 11_API_AND_EVENTS.md.
// Composition root for the loopback API and packaged React application.
using System.Threading.RateLimiting;
using DevPrism.Api.Auth;
using DevPrism.Api.Projects;
using DevPrism.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 64 * 1024;
    options.ListenLocalhost(5081, endpoint => endpoint.UseHttps());
});
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi("v1");
builder.Services.AddDbContext<DevPrismDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DevPrism")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<LocalOperatorSession>();
builder.Services.AddAuthentication(LocalOperatorSession.Scheme)
    .AddScheme<AuthenticationSchemeOptions, LocalOperatorHandler>(LocalOperatorSession.Scheme, _ => { });
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-DevPrism-CSRF";
    options.Cookie.Name = "__Host-DevPrismCsrf";
    options.Cookie.Path = "/";
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("pairing", _ => RateLimitPartition.GetFixedWindowLimiter("local", _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = 8, Window = TimeSpan.FromMinutes(1), QueueLimit = 0,
    }));
});

var app = builder.Build();
// Resolve the bootstrap grant now: its lifetime starts at startup, not the first HTTP request.
_ = app.Services.GetRequiredService<LocalOperatorSession>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/media"))
        context.Response.Headers.CacheControl = "no-store";
    await next(context);
});
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api/v1");
api.MapLocalAuth();
api.MapProjects();
app.MapOpenApi("/api/openapi/{documentName}.json").RequireAuthorization();
// The process can answer HTTP before the database and release prerequisites exist.
api.MapGet("/health", () => Results.Json(
    new { status = "notReady", stage = "scaffold" },
    statusCode: StatusCodes.Status503ServiceUnavailable));

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
