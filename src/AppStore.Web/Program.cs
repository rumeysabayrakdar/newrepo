using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
// This host serves the browser-local demo only; it has no authenticated APIs.
// Explicit opt-in prevents accidentally deploying sample personas as production.
if (!(builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test")) ||
    !builder.Configuration.GetValue<bool>("AppStore:EnableDemo"))
{
    throw new InvalidOperationException(
        "This package is a demo static host. Set ASPNETCORE_ENVIRONMENT=Test and " +
        "AppStore__EnableDemo=true for testing. Production requires the .NET backend and authorization.");
}
var app = builder.Build();
var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
if (!File.Exists(Path.Combine(webRoot, "index.html")))
    throw new InvalidOperationException("Published frontend assets are missing. Run dotnet publish from the full project.");
var files = new PhysicalFileProvider(webRoot);
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Cache-Control"] = "no-store";
    await next();
});
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
app.MapGet("/healthz", () => Results.Json(new { status = "ok", mode = "demo", backendConnected = false }));
app.Run();
