using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;
using AppStore.Web.Identity;
namespace AppStore.Web.Database;

public sealed partial class AppStoreDatabase
{
    static JsonArray Languages(JsonObject model) => model["languages"] as JsonArray ?? new JsonArray(new JsonObject { ["code"]="en", ["name"]="English" }, new JsonObject { ["code"]="tr", ["name"]="Türkçe" });
    static bool KnownLanguage(JsonObject model,string language) => Languages(model).OfType<JsonObject>().Any(l=>Text(l,"code")==language);
    async Task EnsureLanguages()
    {
        await using var c=await source.OpenConnectionAsync();await using var tx=await c.BeginTransactionAsync();var(model,_)=await ExperienceLoad(c,tx,true);
        if(model["languages"] is null){model["languages"]=Languages(model).DeepClone();model["fallbackLanguage"]="en";await StoreExperience(c,tx,model);}await tx.CommitAsync();
    }
    static void ValidateLanguages(JsonObject model)
    {
        if(model["languages"] is not JsonArray languages || languages.Count==0)throw Error(400,"invalid_languages","Add at least one language.");
        var codes=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var l in languages.OfType<JsonObject>())
        {var code=Text(l,"code");if(code.Length>64||!System.Text.RegularExpressions.Regex.IsMatch(code,@"^[A-Za-z]{2,8}(?:-[A-Za-z0-9]{1,8})*$")||!codes.Add(code)||string.IsNullOrWhiteSpace(Text(l,"name"))||Text(l,"name").Length>80)throw Error(400,"invalid_languages","Use a unique language code and a language name.");}
        if(codes.Count!=languages.Count || !codes.Contains("en"))throw Error(400,"invalid_languages","Keep English as the fallback language.");
        model["fallbackLanguage"]="en";
        foreach(var lang in Obj(model["uiOverrides"]))
        {if(!codes.Contains(lang.Key)||lang.Value is not JsonObject values)throw Error(400,"invalid_translation","Select a registered language.");foreach(var item in values){if(item.Value is not JsonValue v||!v.TryGetValue<string>(out var text)||text.Length>4000||text.Contains('<')||text.Contains('>'))throw Error(400,"invalid_translation","Use plain text up to 4000 characters.");}}
    }
    public async Task<JsonObject> ExperienceReport(AppUser actor,string? persona,IQueryCollection query)
    {
        var kind=query["kind"].ToString();if(kind is not("overview" or "activity" or "audit"))throw Error(400,"invalid_report","Choose an overview or a log.");
        var now=DateTimeOffset.UtcNow;var from=now.AddDays(-30);var to=now;
        if(query.ContainsKey("from")&&!DateTimeOffset.TryParse(query["from"],System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.AssumeUniversal,out from))throw Error(400,"invalid_date","Check the start date.");
        if(query.ContainsKey("to")&&!DateTimeOffset.TryParse(query["to"],System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.AssumeUniversal,out to))throw Error(400,"invalid_date","Check the end date.");
        if(from>=to||(to-from).TotalDays>3660)throw Error(400,"invalid_date","Choose a valid date range of at most ten years.");
        int page=1;if(query.ContainsKey("page")&&(!int.TryParse(query["page"],out page)||page<1||page>1000000))throw Error(400,"invalid_page","Choose a valid page.");
        string Arg(string key,int max=200){var v=query[key].ToString().Trim();if(v.Length>max)throw Error(400,"invalid_filter","A filter is too long.");return v;}
        string search=Arg("q"),who=Arg("user",128),action=Arg("action",128),entity=Arg("entity"),outcome=Arg("outcome",64);
        await using var c=await source.OpenConnectionAsync();await using var tx=await c.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var(legacy,_)=await Load(c,tx,false);var(model,_)=await ExperienceLoad(c,tx);var(_,admin,_)=ExperienceIdentity(actor,legacy,model,persona);if(!admin)throw Error(403,"admin_required","Only an Admin can read reports.");
        var web=Arr(model["apps"]).OfType<JsonObject>().Where(a=>Strings(a["platforms"]).All(p=>p=="Web")).Select(a=>"app:"+Text(a,"id")).ToArray();
        const string cte="""
        WITH events AS (
          SELECT id,occurred_at,actor_id,action,entity,payload,'activity'::text AS channel FROM appstore.action_logs
          UNION ALL SELECT id,occurred_at,actor_id,action,entity,payload,'audit'::text FROM appstore.admin_audit_logs
        ), period AS (
          SELECT *,COALESCE(payload->'after'->>'outcome','') AS result,
          COALESCE(payload->'after'->>'resourceType',CASE WHEN entity LIKE 'web:%' OR entity=ANY(@web) THEN 'web' WHEN entity LIKE 'app:%' OR action='Application opened' THEN 'download' ELSE '' END) AS resource_type
          FROM events WHERE occurred_at>=@from AND occurred_at<@to
        ), filtered AS (
          SELECT * FROM period WHERE (@kind='overview' OR channel=@kind)
            AND (@user='' OR actor_id=@user) AND (@action='' OR action=@action)
            AND (@entity='' OR entity=@entity) AND (@outcome='' OR result=@outcome)
            AND (@q='' OR strpos(lower(actor_id||' '||COALESCE(payload->>'actor','')||' '||action||' '||entity||' '||result),lower(@q))>0)
        )
        """;
        NpgsqlCommand Command(string sql){var cmd=new NpgsqlCommand(cte+sql,c,tx);cmd.Parameters.AddWithValue("from",from.ToUniversalTime());cmd.Parameters.AddWithValue("to",to.ToUniversalTime());cmd.Parameters.AddWithValue("web",NpgsqlDbType.Array|NpgsqlDbType.Text,web);cmd.Parameters.AddWithValue("kind",kind);cmd.Parameters.AddWithValue("user",who);cmd.Parameters.AddWithValue("action",action);cmd.Parameters.AddWithValue("entity",entity);cmd.Parameters.AddWithValue("outcome",outcome);cmd.Parameters.AddWithValue("q",search);cmd.Parameters.AddWithValue("offset",(page-1)*50);return cmd;}
        JsonObject result;
        if(kind=="overview")
        {
            const string sql="""
            SELECT jsonb_build_object(
              'downloadRequests',(SELECT count(*) FROM filtered WHERE channel='activity' AND resource_type='download' AND result IN ('linkOpened','redirect_issued','file_requested')),
              'webVisits',(SELECT count(*) FROM filtered WHERE channel='activity' AND resource_type IN ('web','portal') AND result IN ('linkOpened','redirect_issued')),
              'byApplication',COALESCE((SELECT jsonb_agg(to_jsonb(r)) FROM (SELECT entity AS resource,count(*) AS count FROM filtered WHERE channel='activity' AND resource_type='download' AND result IN ('linkOpened','redirect_issued','file_requested') GROUP BY entity ORDER BY count(*) DESC,entity LIMIT 10) r),'[]'::jsonb),
              'daily',COALESCE((SELECT jsonb_agg(to_jsonb(r)) FROM (SELECT (occurred_at AT TIME ZONE 'Europe/Istanbul')::date AS day,count(*) AS count FROM filtered WHERE channel='activity' GROUP BY 1 ORDER BY 1) r),'[]'::jsonb))::text
            """;
            await using var cmd=Command(sql);result=JsonNode.Parse((string)(await cmd.ExecuteScalarAsync())!)!.AsObject();
            var active=Arr(model["apps"]).OfType<JsonObject>().Where(ExperienceActive).ToArray();result["activeApplications"]=active.Length;result["readyApplications"]=active.Count(a=>Arr(a["links"]).OfType<JsonObject>().Any(l=>!Flag(l,"pending")&&!string.IsNullOrEmpty(Text(l,"url"))));
        }
        else
        {
            const string sql="""
            SELECT jsonb_build_object('total',(SELECT count(*) FROM filtered),'page',@offset/50+1,'pageSize',50,
              'items',COALESCE((SELECT jsonb_agg(to_jsonb(r)) FROM (SELECT id,occurred_at AS time,actor_id AS "userId",COALESCE(payload->>'actor',actor_id) AS person,action AS key,entity,result AS outcome,payload->'after' AS details FROM filtered ORDER BY occurred_at DESC,id DESC LIMIT 50 OFFSET @offset) r),'[]'::jsonb),
              'users',COALESCE((SELECT jsonb_agg(to_jsonb(r)) FROM (SELECT DISTINCT actor_id AS id,COALESCE(payload->>'actor',actor_id) AS name FROM period WHERE channel=@kind ORDER BY 2) r),'[]'::jsonb),
              'actions',COALESCE((SELECT jsonb_agg(action) FROM (SELECT DISTINCT action FROM period WHERE channel=@kind ORDER BY action) r),'[]'::jsonb),
              'entities',COALESCE((SELECT jsonb_agg(entity) FROM (SELECT DISTINCT entity FROM period WHERE channel=@kind ORDER BY entity) r),'[]'::jsonb),
              'outcomes',COALESCE((SELECT jsonb_agg(result) FROM (SELECT DISTINCT result FROM period WHERE channel=@kind AND result<>'' ORDER BY result) r),'[]'::jsonb))::text
            """;
            await using var cmd=Command(sql);result=JsonNode.Parse((string)(await cmd.ExecuteScalarAsync())!)!.AsObject();
        }
        result["source"]="postgresql";result["from"]=from.ToString("O");result["to"]=to.ToString("O");result["generatedAt"]=now.ToString("O");await tx.CommitAsync();return result;
    }
}
