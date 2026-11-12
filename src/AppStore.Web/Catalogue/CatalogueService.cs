using System.Text.Json;
using System.Text.Json.Nodes;
using AppStore.Web.Identity;
namespace AppStore.Web.Catalogue;

public sealed class CatalogueService(IConfiguration config)
{
    private readonly JsonArray apps = JsonNode.Parse(File.ReadAllText(
        config["AppStore:CataloguePath"] ?? Path.Combine(AppContext.BaseDirectory, "catalogue.json")))!.AsArray();
    public bool Eligible(JsonObject app, VerifiedEmployee person) => person.CanBrowse &&
        (app["managerOnly"]?.GetValue<bool>() != true || person.IsManager) &&
        app["isActive"]?.GetValue<bool>() == true && app["isArchived"]?.GetValue<bool>() == false &&
        (Strings(app["countries"]).Length == 0 || Strings(app["countries"]).Contains(person.Country, StringComparer.Ordinal)) &&
        Strings(app["employeeTypes"]).Contains(person.Type, StringComparer.Ordinal);
    public JsonArray List(VerifiedEmployee person, string? tab, string? search, string? category)
    {
        IEnumerable<JsonObject> found = apps.OfType<JsonObject>().Where(a => Eligible(a, person));
        if (!string.IsNullOrEmpty(tab)) found = found.Where(a => a["tab"]?.GetValue<string>() == tab);
        if (!string.IsNullOrEmpty(search)) found = found.Where(a =>
            string.Join(' ', new[] { a["name"], a["shortDescription"], a["description"] }.Select(x => x?.GetValue<string>()))
                .Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(category)) found = found.Where(a => Strings(a["categories"]).Contains(category));
        return new JsonArray(found.Select(a => Public(a, person)).Cast<JsonNode>().ToArray());
    }
    public JsonObject Get(string id, VerifiedEmployee person)
    {
        var app = apps.OfType<JsonObject>().SingleOrDefault(a => a["id"]?.GetValue<string>() == id && Eligible(a, person));
        return app ?? throw new GateException(404, "application_unavailable", "This application is unavailable for your profile.");
    }
    public JsonObject Public(JsonObject app, VerifiedEmployee person)
    {
        var result = (JsonObject)app.DeepClone();
        foreach (var collection in new[] { "links", "documents" })
        {
            var entries = new JsonArray();
            foreach (var entry in app[collection]?.AsArray().OfType<JsonObject>() ?? [])
            {
                if (entry["isActive"]?.GetValue<bool>() == false) continue;
                var country = entry["country"]?.GetValue<string>();
                if (!string.IsNullOrEmpty(country) && country != person.Country) continue;
                var copy = (JsonObject)entry.DeepClone();
                var id = entry["id"]!.GetValue<string>();
                copy["url"] = ""; // Destination is resolved and authorized by the backend, not browser input.
                copy["openPath"] = $"/api/apps/{Uri.EscapeDataString(app["id"]!.GetValue<string>())}/{collection}/{Uri.EscapeDataString(id)}/open";
                entries.Add(copy);
            }
            result[collection] = entries;
        }
        return result;
    }
    public string Resolve(string appId, string collection, string linkId, VerifiedEmployee person)
    {
        var app = Get(appId, person);
        var link = app[collection]?.AsArray().OfType<JsonObject>().SingleOrDefault(l => l["id"]?.GetValue<string>() == linkId);
        if (link is null || link["isActive"]?.GetValue<bool>() == false || link["pending"]?.GetValue<bool>() == true)
            throw new GateException(404, "link_unavailable", "The download address has not been added yet.");
        var country = link["country"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(country) && country != person.Country)
            throw new GateException(404, "link_unavailable", "This link is unavailable for your profile.");
        var value = link["url"]?.GetValue<string>();
        if (collection == "documents" && value == "guide.html") return "/guide.html";
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo))
            throw new GateException(400, "unsafe_destination", "This destination is unavailable.");
        return uri.AbsoluteUri;
    }
    private static string[] Strings(JsonNode? node) => node?.AsArray().Select(x => x!.GetValue<string>()).ToArray() ?? [];
}
public sealed class ActionLog(IConfiguration config)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    public async Task Append(VerifiedEmployee person, string action, string? appId, string? linkId,
        string? search, string correlationId)
    {
        var path = config["AppStore:ActionLogPath"] ?? "/app/data/actions.jsonl";
        var row = JsonSerializer.Serialize(new { id = Guid.NewGuid(), employeeId = person.EmployeeId,
            action, applicationId = appId, linkId, searchTerm = search, occurredAt = DateTimeOffset.UtcNow,
            outcome = "requested", correlationId });
        await gate.WaitAsync();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            await File.AppendAllTextAsync(path, row + "\n");
        }
        finally { gate.Release(); }
    }
}
