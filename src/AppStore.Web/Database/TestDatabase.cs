using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;
using AppStore.Web.Identity;

namespace AppStore.Web.Database;

public sealed record TestPerson(string Id, string Name, string Country, string Type, bool IsManager);
public sealed class TestDatabase : IAsyncDisposable
{
    private readonly NpgsqlDataSource source;
    private readonly IConfiguration config;
    private readonly JsonObject seed;
    public TestPerson[] People { get; }
    public TestDatabase(IConfiguration config)
    {
        this.config = config;
        var connection = config.GetConnectionString("AppStore");
        if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("The Test database connection is missing. Configure ConnectionStrings__AppStore securely.");
        var cs = new NpgsqlConnectionStringBuilder(connection) { IncludeErrorDetail = false };
        var passwordFile = config["Database:PasswordFile"];
        if (!string.IsNullOrEmpty(passwordFile)) cs.Password = File.ReadAllText(passwordFile).Trim();
        var expected = config["TestDatabase:Name"];
        if (string.IsNullOrWhiteSpace(expected) || cs.Database != expected)
            throw new InvalidOperationException("The configured database does not match TestDatabase__Name. Startup was blocked.");
        source = NpgsqlDataSource.Create(cs.ConnectionString);
        var document = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "test-seed.json")))!.AsObject();
        seed = document["state"]!.AsObject();
        People = document["personas"]!.AsArray().Select(p => new TestPerson(
            Text(p!, "id"), Text(p!, "name"), Text(p!, "country"), Text(p!, "type"), p!["isManager"]?.GetValue<bool>() == true)).ToArray();
    }
    public async Task Initialize()
    {
        await using var connection = await source.OpenConnectionAsync();
        await using var tx = await connection.BeginTransactionAsync();
        await Exec(connection, tx, "SELECT pg_advisory_xact_lock(732019468)");
        var actual = (string)(await new NpgsqlCommand("SELECT current_database()", connection, tx).ExecuteScalarAsync())!;
        if (actual != config["TestDatabase:Name"]) throw new InvalidOperationException("The connected database is not the configured Test database.");
        var assembly = typeof(TestDatabase).Assembly;
        using var stream = assembly.GetManifestResourceStream("AppStore.Web.Database.schema.sql")!;
        using var reader = new StreamReader(stream);
        await Exec(connection, tx, await reader.ReadToEndAsync());
        var environment = await new NpgsqlCommand("SELECT value FROM appstore_test.metadata WHERE key='environment'", connection, tx).ExecuteScalarAsync();
        if (environment is not null && environment.ToString() != "Test") throw new InvalidOperationException("Database environment marker is not Test.");
        await Exec(connection, tx, "INSERT INTO appstore_test.metadata(key,value) VALUES ('environment','Test'),('schema_version','1') ON CONFLICT DO NOTHING");
        var initial = (JsonObject)seed.DeepClone(); initial.Remove("events"); initial.Remove("_revision");
        await using var insert = new NpgsqlCommand("INSERT INTO appstore_test.catalogue_state(id,payload) VALUES (1,@data) ON CONFLICT DO NOTHING", connection, tx);
        insert.Parameters.AddWithValue("data", NpgsqlDbType.Jsonb, initial.ToJsonString());
        await insert.ExecuteNonQueryAsync();
        await tx.CommitAsync();
    }
    public TestPerson Person(HttpContext context)
    {
        var values = context.Request.Headers["X-Test-Persona"];
        if (values.Count != 1) throw Error(401, "test_profile_required", "Select a Test employee profile.");
        return People.SingleOrDefault(p => p.Id == values[0]) ?? throw Error(401, "unknown_test_profile", "This Test employee does not exist.");
    }
    public async Task<JsonObject> Read(TestPerson person)
    {
        await using var connection = await source.OpenConnectionAsync();
        await using var tx = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var (state, revision) = await Load(connection, tx, false);
        var result = await PublicState(connection, tx, state, revision, person);
        await tx.CommitAsync();
        return result;
    }
    public async Task<JsonObject> Change(TestPerson person, JsonObject command, string correlationId)
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
                if (changed.Length != 1 || !People.Any(p => p.Id == changed[0])) throw Error(400, "invalid_assignment", "Select one existing Test employee.");
                var id = changed[0]; var grant = action == "Admin access granted";
                if (grant == admins.Contains(id)) throw Error(409, "assignment_conflict", "Admin access has already changed.");
                if (!grant && admins.Length == 1) throw Error(409, "last_admin", "At least one Admin must remain.");
                state["admins"] = Array(grant ? admins.Append(id) : admins.Where(a => a != id));
                entity = People.Single(p => p.Id == id).Name; break;
            }
            default: throw Error(400, "unknown_action", "This admin action is not supported.");
        }
        await using var update = new NpgsqlCommand("UPDATE appstore_test.catalogue_state SET payload=@data, revision=revision+1, updated_at=now() WHERE id=1", connection, tx);
        update.Parameters.AddWithValue("data", NpgsqlDbType.Jsonb, state.ToJsonString());
        await update.ExecuteNonQueryAsync();
        await WriteEvent(connection, tx, person, "admin", action, entity, correlationId, before, state);
        var result = await PublicState(connection, tx, state, revision + 1, person);
        await tx.CommitAsync(); return result;
    }
    public async Task Event(TestPerson person, string action, string entity, string correlationId)
    {
        if (entity.Length > 200) throw Error(400, "invalid_event", "Search text must contain at most 200 characters.");
        await using var connection = await source.OpenConnectionAsync(); await using var tx = await connection.BeginTransactionAsync();
        await WriteEvent(connection, tx, person, "user", action, entity, correlationId, null, null);
        await tx.CommitAsync();
    }
    public async Task<string> Open(TestPerson person, string appId, string collection, string linkId, string correlationId)
    {
        await using var connection = await source.OpenConnectionAsync(); await using var tx = await connection.BeginTransactionAsync();
        var (state, _) = await Load(connection, tx, false);
        var app = state["apps"]!.AsArray().OfType<JsonObject>().SingleOrDefault(a => Text(a, "id") == appId && Eligible(a, person));
        var link = app?[collection]?.AsArray().OfType<JsonObject>().SingleOrDefault(l => Text(l, "id") == linkId);
        if (link is null || link["pending"]?.GetValue<bool>() == true || link["isActive"]?.GetValue<bool>() == false ||
            (!string.IsNullOrEmpty(Text(link, "country")) && Text(link, "country") != person.Country)) throw Error(404, "link_unavailable", "This application or link is unavailable for your profile.");
        var url = Text(link, "url");
        if (!(collection == "documents" && url == "guide.html") && !SafeUrl(url)) throw Error(400, "unsafe_url", "This destination is unavailable.");
        await WriteEvent(connection, tx, person, "user", collection == "links" ? "Application opened" : "Document opened", Text(app!, "name"), correlationId, null, new JsonObject { ["applicationId"] = appId, ["linkId"] = linkId });
        await tx.CommitAsync(); return url == "guide.html" ? "/guide.html" : url;
    }
    public static bool Eligible(JsonObject app, TestPerson person) => app["isActive"]?.GetValue<bool>() == true && app["isArchived"]?.GetValue<bool>() == false &&
        (Strings(app["countries"]).Length == 0 || Strings(app["countries"]).Contains(person.Country)) && Strings(app["employeeTypes"]).Contains(person.Type) &&
        (app["managerOnly"]?.GetValue<bool>() != true || person.IsManager);
    private async Task<JsonObject> PublicState(NpgsqlConnection connection, NpgsqlTransaction tx, JsonObject state, long revision, TestPerson person)
    {
        var result = (JsonObject)state.DeepClone(); var admin = Strings(state["admins"]).Contains(person.Id);
        if (!admin) result["apps"] = new JsonArray(result["apps"]!.AsArray().OfType<JsonObject>().Where(a => Eligible(a, person)).Select(a => a.DeepClone()).ToArray());
        foreach (var app in result["apps"]!.AsArray().OfType<JsonObject>())
            foreach (var collection in new[] { "links", "documents" })
            {
                var entries = new JsonArray();
                foreach (var link in app[collection]?.AsArray().OfType<JsonObject>() ?? [])
                {
                    if (!admin && (link["isActive"]?.GetValue<bool>() == false || (!string.IsNullOrEmpty(Text(link, "country")) && Text(link, "country") != person.Country))) continue;
                    var copy = (JsonObject)link.DeepClone();
                    copy["openPath"] = $"/api/test/open/{Uri.EscapeDataString(Text(app, "id"))}/{collection}/{Uri.EscapeDataString(Text(link, "id"))}?persona={Uri.EscapeDataString(person.Id)}";
                    entries.Add(copy);
                }
                app[collection] = entries;
            }
        var events = new JsonArray();
        if (admin)
        {
            await using var cmd = new NpgsqlCommand("SELECT payload::text FROM (SELECT occurred_at,payload FROM appstore_test.action_logs UNION ALL SELECT occurred_at,payload FROM appstore_test.admin_audit_logs) e ORDER BY occurred_at DESC LIMIT 200", connection, tx);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) events.Insert(0, JsonNode.Parse(reader.GetString(0)));
        }
        result["events"] = events; result["_revision"] = revision;
        result["profile"] = new JsonObject { ["id"] = person.Id, ["name"] = person.Name, ["country"] = person.Country, ["type"] = person.Type, ["isManager"] = person.IsManager };
        return result;
    }
    private static async Task<(JsonObject, long)> Load(NpgsqlConnection connection, NpgsqlTransaction tx, bool forUpdate)
    {
        await using var cmd = new NpgsqlCommand("SELECT payload::text,revision FROM appstore_test.catalogue_state WHERE id=1" + (forUpdate ? " FOR UPDATE" : ""), connection, tx);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) throw Error(503, "database_not_initialized", "The Test database has not been initialized.");
        return (JsonNode.Parse(reader.GetString(0))!.AsObject(), reader.GetInt64(1));
    }
    private static async Task WriteEvent(NpgsqlConnection connection, NpgsqlTransaction tx, TestPerson person, string kind, string action, string entity, string correlationId, JsonObject? before, JsonObject? after)
    {
        var id = Guid.NewGuid(); var now = DateTimeOffset.UtcNow;
        var data = new JsonObject { ["id"] = id.ToString(), ["time"] = now.ToString("O"), ["kind"] = kind, ["actorId"] = person.Id, ["actor"] = person.Name,
            ["action"] = action, ["entity"] = entity, ["correlationId"] = correlationId, ["before"] = before?.DeepClone(), ["after"] = after?.DeepClone() };
        var table = kind == "admin" ? "admin_audit_logs" : "action_logs";
        await using var cmd = new NpgsqlCommand($"INSERT INTO appstore_test.{table}(id,occurred_at,actor_id,action,entity,payload) VALUES (@id,@time,@actor,@action,@entity,@data)", connection, tx);
        cmd.Parameters.AddWithValue("id", id); cmd.Parameters.AddWithValue("time", now); cmd.Parameters.AddWithValue("actor", person.Id);
        cmd.Parameters.AddWithValue("action", action); cmd.Parameters.AddWithValue("entity", entity); cmd.Parameters.AddWithValue("data", NpgsqlDbType.Jsonb, data.ToJsonString());
        await cmd.ExecuteNonQueryAsync();
    }
    private void ValidateApp(JsonObject app, string[] categories)
    {
        foreach (var field in new[] { "name", "shortDescription", "description", "version" })
            if (Text(app, field).Length is < 1 or > 10000) throw Error(400, "invalid_app", "Name, descriptions and version are required.");
        if (Text(app, "tab") is not ("arcelik" or "other") || Strings(app["categories"]).Length == 0 || Strings(app["categories"]).Except(categories).Any() ||
            Strings(app["employeeTypes"]).Length == 0 || Strings(app["employeeTypes"]).Except(new[] { "White-collar", "Blue-collar" }).Any() || Strings(app["platforms"]).Length == 0 ||
            Strings(app["platforms"]).Except(new[] { "Web", "iOS", "Android", "Windows" }).Any() || Strings(app["countries"]).Except(People.Select(p => p.Country).Concat(new[] { "Romania", "Germany", "South Africa", "Pakistan" })).Any())
            throw Error(400, "invalid_targeting", "Select valid categories, countries, employee types and platforms.");
        if (app["isActive"] is not JsonValue || app["isArchived"] is not JsonValue) throw Error(400, "invalid_status", "Active and archive flags are required.");
        _ = app["isActive"]!.GetValue<bool>(); _ = app["isArchived"]!.GetValue<bool>();
        if (app["managerOnly"] is not null) _ = app["managerOnly"]!.GetValue<bool>();
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
