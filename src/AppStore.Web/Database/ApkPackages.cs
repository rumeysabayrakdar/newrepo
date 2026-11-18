using System.IO.Compression;
using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;
using AppStore.Web.Identity;
namespace AppStore.Web.Database;
public sealed partial class AppStoreDatabase
{
    public const long MaxApkBytes=100*1024*1024;
    public async Task<JsonObject> UploadApk(AppUser actor,string? persona,IFormFile file,string correlation)
    {
        await using var c=await source.OpenConnectionAsync();await using var tx=await c.BeginTransactionAsync();
        var(legacy,_)=await Load(c,tx,false);var(model,_)=await ExperienceLoad(c,tx);var(_,admin,_)=ExperienceIdentity(actor,legacy,model,persona);
        if(!admin)throw Error(403,"admin_required","Admin access is required.");
        var name=Path.GetFileName(file.FileName);
        if(file.Length is <1 or >MaxApkBytes || name.Length>150 || name.Any(char.IsControl) || !name.EndsWith(".apk",StringComparison.OrdinalIgnoreCase))throw Error(400,"invalid_apk","Choose an APK file up to 100 MB.");
        await using var content=file.OpenReadStream();
        try{using var zip=new ZipArchive(content,ZipArchiveMode.Read,true);if(zip.GetEntry("AndroidManifest.xml") is null)throw Error(400,"invalid_apk","The file does not contain an Android application manifest.");}
        catch(InvalidDataException){throw Error(400,"invalid_apk","The APK archive could not be read.");}
        content.Position=0;var id=Guid.NewGuid();
        await using var cmd=new NpgsqlCommand("INSERT INTO appstore.apk_packages(id,file_name,content,uploaded_by) VALUES(@id,@n,@data,@actor)",c,tx);
        cmd.Parameters.AddWithValue("id",id);cmd.Parameters.AddWithValue("n",name);cmd.Parameters.AddWithValue("actor",actor.Id);cmd.Parameters.Add(new NpgsqlParameter("data",NpgsqlDbType.Bytea){Value=content,Size=(int)file.Length});
        await cmd.ExecuteNonQueryAsync();await WriteEvent(c,tx,actor,"admin","apkUploaded",name,correlation,null,null);await tx.CommitAsync();
        return new JsonObject{["url"]="apk:"+id,["fileName"]=name,["size"]=file.Length};
    }
    // Called only after ExperienceOpen has rechecked the referenced application's eligibility.
    public async Task<(byte[] Content,string Name)> ReadApk(string reference)
    {
        if(!reference.StartsWith("apk:")||!Guid.TryParse(reference[4..],out var id))throw Error(404,"apk_missing","APK unavailable.");
        await using var c=await source.OpenConnectionAsync();await using var cmd=new NpgsqlCommand("SELECT content,file_name FROM appstore.apk_packages WHERE id=@id",c);cmd.Parameters.AddWithValue("id",id);await using var reader=await cmd.ExecuteReaderAsync();if(!await reader.ReadAsync())throw Error(404,"apk_missing","APK unavailable.");return((byte[])reader[0],reader.GetString(1));
    }
}
