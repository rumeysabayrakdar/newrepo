using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Npgsql;
using NpgsqlTypes;
using AppStore.Web.Identity;
namespace AppStore.Web.Database;

// Additive, versioned JSONB repository for the approved catalogue experience.
// Authentication, registered employees and Admin roles remain in the existing store.
public sealed partial class AppStoreDatabase
{
    static JsonObject Obj(JsonNode? n) => n as JsonObject ?? new JsonObject();
    static JsonArray Arr(JsonNode? n) => n as JsonArray ?? new JsonArray();
    static bool Flag(JsonNode n,string key) => n[key]?.GetValue<bool>() == true;
    async Task InitializeExperience()
    {
        await using var c=await source.OpenConnectionAsync(); await using var tx=await c.BeginTransactionAsync();
        await Exec(c,tx,"SELECT pg_advisory_xact_lock(732019469)");
        var exists=await new NpgsqlCommand("SELECT 1 FROM appstore.experience_state WHERE id=1",c,tx).ExecuteScalarAsync();
        if(exists is null)
        {
            var model=JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory,"experience-seed.json")))!.AsObject();
            var (legacy,_)=await Load(c,tx,false);
            var mappings=new Dictionary<string,string>{{"perakende-akademi","pa"},{"servis-akademi","servis"},{"perakende-akademi-ar","ar"},{"okta-verify-china","okta"},{"sirius-tech","sirius"}};
            var migrated=new JsonArray();
            foreach(var old in Arr(legacy["apps"]).OfType<JsonObject>())
            {
                var id=mappings.GetValueOrDefault(Text(old,"id"),Text(old,"id"));
                var a=Arr(model["apps"]).OfType<JsonObject>().FirstOrDefault(x=>Text(x,"id")==id)?.DeepClone().AsObject()??new JsonObject();
                a["id"]=id;a["legacyId"]=Text(old,"id");
                foreach(var lang in new[]{"en","tr"})
                {
                    var translated=Obj(old["translations"]?[lang]);
                    // Preserve existing explicit translations and existing catalogue text.
                    var content=Obj(a[lang]).DeepClone().AsObject();
                    foreach(var pair in new[]{("name","name"),("shortDescription","summary"),("description","description")})
                    {var value=Text(translated,pair.Item1); if(string.IsNullOrWhiteSpace(value)&&lang=="en")value=Text(old,pair.Item1);if(!string.IsNullOrWhiteSpace(value))content[pair.Item2]=value;}
                    a[lang]=content;
                }
                a["version"]=Text(old,"version");a["active"]=Flag(old,"isActive");a["archived"]=Flag(old,"isArchived");a["tab"]=Text(old,"tab");
                a["platforms"]=old["platforms"]?.DeepClone()??new JsonArray();
                var categories=new JsonArray();
                foreach(var cat in Strings(old["categories"]))
                {
                    var found=Arr(model["categories"]).OfType<JsonObject>().FirstOrDefault(x=>Text(x,"en")==cat||Text(x,"tr")==cat||Text(x,"id")==cat);
                    if(found is null){found=new JsonObject{["id"]="category-"+Guid.NewGuid().ToString("N"),["en"]=cat,["tr"]=cat};model["categories"]!.AsArray().Add(found);}categories.Add(Text(found,"id"));
                }
                a["categories"]=categories;a["symbol"]=Text(old,"symbol");a["color"]=Text(old,"color");a["url"]="";
                var links=new JsonArray();foreach(var l in Arr(old["links"]).OfType<JsonObject>()){var link=l.DeepClone().AsObject();link["label"]=new JsonObject{["en"]=Text(l,"label"),["tr"]=Text(l,"label")};link["pending"]=Flag(l,"pending")||l["isActive"]?.GetValue<bool>()==false;links.Add(link);}
                a["links"]=links;a["documents"]=old["documents"]?.DeepClone()??new JsonArray();a["images"]=old["images"]?.DeepClone()??new JsonArray();
                a["labels"]??=new JsonArray();a["comments"]??=new JsonArray();a["image"]??="";a["featured"]??=false;
                migrated.Add(a);
            }
            // New, requested web entries are TEST starter content only.
            if(environment=="Test")foreach(var a in Arr(model["apps"]).OfType<JsonObject>().Where(a=>Text(a,"id")=="connecta"))migrated.Add(a.DeepClone());
            model["apps"]=migrated;
            if(environment!="Test"){model["webEntries"]=new JsonArray();model["testProfiles"]=new JsonArray();model["webPortal"]=new JsonObject{["id"]="behome",["en"]=new JsonObject{["name"]="BeHome",["description"]="Company portal"},["tr"]=new JsonObject{["name"]="BeHome",["description"]="Şirket portalı"},["url"]="https://arcelik.sharepoint.com/sites/BeHome",["symbol"]="b."};}
            if(legacy["branding"]?["logo"] is JsonValue logo)model["logo"]=logo.GetValue<string>();
            foreach(var lang in new[]{"en","tr"})foreach(var pair in new[]{("heroGreeting","greeting"),("heroTitle","title"),("heroAccent","accent"),("heroSubtitle","subtitle")}){var text=Text(Obj(legacy["translations"]?[lang]),pair.Item1);if(text.Length>0)model["copy"]![lang]![pair.Item2]=text;}
            model.Remove("audit");model.Remove("activity");model.Remove("admins");model.Remove("adminAssignments");
            await using var insert=new NpgsqlCommand("INSERT INTO appstore.experience_state(id,payload) VALUES(1,@p)",c,tx);insert.Parameters.AddWithValue("p",NpgsqlDbType.Jsonb,model.ToJsonString());await insert.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync();
    }
    static async Task<(JsonObject Model,long Revision)> ExperienceLoad(NpgsqlConnection c,NpgsqlTransaction tx,bool locked=false)
    {await using var cmd=new NpgsqlCommand("SELECT payload::text,revision FROM appstore.experience_state WHERE id=1"+(locked?" FOR UPDATE":""),c,tx);await using var r=await cmd.ExecuteReaderAsync();if(!await r.ReadAsync())throw Error(503,"experience_missing","Catalogue is not initialized.");return(JsonNode.Parse(r.GetString(0))!.AsObject(),r.GetInt64(1));}
    static async Task StoreExperience(NpgsqlConnection c,NpgsqlTransaction tx,JsonObject model)
    {await using var cmd=new NpgsqlCommand("UPDATE appstore.experience_state SET payload=@p,revision=revision+1,updated_at=now() WHERE id=1",c,tx);cmd.Parameters.AddWithValue("p",NpgsqlDbType.Jsonb,model.ToJsonString());await cmd.ExecuteNonQueryAsync();}
    (JsonObject Profile,bool Admin,string PersonalId) ExperienceIdentity(AppUser actor,JsonObject legacy,JsonObject model,string? persona)
    {
        var admin=Strings(legacy["admins"]).Contains(actor.Id);JsonObject profile=new(){["id"]=actor.Id,["name"]=actor.Name,["employeeId"]=actor.Id,["initials"]=string.Concat(actor.Name.Split(' ').Take(2).Select(x=>x.FirstOrDefault())).ToUpperInvariant(),["synthetic"]=false,["companyCode"]=actor.CompanyCode??"",["country"]=actor.Country??"",["mailGroups"]=new JsonArray((actor.Groups??[]).Select(g=>(JsonNode)JsonValue.Create(g)!).ToArray()),["site"]=actor.Site??""};
        if(!string.IsNullOrEmpty(persona))
        {if(environment!="Test"||!admin)throw Error(403,"test_only","TEST profiles require a TEST administrator.");var p=Arr(model["testProfiles"]).OfType<JsonObject>().SingleOrDefault(x=>Text(x,"id")==persona)??throw Error(404,"profile_missing","TEST profile not found.");profile=p.DeepClone().AsObject();profile["synthetic"]=true;admin=Text(p,"testRole")=="admin";}
        return(profile,admin,actor.Id+(string.IsNullOrEmpty(persona)?"":"::"+persona));
    }
    static bool ExperienceActive(JsonObject a)=>a["active"]?.GetValue<bool>()!=false&&!Flag(a,"archived");
    static bool ExperienceVisible(JsonObject r,JsonObject profile)
    {
        if(Text(r,"targetLocation")!=""&&!Text(r,"targetLocation").Equals(Text(profile,"site"),StringComparison.OrdinalIgnoreCase))return false;
        var audience=Obj(r["audience"]);if(Text(audience,"scope")!="selected")return true;
        var companies=Strings(audience["companyCodes"]);var countries=Strings(audience["countries"]);var groups=Strings(audience["mailGroups"]);
        if(companies.Length+countries.Length+groups.Length==0)return false;
        return(companies.Length==0||companies.Contains(Text(profile,"companyCode"),StringComparer.OrdinalIgnoreCase))
            &&(countries.Length==0||countries.Contains(Text(profile,"country"),StringComparer.OrdinalIgnoreCase))
            &&(groups.Length==0||groups.Intersect(Strings(profile["mailGroups"]),StringComparer.OrdinalIgnoreCase).Any());
    }
    public async Task<JsonObject> ReadExperience(AppUser actor,string? persona=null)
    {
        await using var c=await source.OpenConnectionAsync();await using var tx=await c.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var(legacy,_)=await Load(c,tx,false);var(model,rev)=await ExperienceLoad(c,tx);var(profile,admin,personalId)=ExperienceIdentity(actor,legacy,model,persona);
        var people=new JsonArray();await using(var cmd=new NpgsqlCommand("SELECT id,name FROM appstore.users ORDER BY name",c,tx)){await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync()){var id=r.GetString(0);people.Add(new JsonObject{["id"]=id,["name"]=r.GetString(1),["employeeId"]=id=="rumeysa"?"10001":id=="zeynep"?"10002":id,["initials"]=string.Concat(r.GetString(1).Split(' ').Take(2).Select(x=>x.FirstOrDefault())).ToUpperInvariant()});}}
        model["admins"]=legacy["admins"]?.DeepClone()??new JsonArray();
        model["adminAssignments"]=new JsonArray(people.OfType<JsonObject>().Where(p=>Strings(legacy["admins"]).Contains(Text(p,"id"))).Select(p=>(JsonNode)new JsonObject{["employeeId"]=Text(p,"employeeId"),["profileId"]=Text(p,"id")}).ToArray());
        if(!admin){model["apps"]=new JsonArray(Arr(model["apps"]).OfType<JsonObject>().Where(a=>ExperienceActive(a)&&ExperienceVisible(a,profile)).Select(a=>a.DeepClone()).ToArray());model["webEntries"]=new JsonArray(Arr(model["webEntries"]).OfType<JsonObject>().Where(r=>ExperienceVisible(r,profile)).Select(r=>r.DeepClone()).ToArray());model["testProfiles"]=new JsonArray();model["adminAssignments"]=new JsonArray();}
        var personal=new JsonObject{["favorites"]=new JsonArray(),["likes"]=new JsonArray()};await using(var cmd=new NpgsqlCommand("SELECT payload::text FROM appstore.user_preferences WHERE user_id=@id",c,tx)){cmd.Parameters.AddWithValue("id",personalId);var raw=await cmd.ExecuteScalarAsync();if(raw is string json)personal=JsonNode.Parse(json)!.AsObject();}
        var audit=new JsonArray();var activity=new JsonArray();if(admin)foreach(var table in new[]{"admin_audit_logs","action_logs"}){await using var cmd=new NpgsqlCommand($"SELECT payload::text FROM appstore.{table} ORDER BY occurred_at DESC LIMIT 100",c,tx);await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync()){var row=JsonNode.Parse(r.GetString(0))!;var item=new JsonObject{["time"]=row["time"]?.DeepClone(),["person"]=row["actor"]?.DeepClone(),["key"]=row["action"]?.DeepClone(),["entity"]=row["entity"]?.DeepClone(),["outcome"]=row["after"]?["outcome"]?.DeepClone()};(table=="admin_audit_logs"?audit:activity).Add(item);}}
        var feedback=new JsonArray();if(admin){await using var q=new NpgsqlCommand("SELECT user_id,topic,message,created_at FROM appstore.feedback ORDER BY created_at DESC LIMIT 200",c,tx);await using var r=await q.ExecuteReaderAsync();while(await r.ReadAsync())feedback.Add(new JsonObject{["userId"]=r.GetString(0),["topic"]=r.GetString(1),["message"]=r.GetString(2),["time"]=r.GetDateTime(3).ToString("O")});}model["feedback"]=feedback;model["audit"]=audit;model["activity"]=activity;await tx.CommitAsync();
        return new JsonObject{["data"]=model,["revision"]=rev,["profile"]=profile,["isAdmin"]=admin,["personal"]=personal,["people"]=environment=="Test"&&config["Authentication:Mode"]=="TestProfiles"?new JsonArray(people.OfType<JsonObject>().Where(p=>People.Any(x=>x.Id==Text(p,"id"))).Select(p=>p.DeepClone()).ToArray()):admin?people:new JsonArray(profile.DeepClone()),["environment"]=environment,["authMode"]=config["Authentication:Mode"]??"Gateway",["actor"]=new JsonObject{["id"]=actor.Id,["name"]=actor.Name,["employeeId"]=actor.Id,["initials"]=string.Concat(actor.Name.Split(' ').Take(2).Select(x=>x.FirstOrDefault())).ToUpperInvariant(),["synthetic"]=false}};
    }
    public async Task SaveExperience(AppUser actor,string? persona,JsonObject command,string correlation)
    {
        await using var c=await source.OpenConnectionAsync();await using var tx=await c.BeginTransactionAsync();var(legacy,_)=await Load(c,tx,true);var(current,rev)=await ExperienceLoad(c,tx,true);var(_,admin,_)=ExperienceIdentity(actor,legacy,current,persona);
        if(!admin)throw Error(403,"admin_required","Admin access is required.");if(command["revision"]?.GetValue<long>()!=rev)throw Error(409,"save_conflict","Someone else saved a change. Reload before saving again.");
        var next=Obj(command["data"]).DeepClone().AsObject();next["languages"]??=Languages(current).DeepClone();ValidateExperience(next);ValidateLanguages(next);
        if(environment!="Test"&&Arr(next["testProfiles"]).Count>0)throw Error(403,"test_only","TEST profiles cannot be used in Production.");
        var admins=Strings(next["admins"]);if(admins.Length==0||!admins.Contains(actor.Id))throw Error(409,"own_admin_protected","Keep your own Admin access and at least one Admin.");foreach(var id in admins)if(!await UserExists(c,tx,id))throw Error(400,"unknown_employee","Select a registered employee.");
        // Comments are owned by the comment endpoint; admin may only feature existing ones.
        foreach(var a in Arr(next["apps"]).OfType<JsonObject>()){var old=Arr(current["apps"]).OfType<JsonObject>().FirstOrDefault(x=>Text(x,"id")==Text(a,"id"));a["comments"]=old?["comments"]?.DeepClone()??new JsonArray();}
        next.Remove("audit");next.Remove("activity");next.Remove("feedback");next.Remove("admins");next.Remove("adminAssignments");
        legacy["admins"]=Array(admins);await using(var cmd=new NpgsqlCommand("UPDATE appstore.catalogue_state SET payload=@p,revision=revision+1 WHERE id=1",c,tx)){cmd.Parameters.AddWithValue("p",NpgsqlDbType.Jsonb,legacy.ToJsonString());await cmd.ExecuteNonQueryAsync();}
        await StoreExperience(c,tx,next);await WriteEvent(c,tx,actor,"admin",Text(command,"action"),Text(command,"entity"),correlation,null,new JsonObject{["changedSections"]=new JsonArray(next.Where(k=>k.Value?.ToJsonString()!=current[k.Key]?.ToJsonString()).Select(k=>(JsonNode?)JsonValue.Create(k.Key)).ToArray()),["revision"]=rev+1});await tx.CommitAsync();
    }
    static void ValidateExperience(JsonObject model)
    {
        if(model.ToJsonString().Length>12_000_000||model["apps"] is not JsonArray||model["categories"] is not JsonArray)throw Error(400,"invalid_catalogue","Check catalogue data and image sizes.");
        void Link(string url,bool optional=false,bool apk=false){if(IsExperienceFile(url))return;if(apk&&url.StartsWith("apk:")&&Guid.TryParse(url[4..],out _))return;if(optional&&url.Length==0)return;if(!SafeUrl(url))throw Error(400,"invalid_url","Use HTTPS links without credentials.");}
        void Image(string url){if(IsExperienceFile(url))return;if(url=="sample-screen.svg"||url.Length==0||Regex.IsMatch(url,@"^assets/[a-zA-Z0-9_.-]+$"))return;if(url.Length<820000&&Regex.IsMatch(url,@"^data:image/(png|jpeg|webp);base64,[A-Za-z0-9+/=]+$"))return;Link(url);}
        foreach(var name in new[]{"apps","webEntries","testProfiles"}){var ids=new HashSet<string>();foreach(var x in Arr(model[name]).OfType<JsonObject>())if(!Regex.IsMatch(Text(x,"id"),@"^[\w-]{1,128}$")||!ids.Add(Text(x,"id")))throw Error(400,"invalid_id","Use unique item IDs.");}
        foreach(var a in Arr(model["apps"]).OfType<JsonObject>())
        {Image(Text(a,"image"));if(!Regex.IsMatch(Text(a,"color"),@"^#[a-fA-F0-9]{6}$"))throw Error(400,"invalid_color","Use a six-digit accent colour.");foreach(var kind in new[]{"links","documents"})foreach(var l in Arr(a[kind]).OfType<JsonObject>()){var url=Text(l,"url");if(kind=="documents"&&url=="guide.html")continue;Link(url,kind=="links"&&Flag(l,"pending"),kind=="links");}foreach(var l in Arr(a["images"]))Image(l is JsonValue?l.GetValue<string>():Text(l!,"url"));}
        foreach(var r in Arr(model["webEntries"]).OfType<JsonObject>().Append(Obj(model["webPortal"]))){Link(Text(r,"url"));Image(Text(r,"image"));Image(Text(r,"background"));}
        foreach(var entry in Arr(model["apps"]).OfType<JsonObject>().Concat(Arr(model["webEntries"]).OfType<JsonObject>()).Append(Obj(model["webPortal"])))
        {var audience=Obj(entry["audience"]);if(Text(audience,"scope") is not ("" or "all" or "selected"))throw Error(400,"invalid_audience","Choose an audience.");
         foreach(var key in new[]{"companyCodes","countries","mailGroups"}){var values=Strings(audience[key]);if(values.Length>100||values.Any(v=>v.Length>256||v.Any(char.IsControl)))throw Error(400,"invalid_audience","Check audience values.");}}
        Image(Text(model,"logo"));
    }
    public async Task ChangePersonal(AppUser actor,string? persona,JsonObject body)
    {
        var key=Text(body,"key");var id=Text(body,"id");if(key is not ("favorites" or "likes")||id.Length>180)throw Error(400,"invalid_preference","Invalid preference.");
        await using var c=await source.OpenConnectionAsync();await using var tx=await c.BeginTransactionAsync();var(legacy,_)=await Load(c,tx,false);var(model,_)=await ExperienceLoad(c,tx);var(_,_,personalId)=ExperienceIdentity(actor,legacy,model,persona);
        await using(var init=new NpgsqlCommand("INSERT INTO appstore.user_preferences(user_id) VALUES(@id) ON CONFLICT DO NOTHING",c,tx)){init.Parameters.AddWithValue("id",personalId);await init.ExecuteNonQueryAsync();}
        JsonObject personal;await using(var get=new NpgsqlCommand("SELECT payload::text FROM appstore.user_preferences WHERE user_id=@id FOR UPDATE",c,tx)){get.Parameters.AddWithValue("id",personalId);personal=JsonNode.Parse((string)(await get.ExecuteScalarAsync())!)!.AsObject();}
        var list=Strings(personal[key]).Where(v=>v!=id).ToList();if(Flag(body,"selected"))list.Add(id);personal[key]=Array(list.Take(2000));await using(var cmd=new NpgsqlCommand("UPDATE appstore.user_preferences SET payload=@p,updated_at=now() WHERE user_id=@id",c,tx)){cmd.Parameters.AddWithValue("p",NpgsqlDbType.Jsonb,personal.ToJsonString());cmd.Parameters.AddWithValue("id",personalId);await cmd.ExecuteNonQueryAsync();}await tx.CommitAsync();
    }
    public async Task ExperienceComment(AppUser actor,string? persona,JsonObject body,string correlation)
    {
        var text=Text(body,"text").Trim();var lang=Text(body,"language");if(text.Length is <1 or >300||lang.Length>64)throw Error(400,"invalid_comment","Write a comment up to 300 characters.");
        await using var c=await source.OpenConnectionAsync();await using var tx=await c.BeginTransactionAsync();var(legacy,_)=await Load(c,tx,false);var(model,_)=await ExperienceLoad(c,tx,true);var(profile,_,_)=ExperienceIdentity(actor,legacy,model,persona);if(!KnownLanguage(model,lang))throw Error(400,"invalid_language","Choose a registered language.");var a=Arr(model["apps"]).OfType<JsonObject>().FirstOrDefault(a=>Text(a,"id")==Text(body,"appId")&&ExperienceActive(a));if(a is null||!ExperienceVisible(a,profile)||Strings(a["platforms"]).All(x=>x=="Web"))throw Error(403,"comment_not_allowed","Comments are not available here.");a["comments"]??=new JsonArray();a["comments"]!.AsArray().Add(new JsonObject{["id"]="comment-"+Guid.NewGuid().ToString("N"),["author"]=Text(profile,"name"),["time"]=DateTimeOffset.UtcNow.ToString("O"),["language"]=lang,["text"]=new JsonObject{[lang]=text}});await StoreExperience(c,tx,model);await WriteEvent(c,tx,actor,"user","commentAdded",Text(body,"appId"),correlation,null,null);await tx.CommitAsync();
    }
    public async Task ExperienceFeedback(AppUser actor,JsonObject body,string correlation)
    {var topic=Text(body,"topic");var message=Text(body,"message").Trim();if(topic.Length is <1 or >100||message.Length is <1 or >2000)throw Error(400,"invalid_feedback","Enter a topic and message (up to 2000 characters).");await using var c=await source.OpenConnectionAsync();await using var tx=await c.BeginTransactionAsync();await using var cmd=new NpgsqlCommand("INSERT INTO appstore.feedback(id,user_id,topic,message) VALUES(@id,@u,@t,@m)",c,tx);cmd.Parameters.AddWithValue("id",Guid.NewGuid());cmd.Parameters.AddWithValue("u",actor.Id);cmd.Parameters.AddWithValue("t",topic);cmd.Parameters.AddWithValue("m",message);await cmd.ExecuteNonQueryAsync();await WriteEvent(c,tx,actor,"user","feedbackReceived",topic,correlation,null,null);await tx.CommitAsync();}
    public async Task<string> ExperienceOpen(AppUser actor,string? persona,string resource,string? kind,string? linkId,string correlation)
    {
        if(resource.Length>200||(linkId?.Length??0)>128)throw Error(400,"invalid_resource","Invalid resource.");
        await using var c=await source.OpenConnectionAsync();await using var tx=await c.BeginTransactionAsync();
        var(legacy,_)=await Load(c,tx,false);var(model,_)=await ExperienceLoad(c,tx);var(profile,_,_)=ExperienceIdentity(actor,legacy,model,persona);
        string url="",resourceType=resource.StartsWith("app:")?"download":"web";
        try
        {
            if(resource.StartsWith("app:"))
            {
                var app=Arr(model["apps"]).OfType<JsonObject>().SingleOrDefault(x=>Text(x,"id")==resource[4..]&&ExperienceActive(x))??throw Error(404,"app_missing","Application unavailable.");
                resourceType=kind=="document"?"document":Strings(app["platforms"]).All(p=>p=="Web")?"web":"download";
                if(!ExperienceVisible(app,profile))throw Error(403,"not_eligible","This application is unavailable for this account.");
                if(string.IsNullOrEmpty(linkId))url=Text(app,"url");
                else{var link=Arr(app[kind=="document"?"documents":"links"]).OfType<JsonObject>().SingleOrDefault(x=>Text(x,"id")==linkId&&!Flag(x,"pending"))??throw Error(404,"link_missing","Link unavailable.");url=Text(link,"url");}
            }
            else
            {
                var id=resource.StartsWith("web:")?resource[4..]:resource;resourceType=id=="behome"?"portal":"web";
                var item=id=="behome"?Obj(model["webPortal"]):Arr(model["webEntries"]).OfType<JsonObject>().SingleOrDefault(x=>Text(x,"id")==id)??throw Error(404,"web_missing","Website unavailable.");
                if(!ExperienceVisible(item,profile))throw Error(403,"not_eligible","This tool is unavailable for this account.");url=Text(item,"url");
            }
            if(!SafeUrl(url)&&!IsExperienceFile(url)&&!(url.StartsWith("apk:")&&Guid.TryParse(url[4..],out _))&&!(kind=="document"&&url=="guide.html"))throw Error(400,"invalid_url","This link is not ready.");
            if(url.StartsWith("apk:"))
            {await using var check=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM appstore.apk_packages WHERE id=@id)",c,tx);check.Parameters.AddWithValue("id",Guid.Parse(url[4..]));if(!(bool)(await check.ExecuteScalarAsync())!)throw Error(404,"apk_missing","APK unavailable.");}
            await WriteEvent(c,tx,actor,"user","activityOpen",resource,correlation,null,new JsonObject{["outcome"]=url.StartsWith("apk:")?"file_requested":"redirect_issued",["resourceType"]=resourceType,["applicationId"]=resource,["linkId"]=linkId??"",["statusCode"]=url.StartsWith("apk:")?200:302});
            await tx.CommitAsync();return url;
        }
        catch(GateException error)
        {
            await WriteEvent(c,tx,actor,"user","activityOpen",resource,correlation,null,new JsonObject{["outcome"]="blocked",["resourceType"]=resourceType,["statusCode"]=error.Status,["reason"]=error.Code,["applicationId"]=resource,["linkId"]=linkId??""});await tx.CommitAsync();throw;
        }
    }
}
