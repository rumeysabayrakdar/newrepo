using System.IO.Compression;
using System.Text.Json.Nodes;
using AppStore.Web.Identity;
using Npgsql;
using NpgsqlTypes;

namespace AppStore.Web.Database;

public sealed partial class AppStoreDatabase
{
    const int MaxExperienceFileBytes = 10 * 1024 * 1024;

    static bool IsExperienceFile(string url) =>
        url.StartsWith("/api/experience/files/", StringComparison.Ordinal)
        && Guid.TryParse(url[22..], out _);

    public async Task<JsonObject> UploadExperienceFile(
        AppUser actor,
        string? persona,
        IFormFile file,
        string correlation
    )
    {
        await using var c = await source.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        var (legacy, _) = await Load(c, tx, false);
        var (model, _) = await ExperienceLoad(c, tx);
        var (_, admin, _) = ExperienceIdentity(actor, legacy, model, persona);
        if (!admin)
            throw Error(403, "admin_required", "Admin access is required.");
        var name = Path.GetFileName(file.FileName);
        var ext = Path.GetExtension(name).ToLowerInvariant();
        var types = new Dictionary<string, string>
        {
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".webp", "image/webp" },
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        };
        if (
            file.Length is < 1 or > MaxExperienceFileBytes
            || name.Length > 150
            || name.Any(char.IsControl)
            || !types.TryGetValue(ext, out var type)
        )
            throw Error(400, "invalid_file", "Choose an image or office/PDF document up to 10 MB.");
        await using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        bool Starts(params byte[] signature) => bytes.AsSpan().StartsWith(signature);
        var valid = ext switch
        {
            ".png" => Starts(137, 80, 78, 71, 13, 10, 26, 10),
            ".jpg" or ".jpeg" => Starts(255, 216, 255),
            ".webp" => Starts(82, 73, 70, 70)
                && bytes.Length > 12
                && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            ".pdf" => Starts(37, 80, 68, 70, 45),
            ".doc" or ".xls" => Starts(208, 207, 17, 224, 161, 177, 26, 225),
            ".docx" or ".xlsx" => Starts(80, 75, 3, 4),
            _ => false,
        };
        if (valid && ext is ".docx" or ".xlsx")
            try
            {
                using var zip = new ZipArchive(new MemoryStream(bytes));
                valid =
                    zip.GetEntry("[Content_Types].xml") is not null
                    && zip.Entries.Any(e =>
                        e.FullName.StartsWith(
                            ext == ".docx" ? "word/" : "xl/",
                            StringComparison.Ordinal
                        )
                    );
            }
            catch (InvalidDataException)
            {
                valid = false;
            }
        if (!valid)
            throw Error(400, "invalid_file", "The file content does not match its extension.");
        var id = Guid.NewGuid();
        await using var cmd = new NpgsqlCommand(
            "INSERT INTO appstore.experience_files(id,file_name,content_type,content,uploaded_by) VALUES(@id,@n,@t,@b,@u)",
            c,
            tx
        );
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("n", name);
        cmd.Parameters.AddWithValue("t", type);
        cmd.Parameters.AddWithValue("b", NpgsqlDbType.Bytea, bytes);
        cmd.Parameters.AddWithValue("u", actor.Id);
        await cmd.ExecuteNonQueryAsync();
        await WriteEvent(c, tx, actor, "admin", "fileUploaded", name, correlation, null, null);
        await tx.CommitAsync();
        return new JsonObject
        {
            ["id"] = id.ToString(),
            ["url"] = "/api/experience/files/" + id,
            ["name"] = name,
            ["type"] = type,
            ["size"] = bytes.Length,
        };
    }

    public async Task<(byte[] Content, string Type, string Name)> ReadExperienceFile(
        AppUser actor,
        string? persona,
        Guid id
    )
    {
        await using var c = await source.OpenConnectionAsync();
        await using var tx = await c.BeginTransactionAsync();
        var (legacy, _) = await Load(c, tx, false);
        var (model, _) = await ExperienceLoad(c, tx);
        var (profile, admin, _) = ExperienceIdentity(actor, legacy, model, persona);
        var url = "/api/experience/files/" + id;
        bool References(JsonNode? n) =>
            n switch
            {
                JsonValue v => v.TryGetValue<string>(out var text) && text == url,
                JsonArray a => a.Any(References),
                JsonObject o => o.Any(p => References(p.Value)),
                _ => false,
            };
        var allowed =
            admin
            || Text(model, "logo") == url
            || Arr(model["apps"])
                .OfType<JsonObject>()
                .Any(a => ExperienceActive(a) && ExperienceVisible(a, profile) && References(a))
            || Arr(model["webEntries"])
                .OfType<JsonObject>()
                .Append(Obj(model["webPortal"]))
                .Any(a => ExperienceVisible(a, profile) && References(a));
        if (!allowed)
            throw Error(404, "file_unavailable", "File unavailable.");
        await using var cmd = new NpgsqlCommand(
            "SELECT content,content_type,file_name FROM appstore.experience_files WHERE id=@id",
            c,
            tx
        );
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw Error(404, "file_unavailable", "File unavailable.");
        return ((byte[])reader[0], reader.GetString(1), reader.GetString(2));
    }
}
