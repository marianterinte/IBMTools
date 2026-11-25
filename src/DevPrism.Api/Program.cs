// Spec: Documentation/dev-prism-ai-specs/04_ARCHITECTURE.md and 11_API_AND_EVENTS.md.
// Composition root for the loopback API and packaged React application.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
    options.ListenLocalhost(5081, endpoint => endpoint.UseHttps()));
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseStaticFiles();

var api = app.MapGroup("/api/v1");
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
