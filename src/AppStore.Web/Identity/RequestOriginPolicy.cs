namespace AppStore.Web.Identity;

// Compare browser origins to deployment configuration, never forwarded headers.
public sealed class RequestOriginPolicy
{
    private readonly string? trustedOrigin;
    public RequestOriginPolicy(string? configuredOrigin)
    {
        if (!string.IsNullOrWhiteSpace(configuredOrigin))
            trustedOrigin = Normalize(configuredOrigin) ?? throw new InvalidOperationException("Hosting:TrustedPublicOrigin must be an HTTP(S) origin without a path, credentials, query or fragment.");
    }

    public bool Allows(string origin, string fetchSite, string scheme, string host)
    {
        if (string.Equals(fetchSite, "cross-site", StringComparison.OrdinalIgnoreCase)) return false;
        // Non-browser JSON clients still need the endpoint's authenticated identity.
        if (string.IsNullOrEmpty(origin)) return true;
        var actual = Normalize(origin);
        var expected = trustedOrigin ?? Normalize(scheme + "://" + host);
        return actual is not null && expected is not null && string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static string? Normalize(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") || string.IsNullOrEmpty(uri.Host) ||
            uri.UserInfo.Length != 0 || uri.AbsolutePath != "/" ||
            uri.Query.Length != 0 || uri.Fragment.Length != 0) return null;
        return uri.GetLeftPart(UriPartial.Authority);
    }
}
