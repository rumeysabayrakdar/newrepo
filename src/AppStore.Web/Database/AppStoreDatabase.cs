using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;
using AppStore.Web.Identity;

namespace AppStore.Web.Database;

public sealed class AppStoreDatabase : IAsyncDisposable
{
    private readonly NpgsqlDataSource source;
    private readonly IConfiguration config;
    private readonly JsonObject seed;
    public AppUser[] People { get; }
    private readonly string environment;
    public AppStoreDatabase(IConfiguration config)
    {
        this.config = config;
        environment = config["AppStore:Environment"]!;
        var connection = config.GetConnectionString("AppStore");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("The PostgreSQL connection is missing. Ask your hosting administrator to configure it securely.");
        var cs = new NpgsqlConnectionStringBuilder(connection) { IncludeErrorDetail = false };
        var passwordFile = config["Database:PasswordFile"];
        if (!string.IsNullOrEmpty(passwordFile)) cs.Password = File.ReadAllText(passwordFile).Trim();
        var expected = config["Database:Name"];
        if (string.IsNullOrWhiteSpace(expected) || cs.Database != expected)
            throw new InvalidOperationException("The database name does not match the selected environment. Startup was blocked.");
        cs["GSS Encryption Mode"] = "Disable"; // Password-based PostgreSQL; TLS settings are preserved.
        source = NpgsqlDataSource.Create(cs.ConnectionString);
        var document = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "catalogue-seed.json")))!.AsObject();
        seed = document["state"]!.AsObject();
        People = environment == "Test" ? config.GetSection("Authentication:TestUsers").GetChildren().Select(u=>new AppUser(u["Id"]!,u["Name"]!)).ToArray() : [];

    }
    public async Task Initialize()
    {
        await using var connection = await source.OpenConnectionAsync();
        await using var tx = await connection.BeginTransactionAsync();
        await Exec(connection, tx, "SELECT pg_advisory_xact_lock(732019468)");
        var actual = (string)(await new NpgsqlCommand("SELECT current_database()", connection, tx).ExecuteScalarAsync())!;
        if (actual != config["Database:Name"]) throw new InvalidOperationException("The database does not match the selected environment.");
        var assembly = typeof(AppStoreDatabase).Assembly;
        using var stream = assembly.GetManifestResourceStream("AppStore.Web.Database.schema.sql")!;
        using var reader = new StreamReader(stream);
        await Exec(connection, tx, await reader.ReadToEndAsync());
        var marker = await new NpgsqlCommand("SELECT value FROM appstore.metadata WHERE key='environment'", connection, tx).ExecuteScalarAsync();
        if (marker is not null && marker.ToString() != environment) throw new InvalidOperationException("Database environment marker does not match appsettings.");
        var legacy = await new NpgsqlCommand("SELECT to_regclass('appstore_test.catalogue_state')::text", connection, tx).ExecuteScalarAsync();
        if (legacy is not null && legacy is not DBNull)
        {
            if (environment != "Test") throw new InvalidOperationException("A legacy Test database cannot be used for Production.");
            await Exec(connection, tx, "INSERT INTO appstore.catalogue_state SELECT * FROM appstore_test.catalogue_state ON CONFLICT DO NOTHING; INSERT INTO appstore.action_logs SELECT * FROM appstore_test.action_logs ON CONFLICT DO NOTHING; INSERT INTO appstore.admin_audit_logs SELECT * FROM appstore_test.admin_audit_logs ON CONFLICT DO NOTHING;");
        }
        await using (var mark = new NpgsqlCommand("INSERT INTO appstore.metadata(key,value) VALUES ('environment',@environment),('schema_version','2') ON CONFLICT DO NOTHING", connection, tx))
        { mark.Parameters.AddWithValue("environment", environment); await mark.ExecuteNonQueryAsync(); }
        var initial = (JsonObject)seed.DeepClone(); initial.Remove("events"); initial.Remove("_revision");
        initial["admins"] = environment == "Test" ? Array(config.GetSection("Authentication:TestAdminIds").Get<string[]>() ?? []) : new JsonArray();
        await using var insert = new NpgsqlCommand("INSERT INTO appstore.catalogue_state(id,payload) VALUES (1,@data) ON CONFLICT DO NOTHING", connection, tx);
        insert.Parameters.AddWithValue("data", NpgsqlDbType.Jsonb, initial.ToJsonString());
        await insert.ExecuteNonQueryAsync();
        foreach (var user in People) await Register(connection, tx, user);
        await tx.CommitAsync();
        await UpgradeLegacy();
    }
    public AppUser Person(HttpContext context)
    {
        var values = context.Request.Headers["X-Test-User"];
        if (values.Count != 1) throw Error(401, "test_profile_required", "Choose a Test account.");
        return People.SingleOrDefault(p => p.Id == values[0]) ?? throw Error(401, "unknown_test_profile", "This Test account does not exist.");
    }
    private static async Task Register(NpgsqlConnection connection, NpgsqlTransaction tx, AppUser user)
    {
        await using var command = new NpgsqlCommand("INSERT INTO appstore.users(id,name,internal_user_id,email) VALUES (@id,@name,@internal,@email) ON CONFLICT(id) DO UPDATE SET name=EXCLUDED.name,internal_user_id=EXCLUDED.internal_user_id,email=EXCLUDED.email",connection,tx);
        command.Parameters.AddWithValue("id",user.Id);command.Parameters.AddWithValue("name",user.Name);
        command.Parameters.AddWithValue("internal",(object?)user.InternalUserId??DBNull.Value);command.Parameters.AddWithValue("email",(object?)user.Email??DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }
    public async Task RegisterVerifiedUser(AppUser user)
    {
        await using var connection=await source.OpenConnectionAsync();await using var tx=await connection.BeginTransactionAsync();
        await Register(connection,tx,user);
        var initial=config.GetSection("Authentication:InitialAdminEmployeeIds").Get<string[]>()??[];
        if(environment=="Production"&&initial.Contains(user.Id))
        {
            var(state,revision)=await Load(connection,tx,true);
            var bootstrapped=await new NpgsqlCommand("SELECT 1 FROM appstore.metadata WHERE key='bootstrap:'||@id",connection,tx){Parameters={new("id",user.Id)}}.ExecuteScalarAsync();
            if(bootstrapped is null)
            {
                var before=(JsonObject)state.DeepClone();
                if(!Strings(state["admins"]).Contains(user.Id))state["admins"]!.AsArray().Add(user.Id);
                await using var save=new NpgsqlCommand("UPDATE appstore.catalogue_state SET payload=@data,revision=revision+1 WHERE id=1",connection,tx);save.Parameters.AddWithValue("data",NpgsqlDbType.Jsonb,state.ToJsonString());await save.ExecuteNonQueryAsync();
                await WriteEvent(connection,tx,user,"admin","Initial Admin granted",user.Id,Guid.NewGuid().ToString(),before,state);
                await using var mark=new NpgsqlCommand("INSERT INTO appstore.metadata(key,value) VALUES ('bootstrap:'||@id,'complete') ON CONFLICT DO NOTHING",connection,tx);mark.Parameters.AddWithValue("id",user.Id);await mark.ExecuteNonQueryAsync();
            }
        }
        await tx.CommitAsync();
    }
    private async Task UpgradeLegacy()
    {
        await using var connection=await source.OpenConnectionAsync();await using var tx=await connection.BeginTransactionAsync();
        await Exec(connection,tx,"SELECT pg_advisory_xact_lock(732019468)");
        if(await new NpgsqlCommand("SELECT 1 FROM appstore.metadata WHERE key='unrestricted-catalogue-v2'",connection,tx).ExecuteScalarAsync() is not null)return;
        var(state,revision)=await Load(connection,tx,true);var before=(JsonObject)state.DeepClone();
        foreach(var a in state["apps"]!.AsArray().OfType<JsonObject>())
        {
            a.Remove("countries");a.Remove("employeeTypes");a.Remove("managerOnly");
            var samples=new Dictionary<string,string>{{"connect","Arçelik Connect"},{"hr","HR Portal"},{"one","One Arçelik"},{"expense","Expense Manager"},{"learn","Learning Hub"},{"service","Service Partner"},{"well","Wellbeing"},{"teams","Microsoft Teams"},{"outlook","Microsoft Outlook"},{"miro","Miro"}};
            if(a["isTestScenario"]?.GetValue<bool>()==true||(samples.TryGetValue(Text(a,"id"),out var sample)&&sample==Text(a,"name")))a["isArchived"]=true;
            foreach(var link in a["links"]!.AsArray().OfType<JsonObject>())link.Remove("country");
        }
        if(environment=="Test")
        {
            var ids=People.Select(u=>u.Id).ToArray();var admins=Strings(state["admins"]).Intersect(ids).ToArray();
            state["admins"]=Array(admins.Length>0?admins:config.GetSection("Authentication:TestAdminIds").Get<string[]>()??[]);
        }
        await using var save=new NpgsqlCommand("UPDATE appstore.catalogue_state SET payload=@data,revision=revision+1 WHERE id=1",connection,tx);save.Parameters.AddWithValue("data",NpgsqlDbType.Jsonb,state.ToJsonString());await save.ExecuteNonQueryAsync();
        await WriteEvent(connection,tx,new AppUser("system","System"),"admin","Catalogue model updated","Removed employee targeting",Guid.NewGuid().ToString(),before,state);
        await Exec(connection,tx,"INSERT INTO appstore.metadata(key,value) VALUES ('unrestricted-catalogue-v2','complete')");await tx.CommitAsync();
    }
    public async Task<JsonObject> Read(AppUser person)
    {
        await using var connection = await source.OpenConnectionAsync();
        await using var tx = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var (state, revision) = await Load(connection, tx, false);
        var result = await PublicState(connection, tx, state, revision, person);
        await tx.CommitAsync();
        return result;
    }
    public async Task<JsonObject> Change(AppUser person, JsonObject command, string correlationId)
    {
        await using var connection = await source.OpenConnectionAsync();
        await using var tx = await connection.BeginTransactionAsync();
        var (state, revision) = await Load(connection, tx, true);
        if (!Strings(state["admins"]).Contains(person.Id)) throw Error(403, "admin_required", "Admin access is required.");
        if (command["revision"]?.GetValue<long>() != revision) throw Error(409, "save_conflict", "Someone else changed the catalogue. Refresh and try again.");
        var before = (JsonObject)state.DeepClone();
        var action = Text(command, "action"); var entity = Text(command, "entity");
        var after = command["after"] as JsonObject;
        var apps = state["apps"]!.AsArray(); var categories = Strings(state["categories"]);
        switch (action)
        {
            case "Application created":
            case "Application edited":
            {
                var app = (JsonObject)(after?.DeepClone() ?? throw Error(400, "invalid_app", "Application data is required."));
                ValidateApp(app, categories);
                var id = Text(app, "id");
                if (string.IsNullOrEmpty(id)) { id = Guid.NewGuid().ToString(); app["id"] = id; }
                var index = Find(apps, id);
                if (action == "Application created")
                {
                    if (index >= 0) throw Error(409, "application_exists", "This application already exists.");
                    apps.Add(app);
                }
                else
                {
                    if (index < 0) throw Error(404, "application_missing", "Application not found.");
                    apps[index] = app;
                }
                entity = Text(app, "name"); break;
            }
            case "Application activated": case "Application deactivated":
            case "Application archived": case "Application restored":
            {
                var id = Text(after!, "id"); var index = Find(apps, id);
                if (index < 0) throw Error(404, "application_missing", "Application not found.");
                var app = apps[index]!.AsObject();
                if (action is "Application archived" or "Application restored") app["isArchived"] = action == "Application archived";
                else app["isActive"] = action == "Application activated";
                entity = Text(app, "name"); break;
            }
            case "Category created":
                if (entity.Length is < 1 or > 50 || categories.Any(c => c.Equals(entity, StringComparison.OrdinalIgnoreCase))) throw Error(400, "invalid_category", "Choose a unique category name.");
                state["categories"]!.AsArray().Add(entity); break;
            case "Category renamed":
            {
                var name = Text(after!, "name");
                if (!categories.Contains(entity) || name.Length is < 1 or > 50 || categories.Any(c => c.Equals(name, StringComparison.OrdinalIgnoreCase))) throw Error(400, "invalid_category", "Choose an existing category and a unique new name.");
                state["categories"] = Array(categories.Select(c => c == entity ? name : c));
                foreach (var app in apps.OfType<JsonObject>()) app["categories"] = Array(Strings(app["categories"]).Select(c => c == entity ? name : c));
                break;
            }
            case "Unused category removed":
                if (!categories.Contains(entity) || apps.OfType<JsonObject>().Any(a => Strings(a["categories"]).Contains(entity))) throw Error(409, "category_in_use", "Only unused categories can be removed.");
                state["categories"] = Array(categories.Where(c => c != entity)); break;
            case "Translation edited":
            {
                var parts = entity.Split('.', 2); var value = Text(after!, "value");
                if (parts.Length != 2 || value.Length is < 1 or > 2000 || state["translations"]?[parts[0]]?[parts[1]] is null) throw Error(400, "invalid_translation", "This translation key or value is invalid.");
                state["translations"]![parts[0]]![parts[1]] = value; break;
            }
            case "Admin access granted": case "Admin access revoked":
            {
                var admins = Strings(state["admins"]); var desired = Strings(command["patch"]?["admins"]);
                var changed = admins.Except(desired).Concat(desired.Except(admins)).ToArray();
                if (changed.Length != 1 || !await UserExists(connection,tx,changed[0])) throw Error(400, "invalid_assignment", "Select one existing signed-in user.");
                var id = changed[0]; var grant = action == "Admin access granted";
                if (grant == admins.Contains(id)) throw Error(409, "assignment_conflict", "Admin access has already changed.");
                if (!grant && admins.Length == 1) throw Error(409, "last_admin", "At least one Admin must remain.");
                state["admins"] = Array(grant ? admins.Append(id) : admins.Where(a => a != id));
                entity = id; break;
            }
            default: throw Error(400, "unknown_action", "This admin action is not supported.");
        }
        await using var update = new NpgsqlCommand("UPDATE appstore.catalogue_state SET payload=@data, revision=revision+1, updated_at=now() WHERE id=1", connection, tx);
        update.Parameters.AddWithValue("data", NpgsqlDbType.Jsonb, state.ToJsonString());
        await update.ExecuteNonQueryAsync();
        await WriteEvent(connection, tx, person, "admin", action, entity, correlationId, before, state);
        var result = await PublicState(connection, tx, state, revision + 1, person);
        await tx.CommitAsync(); return result;
    }
    public async Task Event(AppUser person, string action, string entity, string correlationId)
    {
        if (entity.Length > 200) throw Error(400, "invalid_event", "Search text must contain at most 200 characters.");
        await using var connection = await source.OpenConnectionAsync(); await using var tx = await connection.BeginTransactionAsync();
        await WriteEvent(connection, tx, person, "user", action, entity, correlationId, null, null);
        await tx.CommitAsync();
    }
    public async Task<string> Open(AppUser person, string appId, string collection, string linkId, string correlationId)
    {
        await using var connection = await source.OpenConnectionAsync(); await using var tx = await connection.BeginTransactionAsync();
        var (state, _) = await Load(connection, tx, false);
        var app = state["apps"]!.AsArray().OfType<JsonObject>().SingleOrDefault(a => Text(a, "id") == appId && Eligible(a, person));
        var link = app?[collection]?.AsArray().OfType<JsonObject>().SingleOrDefault(l => Text(l, "id") == linkId);
        if (link is null || link["pending"]?.GetValue<bool>() == true || link["isActive"]?.GetValue<bool>() == false) throw Error(404, "link_unavailable", "This application or link is unavailable for your profile.");
        var url = Text(link, "url");
        if (!(collection == "documents" && url == "guide.html") && !SafeUrl(url)) throw Error(400, "unsafe_url", "This destination is unavailable.");
        await WriteEvent(connection, tx, person, "user", collection == "links" ? "Application opened" : "Document opened", Text(app!, "name"), correlationId, null, new JsonObject { ["applicationId"] = appId, ["linkId"] = linkId });
        await tx.CommitAsync(); return url == "guide.html" ? "/guide.html" : url;
    }
    public static bool Eligible(JsonObject app, AppUser person) => app["isActive"]?.GetValue<bool>() == true && app["isArchived"]?.GetValue<bool>() == false;
    private static async Task<bool> UserExists(NpgsqlConnection c,NpgsqlTransaction t,string id){await using var cmd=new NpgsqlCommand("SELECT 1 FROM appstore.users WHERE id=@id",c,t);cmd.Parameters.AddWithValue("id",id);return await cmd.ExecuteScalarAsync() is not null;}
    public async Task<bool> Healthy(){try{await using var c=await source.OpenConnectionAsync();await new NpgsqlCommand("SELECT 1",c).ExecuteScalarAsync();return true;}catch{return false;}}
    private async Task<JsonObject> PublicState(NpgsqlConnection connection, NpgsqlTransaction tx, JsonObject state, long revision, AppUser person)
    {
        var result = (JsonObject)state.DeepClone(); var admin = Strings(state["admins"]).Contains(person.Id);
        if (!admin) result["apps"] = new JsonArray(result["apps"]!.AsArray().OfType<JsonObject>().Where(a => Eligible(a, person)).Select(a => a.DeepClone()).ToArray());
        foreach (var app in result["apps"]!.AsArray().OfType<JsonObject>())
            foreach (var collection in new[] { "links", "documents" })
            {
                var entries = new JsonArray();
                foreach (var link in app[collection]?.AsArray().OfType<JsonObject>() ?? [])
                {
                    if (!admin && (link["isActive"]?.GetValue<bool>() == false)) continue;
                    var copy = (JsonObject)link.DeepClone();
                    copy["openPath"] = $"/api/open/{Uri.EscapeDataString(Text(app, "id"))}/{collection}/{Uri.EscapeDataString(Text(link, "id"))}";
                    entries.Add(copy);
                }
                app[collection] = entries;
            }
        var events = new JsonArray();
        if (admin)
        {
            await using var cmd = new NpgsqlCommand("SELECT payload::text FROM (SELECT occurred_at,payload FROM appstore.action_logs UNION ALL SELECT occurred_at,payload FROM appstore.admin_audit_logs) e ORDER BY occurred_at DESC LIMIT 200", connection, tx);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) events.Insert(0, JsonNode.Parse(reader.GetString(0)));
        }
        result["events"] = events; result["_revision"] = revision;
        result["profile"] = new JsonObject { ["id"] = person.Id, ["name"] = person.Name, ["isAdmin"] = admin };
        var users=new JsonArray();
        if(admin){await using var q=new NpgsqlCommand("SELECT id,name FROM appstore.users ORDER BY name",connection,tx);await using var rows=await q.ExecuteReaderAsync();while(await rows.ReadAsync())users.Add(new JsonObject{["id"]=rows.GetString(0),["name"]=rows.GetString(1)});}
        result["users"]=users;
        return result;
    }
    private static async Task<(JsonObject, long)> Load(NpgsqlConnection connection, NpgsqlTransaction tx, bool forUpdate)
    {
        await using var cmd = new NpgsqlCommand("SELECT payload::text,revision FROM appstore.catalogue_state WHERE id=1" + (forUpdate ? " FOR UPDATE" : ""), connection, tx);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw Error(503, "database_not_initialized", "The Test database has not been initialized.");
        return (JsonNode.Parse(reader.GetString(0))!.AsObject(), reader.GetInt64(1));
    }
    private static async Task WriteEvent(NpgsqlConnection connection, NpgsqlTransaction tx, AppUser person, string kind, string action, string entity, string correlationId, JsonObject? before, JsonObject? after)
    {
        var id = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var data = new JsonObject { ["id"] = id.ToString(), ["time"] = now.ToString("O"), ["kind"] = kind, ["actorId"] = person.Id, ["actor"] = person.Name,
            ["action"] = action, ["entity"] = entity, ["correlationId"] = correlationId, ["before"] = before?.DeepClone(), ["after"] = after?.DeepClone() };
        var table = kind == "admin" ? "admin_audit_logs" : "action_logs";
        await using var cmd = new NpgsqlCommand($"INSERT INTO appstore.{table}(id,occurred_at,actor_id,action,entity,payload) VALUES (@id,@time,@actor,@action,@entity,@data)", connection, tx);
        cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("time", now); cmd.Parameters.AddWithValue("actor", person.Id);
        cmd.Parameters.AddWithValue("action", action); cmd.Parameters.AddWithValue("entity", entity); cmd.Parameters.AddWithValue("data", NpgsqlDbType.Jsonb, data.ToJsonString());
        await cmd.ExecuteNonQueryAsync();
    }
    private void ValidateApp(JsonObject app, string[] categories)
    {
        foreach (var field in new[] { "name", "shortDescription", "description", "version" })
            if (string.IsNullOrWhiteSpace(Text(app,field)) || Text(app,field).Length>10000) throw Error(400, "invalid_app", "Name, descriptions and version are required.");
        if (Text(app,"tab") is not("arcelik" or "other") || Strings(app["categories"]).Length==0 || Strings(app["categories"]).Except(categories).Any() || Strings(app["platforms"]).Length==0 || Strings(app["platforms"]).Except(new[]{"Web","iOS","Android","Windows"}).Any())throw Error(400,"invalid_application","Select valid categories and platforms.");
        if (app["translations"] is not null)
        {
            if (app["translations"] is not JsonObject translations || translations.Any(p => p.Key is not ("en" or "tr")))
                throw Error(400, "invalid_translation", "Application translations support English and Turkish only.");
            foreach (var language in translations)
            {
                if (language.Value is not JsonObject fields || fields.Any(p => p.Key is not ("name" or "shortDescription" or "description")))
                    throw Error(400, "invalid_translation", "Check the translated application fields.");
                foreach (var field in fields)
                {
                    var maximum = field.Key == "name" ? 80 : field.Key == "shortDescription" ? 500 : 10000;
                    if (field.Value is not JsonValue value || !value.TryGetValue<string>(out var text) || text.Length > maximum)
                        throw Error(400, "invalid_translation", "Check the translated text length and format.");
                }
            }
        }
        app.Remove("countries");app.Remove("employeeTypes");app.Remove("managerOnly");
        if (app["isActive"] is not JsonValue || app["isArchived"] is not JsonValue) throw Error(400, "invalid_status", "Active and archive flags are required.");
        _ = app["isActive"]!.GetValue<bool>(); _ = app["isArchived"]!.GetValue<bool>();

        foreach (var collection in new[] { "links", "documents" })
        {
            var entries = app[collection]?.AsArray() ?? throw Error(400, "invalid_links", "Links and documents must be arrays.");
            if (entries.Any(n => n is not JsonObject)) throw Error(400, "invalid_links", "Each link must be an object.");
            var ids = new HashSet<string>();
            foreach (var entry in entries.OfType<JsonObject>())
            {
                var id = Text(entry, "id"); var url = Text(entry, "url"); var pending = entry["pending"]?.GetValue<bool>() == true;
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id) || string.IsNullOrWhiteSpace(Text(entry, collection == "links" ? "label" : "title")) ||
                    (!SafeUrl(url) && !(pending && string.IsNullOrEmpty(url)) && !(collection == "documents" && url == "guide.html")))
                    throw Error(400, "invalid_links", "Use labelled HTTPS destinations or pending empty links.");
            }
        }
        if (app["links"]!.AsArray().Count == 0) throw Error(400, "links_required", "At least one link is required; it may be pending.");
        if (Strings(app["images"]).Any(url => url != "sample-screen.svg" && !SafeUrl(url))) throw Error(400, "invalid_images", "Use HTTPS image addresses.");
    }
    private static bool SafeUrl(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo);
    private static int Find(JsonArray apps, string id) { for (var i = 0; i < apps.Count; i++) if (Text(apps[i]!, "id") == id) return i; return -1; }
    private static string Text(JsonNode node, string field) => node[field]?.GetValue<string>() ?? "";
    private static string[] Strings(JsonNode? node) => node?.AsArray().Select(n => n!.GetValue<string>()).ToArray() ?? [];
    private static JsonArray Array(IEnumerable<string> values) => new(values.Select(v => (JsonNode?)JsonValue.Create(v)).ToArray());
    private static GateException Error(int status, string code, string message) => new(status, code, message);
    private static async Task Exec(NpgsqlConnection c, NpgsqlTransaction t, string sql) { await using var cmd = new NpgsqlCommand(sql, c, t); await cmd.ExecuteNonQueryAsync(); }
    public ValueTask DisposeAsync() => source.DisposeAsync();
}
