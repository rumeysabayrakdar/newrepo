using System.Net.Mail;
namespace AppStore.Web.Identity;
public sealed record AppUser(string Id, string Name, string? InternalUserId = null, string? Email = null);
public sealed class GateException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
public sealed class GateIdentityService(IConfiguration config)
{
    public AppUser Read(HttpContext context)
    {
        var ranges = config.GetSection("Authentication:TrustedProxyCidrs").Get<string[]>() ?? [];
        var peer = context.Connection.RemoteIpAddress;
        if (peer?.IsIPv4MappedToIPv6 == true) peer = peer.MapToIPv4();
        if (peer is null || !ranges.Any(c => System.Net.IPNetwork.TryParse(c, out var network) && network.Contains(peer)))
            throw new GateException(403, "trusted_gateway_required", "Please open this application through your company sign-in gateway.");
        var id = Header(context, "X-User-Sicil", true)!;
        var internalId = Header(context, "X-User-Id", true)!;
        if (id.Length > 128 || id.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)) || !Guid.TryParse(internalId, out var guid) || guid == Guid.Empty)
            throw new GateException(401, "invalid_identity", "Your company identity could not be verified.");
        var email = Header(context, "X-User-Email", false);
        if (email is not null && (!MailAddress.TryCreate(email, out var address) || address.Address != email))
            throw new GateException(401, "invalid_email", "Your company email could not be verified.");
        return new(id, email ?? id, guid.ToString(), email);
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
