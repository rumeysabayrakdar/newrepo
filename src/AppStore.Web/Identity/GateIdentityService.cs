using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace AppStore.Web.Identity;

public sealed record VerifiedEmployee(string EmployeeId, Guid UserId, string? Email,
    string? CountryCode, string? WorkerClassification, string? Country, string? Type,
    bool CanBrowse, string ProfileStatus, bool IsManager = false);
public sealed class GateException(int status, string code, string message) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
}
public sealed class GateIdentityService(IConfiguration config)
{
    public VerifiedEmployee Read(HttpContext context)
    {
        // Compare the immediate peer only. Do not trust X-Forwarded-For or client headers.
        var ranges = config.GetSection("AuthGate:TrustedProxyCidrs").Get<string[]>() ?? [];
        if (ranges.Length == 0)
            throw new GateException(503, "gateway_not_configured", "Your platform team must configure the trusted auth-gate network first.");
        var peer = context.Connection.RemoteIpAddress;
        if (peer?.IsIPv4MappedToIPv6 == true) peer = peer.MapToIPv4();
        if (peer is null || !ranges.Any(cidr => System.Net.IPNetwork.TryParse(cidr, out var network) && network.Contains(peer)))
            throw new GateException(403, "untrusted_gateway", "This application must be accessed through its configured auth-gate.");

        var employeeId = Header(context, "X-User-Sicil", true)!;
        var internalId = Header(context, "X-User-Id", true);
        // The organisation-specific employee ID is kept distinct from the internal GUID.
        if (employeeId.Length > 128 || employeeId.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)) ||
            !Guid.TryParse(internalId, out var userId) || userId == Guid.Empty)
            throw new GateException(401, "invalid_identity", "The gateway supplied an invalid employee identity.");
        var pattern = config["AuthGate:EmployeeIdPattern"];
        if (!string.IsNullOrEmpty(pattern) && !Regex.IsMatch(employeeId, pattern,
                RegexOptions.CultureInvariant | RegexOptions.NonBacktracking, TimeSpan.FromMilliseconds(100)))
            throw new GateException(401, "invalid_employee_id", "The employee ID does not match the configured organisation format.");
        var email = Header(context, "X-User-Email", false);
        if (email is not null && (email.Length > 254 || !MailAddress.TryCreate(email, out var parsed) || parsed.Address != email))
            throw new GateException(401, "invalid_email", "The gateway supplied an invalid email address.");

        // No invented country/classification header names or values: configure them with the platform.
        var countryHeader = config["AuthGate:CountryHeader"];
        var workerHeader = config["AuthGate:WorkerClassificationHeader"];
        if (string.IsNullOrWhiteSpace(countryHeader) || string.IsNullOrWhiteSpace(workerHeader))
            return new(employeeId, userId, email, null, null, null, null, false, "targeting-not-configured");
        var code = Header(context, countryHeader, false);
        var worker = Header(context, workerHeader, false);
        if (code is null || worker is null)
            return new(employeeId, userId, email, code, worker, null, null, false, "targeting-attributes-missing");
        var countryMap = config.GetSection("AuthGate:CountryMap").Get<Dictionary<string, string>>() ?? [];
        var workerMap = config.GetSection("AuthGate:WorkerClassificationMap").Get<Dictionary<string, string>>() ?? [];
        // ISO alpha-2 shape plus an explicit configured allowlist. No silent normalization or fallback.
        if (!Regex.IsMatch(code, "^[A-Z]{2}$", RegexOptions.CultureInvariant) ||
            !countryMap.TryGetValue(code, out var country) || string.IsNullOrWhiteSpace(country) ||
            !workerMap.TryGetValue(worker, out var type) || string.IsNullOrWhiteSpace(type))
            return new(employeeId, userId, email, code, worker, null, null, false, "targeting-attributes-unrecognized");
        return new(employeeId, userId, email, code, worker, country, type, true, "ready");
    }
    public VerifiedEmployee RequireCatalogue(HttpContext context)
    {
        var employee = Read(context);
        if (!employee.CanBrowse)
            throw new GateException(403, employee.ProfileStatus,
                "Your country and employee classification are unavailable or not configured. Please contact your platform team.");
        return employee;
    }
    private static string? Header(HttpContext context, string name, bool required)
    {
        if (!context.Request.Headers.TryGetValue(name, out var values) || values.Count == 0 || string.IsNullOrWhiteSpace(values[0]))
        {
            if (required) throw new GateException(401, "identity_missing", "No verified identity was supplied. Please enter through your company's auth-gate.");
            return null;
        }
        var value = values[0]!;
        if (values.Count != 1 || value != value.Trim() || value.Contains(',') || value.Any(char.IsControl) || value.Length > 512)
            throw new GateException(401, "invalid_identity_header", "A gateway identity header has an invalid or repeated value.");
        return value;
    }
}
