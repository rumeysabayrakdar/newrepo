using System.Text.Json;
using System.Text.Json.Nodes;
using AppStore.Web.Database;
using AppStore.Web.Identity;
using AppStore.Web.Catalogue;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var mode = builder.Configuration["AppStore:Mode"] ?? "AuthGate";
var isTest = builder.Environment.IsEnvironment("Test");
// Test always uses synthetic profiles, even if old deployment variables select AuthGate.
var demo = isTest || mode == "Demo";
if (mode is not ("Demo" or "AuthGate")) throw new InvalidOperationException("AppStore:Mode must be Demo or AuthGate.");
if (!(builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test")))
    throw new InvalidOperationException("This MVP supports Test/Development only. Production requires approved inventory, storage and a completed backend review.");
if (demo && !isTest && !builder.Configuration.GetValue<bool>("AppStore:EnableDemo"))
    throw new InvalidOperationException("Demo mode needs explicit AppStore__EnableDemo=true.");
var databaseEnabled = builder.Configuration.GetValue<bool>("Database:Enabled");
if (databaseEnabled && !isTest) throw new InvalidOperationException("Shared Test database mode requires ASPNETCORE_ENVIRONMENT=Test.");
if (databaseEnabled) builder.Services.AddSingleton<TestDatabase>();
builder.Services.AddSingleton<GateIdentityService>();
builder.Services.AddSingleton<CatalogueService>();
builder.Services.AddSingleton<ActionLog>();
var app = builder.Build();
if (databaseEnabled)
{
    try { await app.Services.GetRequiredService<TestDatabase>().Initialize(); }
    catch { throw new InvalidOperationException("The Test database could not be initialized. Check the secure connection, expected Test database name and PostgreSQL availability."); }
}
var webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
if (!File.Exists(Path.Combine(webRoot, "index.html"))) throw new InvalidOperationException("Published frontend assets are missing.");
var files = new PhysicalFileProvider(webRoot);
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Cache-Control"] = "no-store";
    context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
    try { await next(); }
    catch (GateException error)
    {
        context.Response.StatusCode = error.Status;
        await context.Response.WriteAsJsonAsync(new { code = error.Code, message = error.Message, correlationId = context.TraceIdentifier });
    }
    catch (Exception)
    {
        // Do not echo request identity or configuration into errors or logs.
        context.Response.StatusCode = 503;
        await context.Response.WriteAsJsonAsync(new { code = "service_unavailable", message = "This request could not be completed. Please contact support.", correlationId = context.TraceIdentifier });
    }
});
// Exact runtime route precedes static files, so browser input cannot switch the server mode.
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/runtime-config.js" && HttpMethods.IsGet(context.Request.Method))
    {
        context.Response.ContentType = "application/javascript";
        await context.Response.WriteAsync("window.ARCELIK_RUNTIME = " + JsonSerializer.Serialize(new { mode = databaseEnabled ? "testdb" : demo ? "demo" : "authgate" }) + ";");
        return;
    }
    await next();
});
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
app.UseStaticFiles(new StaticFileOptions { FileProvider = files });
app.MapGet("/healthz", () => Results.Json(new { status = "ok", mode = databaseEnabled ? "testdb" : demo ? "demo" : "authgate", backendConnected = databaseEnabled || !demo }));
app.MapGet("/api/me", (HttpContext context, GateIdentityService identities) =>
{
    if (demo) return Results.Json(new { mode = "demo", verified = false });
    var me = identities.Read(context);
    return Results.Json(new { verified = true, employeeId = me.EmployeeId, userId = me.UserId,
        email = me.Email, countryCode = me.CountryCode, workerClassification = me.WorkerClassification,
        country = me.Country, type = me.Type, isManager = me.IsManager, canBrowse = me.CanBrowse, profileStatus = me.ProfileStatus });
});
var api = app.MapGroup("/api");
api.AddEndpointFilter(async (invocation, next) =>
{
    if (demo) throw new GateException(404, "demo_api_unavailable", "This API is available only in AuthGate mode.");
    var context = invocation.HttpContext;
    context.Items["employee"] = context.RequestServices.GetRequiredService<GateIdentityService>().RequireCatalogue(context);
    return await next(invocation);
});
api.MapGet("/apps", (HttpContext context, CatalogueService catalogue) =>
{
    var person = (VerifiedEmployee)context.Items["employee"]!;
    return Results.Json(catalogue.List(person, context.Request.Query["tab"], context.Request.Query["search"], context.Request.Query["category"]));
});
api.MapGet("/apps/{id}", (string id, HttpContext context, CatalogueService catalogue) =>
{
    var person = (VerifiedEmployee)context.Items["employee"]!;
    return Results.Json(catalogue.Public(catalogue.Get(id, person), person));
});
foreach (var collection in new[] { "links", "documents" })
{
    var kind = collection;
    api.MapGet("/apps/{id}/" + kind + "/{linkId}/open", async (string id, string linkId, HttpContext context, CatalogueService catalogue, ActionLog log) =>
    {
        var person = (VerifiedEmployee)context.Items["employee"]!;
        var destination = catalogue.Resolve(id, kind, linkId, person);
        await log.Append(person, kind == "links" ? "application-open" : "document-open", id, linkId, null, context.TraceIdentifier);
        return Results.Redirect(destination);
    });
}
api.MapPost("/events/search", async (HttpContext context, ActionLog log) =>
{
    if (!context.Request.HasJsonContentType()) return Results.StatusCode(415);
    var origin = context.Request.Headers.Origin.ToString();
    if (!string.IsNullOrEmpty(origin) && (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
        uri.Authority != context.Request.Host.Value || uri.Scheme is not ("http" or "https")))
        return Results.StatusCode(403);
    if (context.Request.Headers["Sec-Fetch-Site"] == "cross-site") return Results.StatusCode(403);
    var body = await context.Request.ReadFromJsonAsync<SearchEvent>();
    if (body is null || body.Term is null || body.Term.Length > 200) return Results.BadRequest(new { message = "Search text must contain at most 200 characters." });
    await log.Append((VerifiedEmployee)context.Items["employee"]!, "search", null, null, body.Term, context.TraceIdentifier);
    return Results.NoContent();
});
if (databaseEnabled)
{
    var shared = app.MapGroup("/api/test");
    shared.AddEndpointFilter(async (invocation, next) =>
    {
        var context = invocation.HttpContext;
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            if (!context.Request.HasJsonContentType()) return Results.StatusCode(415);
            var origin = context.Request.Headers.Origin.ToString();
            if ((!string.IsNullOrEmpty(origin) && (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Authority != context.Request.Host.Value)) || context.Request.Headers["Sec-Fetch-Site"] == "cross-site")
                return Results.StatusCode(403);
        }
        context.Items["testPerson"] = context.RequestServices.GetRequiredService<TestDatabase>().Person(context);
        return await next(invocation);
    });
    shared.MapGet("/state", async (HttpContext context, TestDatabase db) => Results.Json(await db.Read((TestPerson)context.Items["testPerson"]!)));
    shared.MapPost("/admin/commands", async (HttpContext context, TestDatabase db, JsonObject command) =>
        Results.Json(await db.Change((TestPerson)context.Items["testPerson"]!, command, context.TraceIdentifier)));
    shared.MapPost("/events/search", async (HttpContext context, TestDatabase db, SearchEvent body) =>
    {
        await db.Event((TestPerson)context.Items["testPerson"]!, "Search", body.Term ?? "", context.TraceIdentifier);
        return Results.NoContent();
    });
    foreach (var collection in new[] { "links", "documents" })
    {
        var kind = collection;
        // A normal browser navigation cannot set X-Test-Persona. The Test-only profile ID
        // is supplied as an explicit persona query parameter for these navigation routes.
        app.MapGet("/api/test/open/{id}/" + kind + "/{linkId}", async (string id, string linkId, HttpContext context, TestDatabase db) =>
        {
            var persona = context.Request.Query["persona"].ToString();
            var person = db.People.SingleOrDefault(p => p.Id == persona) ?? throw new GateException(401, "unknown_test_profile", "Select a Test profile.");
            return Results.Redirect(await db.Open(person, id, kind, linkId, context.TraceIdentifier));
        });
    }
}
app.Run();
public sealed record SearchEvent(string? Term);
