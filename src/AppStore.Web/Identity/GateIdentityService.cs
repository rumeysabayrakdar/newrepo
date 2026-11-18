using System.Net.Mail;
namespace AppStore.Web.Identity;
public sealed record AppUser(string Id, string Name, string? InternalUserId = null, string? Email = null, string? CompanyCode = null, string? Country = null, string[]? Groups = null, string? Site = null);
public sealed class GateException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
public sealed class GateIdentityService(IConfiguration config)
{
    public AppUser Read(HttpContext context)
    {
        // Trust the immediate configured Norm gateway, not client forwarding headers.
        var peer=context.Connection.RemoteIpAddress;
        if(peer?.IsIPv4MappedToIPv6==true)peer=peer.MapToIPv4();
        var networks=config.GetSection("Authentication:TrustedProxyCidrs").Get<string[]>()??[];
        if(peer is null || !networks.Any(c=>System.Net.IPNetwork.TryParse(c,out var n)&&n.Contains(peer)))
            throw new GateException(403,"untrusted_gateway","Sign in through the configured company gateway.");
        var id = Header(context, "X-User-Sicil", true)!;
        var internalId = Header(context, "X-User-Id", true)!;
        if (id.Length > 128 || id.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)) || !Guid.TryParse(internalId, out var guid) || guid == Guid.Empty)
            throw new GateException(401, "invalid_identity", "Your company identity could not be verified.");
        var email = Header(context, "X-User-Email", false);
        if (email is not null && (!MailAddress.TryCreate(email, out var address) || address.Address != email))
            throw new GateException(401, "invalid_email", "Your company email could not be verified.");
        string? Claim(string name,string fallback)=>Header(context,config["Authentication:Headers:"+name]??fallback,false);
        var groupsKey=config["Authentication:Headers:Groups"]??"X-User-Groups";
        var rawGroups=context.Request.Headers[groupsKey].ToString();
        if(rawGroups.Length>8192||rawGroups.Any(char.IsControl))throw new GateException(401,"invalid_groups","Invalid company groups.");
        var groups=rawGroups.Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if(groups.Length>100||groups.Any(g=>g.Length>256))throw new GateException(401,"invalid_groups","Invalid company groups.");
        return new(id, Claim("Name","X-User-Name")??email??id, guid.ToString(), email,Claim("CompanyCode","X-User-Company-Code"),Claim("Country","X-User-Country"),groups,Claim("Site","X-User-Site"));
    }
    private static string? Header(HttpContext context, string key, bool required)
    {
        var values = context.Request.Headers[key];
        if (values.Count == 0 || string.IsNullOrWhiteSpace(values[0]))
        {
            if (required) throw new GateException(401, "sign_in_required", "Please sign in through your company's Okta gateway.");
            return null;
        }
        var value = values[0]!;
        if (values.Count != 1 || value != value.Trim() || value.Contains(',') || value.Any(char.IsControl) || value.Length > 512)
            throw new GateException(401, "invalid_identity", "Your company identity could not be verified.");
        return value;
    }
}
