using System.Text.Json;
using System.Text.Json.Nodes;
using AppStore.Web.Database;
using AppStore.Web.Identity;
using Microsoft.Extensions.FileProviders;

var directory = AppContext.BaseDirectory;
// Only this committed appsettings selector chooses Test/Production. Environment
// variables may supply secrets/connection details, but cannot change the mode.
var selector = new ConfigurationBuilder().SetBasePath(directory).AddJsonFile("appsettings.json", false, false).Build();
var environment = selector["AppStore:Environment"];
if (environment is not ("Test" or "Production")) throw new InvalidOperationException("Set AppStore:Environment to Test or Production in appsettings.json.");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, EnvironmentName = environment, ContentRootPath = directory });
builder.Configuration.Sources.Clear();
builder.Configuration.SetBasePath(directory).AddJsonFile("appsettings.json", false, false)
    .AddJsonFile($"appsettings.{environment}.json", false, false).AddEnvironmentVariables()
    .AddInMemoryCollection(new Dictionary<string,string?> { ["AppStore:Environment"] = environment, ["Authentication:Mode"] = environment == "Test" ? "TestProfiles" : "Gateway" });
var authMode=builder.Configuration["Authentication:Mode"]??"Gateway";
if(authMode is not ("Gateway" or "TestProfiles") || (environment=="Production"&&authMode!="Gateway"))
    throw new InvalidOperationException("Use Gateway authentication. TestProfiles is allowed only in Test.");
var testLogin=environment=="Test";
if(environment=="Production")
{
    var cidrs=builder.Configuration.GetSection("Authentication:TrustedProxyCidrs").Get<string[]>()??[];
    if(cidrs.Length==0 || cidrs.Any(c=>!System.Net.IPNetwork.TryParse(c,out var n)||n.PrefixLength==0))
        throw new InvalidOperationException("Configure trusted Norm auth-gate networks before starting Production.");
}
builder.WebHost.ConfigureKestrel(options => { options.Limits.MaxRequestBodySize = 115_343_360; options.ListenAnyIP(builder.Configuration.GetValue<int?>("Hosting:Port") ?? 8080); });

var originPolicy = new RequestOriginPolicy(builder.Configuration["Hosting:TrustedPublicOrigin"]);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options => options.MultipartBodyLengthLimit=115_343_360);
builder.Services.AddSingleton<GateIdentityService>();
builder.Services.AddSingleton<AppStoreDatabase>();
var app = builder.Build();
try { await app.Services.GetRequiredService<AppStoreDatabase>().Initialize(); }
catch(Exception error) { Console.Error.WriteLine("Database startup failed: "+error.GetType().Name+" / "+(error is Npgsql.PostgresException pg?pg.SqlState:"")+" / "+error.StackTrace?.Split('\n').FirstOrDefault(line=>line.Contains("AppStore.Web"))); throw new InvalidOperationException("PostgreSQL could not initialize. Check the selected environment, database name and secure connection settings. No browser-storage fallback is used."); }
var files = new PhysicalFileProvider(Path.Combine(directory, "wwwroot"));
app.Use(async (context,next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Cache-Control"] = "no-store";
    context.Response.Headers["X-Correlation-ID"] = context.TraceIdentifier;
    try { await next(); }
    catch(GateException e) { context.Response.StatusCode=e.Status;await context.Response.WriteAsJsonAsync(new { code=e.Code,message=e.Message,correlationId=context.TraceIdentifier }); }
    catch(JsonException) { context.Response.StatusCode=400;await context.Response.WriteAsJsonAsync(new {code="invalid_input",message="Check the submitted information.",correlationId=context.TraceIdentifier}); }
    catch(Exception) { context.Response.StatusCode=503;await context.Response.WriteAsJsonAsync(new {code="service_unavailable",message="The request could not be saved. Please try again or contact support.",correlationId=context.TraceIdentifier}); }
});
app.Use(async (context,next) =>
{
    if(context.Request.Path=="/runtime-config.js")
    {
        context.Response.ContentType="application/javascript";
        await context.Response.WriteAsync("window.ARCELIK_RUNTIME="+JsonSerializer.Serialize(new {environment,authMode,testUsers=testLogin?app.Services.GetRequiredService<AppStoreDatabase>().People:[]},new JsonSerializerOptions(JsonSerializerDefaults.Web))+";");return;
    }
    await next();
});
app.UseDefaultFiles(new DefaultFilesOptions {FileProvider=files});
app.UseStaticFiles(new StaticFileOptions {FileProvider=files});
app.MapGet("/healthz",async(AppStoreDatabase db)=>await db.Healthy()?Results.Json(new {status="ok",database="connected",environment}):Results.Json(new {status="unavailable",database="disconnected",environment},statusCode:503));
var api=app.MapGroup("/api");
api.AddEndpointFilter(async(invocation,next)=>
{
    var context=invocation.HttpContext;var db=context.RequestServices.GetRequiredService<AppStoreDatabase>();
    if(!HttpMethods.IsGet(context.Request.Method))
    {
        if(!context.Request.HasJsonContentType() && !((context.Request.Path=="/api/experience/packages" || context.Request.Path=="/api/experience/files") && context.Request.HasFormContentType))return Results.StatusCode(415);
        var origin=context.Request.Headers.Origin.ToString();
        if(!originPolicy.Allows(origin,context.Request.Headers["Sec-Fetch-Site"].ToString(),context.Request.Scheme,context.Request.Host.ToString()))
            throw new GateException(403,"origin_not_allowed","This save was blocked because the website address does not match the configured public address. Please contact support.");
    }
    AppUser user;
    if(testLogin)
    {
        // Query selection is allowed only for Test navigation, never Production.
        if((context.Request.Path.StartsWithSegments("/api/open") || context.Request.Path=="/api/experience/open" || (HttpMethods.IsGet(context.Request.Method)&&context.Request.Path.StartsWithSegments("/api/experience/files"))))
        {
            user=db.People.SingleOrDefault(p=>p.Id==context.Request.Query["testUser"])??throw new GateException(401,"test_account_required","Choose a Test account.");
        }
        else user=db.Person(context);
    }
    else
    {
        user=context.RequestServices.GetRequiredService<GateIdentityService>().Read(context);
        await db.RegisterVerifiedUser(user);
    }
    context.Items["user"]=user;return await next(invocation);
});
static AppUser User(HttpContext c)=>(AppUser)c.Items["user"]!;
api.MapGet("/state",async(HttpContext c,AppStoreDatabase db)=>Results.Json(await db.Read(User(c))));
api.MapGet("/me",async(HttpContext c,AppStoreDatabase db)=>Results.Json((await db.Read(User(c)))["profile"]));
api.MapGet("/apps",async(HttpContext c,AppStoreDatabase db)=>
{
    var state=await db.Read(User(c));IEnumerable<JsonObject> apps=state["apps"]!.AsArray().OfType<JsonObject>().Where(a=>AppStoreDatabase.Eligible(a,User(c)));
    foreach(var key in new[]{"tab","category","platform","search"})
    {
        var value=c.Request.Query[key].ToString();if(string.IsNullOrEmpty(value))continue;
        apps=key switch{
            "tab"=>apps.Where(a=>a["tab"]!.GetValue<string>()==value),
            "category"=>apps.Where(a=>a["categories"]!.AsArray().Any(v=>v!.GetValue<string>()==value)),
            "platform"=>apps.Where(a=>a["platforms"]!.AsArray().Any(v=>v!.GetValue<string>()==value)),
            _=>apps.Where(a=>(a["name"]+" "+a["shortDescription"]+" "+a["description"]).Contains(value,StringComparison.OrdinalIgnoreCase))};
    }
    return Results.Json(new JsonArray(apps.Select(a=>a.DeepClone()).ToArray()));
});
api.MapGet("/apps/{id}",async(string id,HttpContext c,AppStoreDatabase db)=>
{
    var state=await db.Read(User(c));var found=state["apps"]!.AsArray().OfType<JsonObject>().SingleOrDefault(a=>a["id"]!.GetValue<string>()==id&&AppStoreDatabase.Eligible(a,User(c)));
    return found is null?Results.NotFound():Results.Json(found);
});
api.MapPost("/admin/commands",async(HttpContext c,AppStoreDatabase db,JsonObject command)=>Results.Json(await db.Change(User(c),command,c.TraceIdentifier)));
api.MapPost("/events/search",async(HttpContext c,AppStoreDatabase db,SearchEvent body)=>{await db.Event(User(c),"Search",body.Term??"",c.TraceIdentifier);return Results.NoContent();});
foreach(var collection in new[]{"links","documents"})
{
    var kind=collection;
    api.MapGet("/open/{id}/"+kind+"/{linkId}",async(string id,string linkId,HttpContext c,AppStoreDatabase db)=>Results.Redirect(await db.Open(User(c),id,kind,linkId,c.TraceIdentifier)));
}
// Approved UI endpoints reuse the existing identity/origin gate and PostgreSQL store.
string? Persona(HttpContext c)=>c.Request.Headers["X-Test-Persona"].FirstOrDefault();
api.MapGet("/experience",async(HttpContext c,AppStoreDatabase db)=>Results.Json(await db.ReadExperience(User(c),Persona(c))));
api.MapPost("/experience/admin",async(HttpContext c,AppStoreDatabase db,JsonObject body)=>{await db.SaveExperience(User(c),Persona(c),body,c.TraceIdentifier);return Results.Json(await db.ReadExperience(User(c),Persona(c)));});
api.MapPost("/experience/personal",async(HttpContext c,AppStoreDatabase db,JsonObject body)=>{await db.ChangePersonal(User(c),Persona(c),body);return Results.Json(await db.ReadExperience(User(c),Persona(c)));});
api.MapPost("/experience/comments",async(HttpContext c,AppStoreDatabase db,JsonObject body)=>{await db.ExperienceComment(User(c),Persona(c),body,c.TraceIdentifier);return Results.Json(await db.ReadExperience(User(c),Persona(c)));});
api.MapPost("/experience/feedback",async(HttpContext c,AppStoreDatabase db,JsonObject body)=>{await db.ExperienceFeedback(User(c),body,c.TraceIdentifier);return Results.Json(new {received=true});});
api.MapPost("/experience/event",async(HttpContext c,AppStoreDatabase db,JsonObject body)=>{await db.Event(User(c),body["key"]?.GetValue<string>()??"activityView",body["entity"]?.GetValue<string>()??"",c.TraceIdentifier);return Results.NoContent();});
api.MapPost("/experience/packages",async(HttpContext c,AppStoreDatabase db)=>{var access=await db.ReadExperience(User(c),Persona(c));if(access["isAdmin"]?.GetValue<bool>()!=true)throw new GateException(403,"admin_required","Admin access is required.");var form=await c.Request.ReadFormAsync();var file=form.Files.GetFile("file")??throw new GateException(400,"apk_required","Choose an APK file.");return Results.Json(await db.UploadApk(User(c),Persona(c),file,c.TraceIdentifier));});
api.MapPost("/experience/files",async(HttpContext c,AppStoreDatabase db)=>{
 var form=await c.Request.ReadFormAsync();var file=form.Files.GetFile("file")??throw new GateException(400,"file_required","Choose a file.");
 return Results.Json(await db.UploadExperienceFile(User(c),Persona(c),file,c.TraceIdentifier));
});
api.MapGet("/experience/files/{id:guid}",async(Guid id,HttpContext c,AppStoreDatabase db)=>{
 var persona=Persona(c);if(environment=="Test"&&c.Request.Query.ContainsKey("persona"))persona=c.Request.Query["persona"];
 var file=await db.ReadExperienceFile(User(c),persona,id);
 c.Response.Headers["X-Content-Type-Options"]="nosniff";
 return Results.File(file.Content,file.Type,file.Type.StartsWith("image/")?null:file.Name,enableRangeProcessing:true);
});
async Task<IResult> OpenExperience(HttpContext c,AppStoreDatabase db){
    var actor=User(c);string? persona=Persona(c);
    if(testLogin&&c.Request.Query.ContainsKey("testUser")){actor=db.People.SingleOrDefault(p=>p.Id==c.Request.Query["testUser"])??throw new GateException(401,"test_account_required","Choose a TEST account.");persona=c.Request.Query["persona"];}
    if(environment=="Test"&&c.Request.Query.ContainsKey("persona"))persona=c.Request.Query["persona"];
    var target=await db.ExperienceOpen(actor,persona,c.Request.Query["resource"].ToString(),c.Request.Query["kind"],c.Request.Query["link"],c.TraceIdentifier);
    if(target.StartsWith("apk:")){var package=await db.ReadApk(target);return Results.File(package.Content,"application/vnd.android.package-archive",package.Name,enableRangeProcessing:true);}
    if(target.StartsWith("/api/experience/files/")&&Guid.TryParse(target[22..],out var fileId)){var file=await db.ReadExperienceFile(actor,persona,fileId);return Results.File(file.Content,file.Type,file.Type.StartsWith("image/")?null:file.Name,enableRangeProcessing:true);}
    return Results.Redirect(target);
}
api.MapGet("/experience/open",OpenExperience);
app.Run();
public sealed record SearchEvent(string? Term);
