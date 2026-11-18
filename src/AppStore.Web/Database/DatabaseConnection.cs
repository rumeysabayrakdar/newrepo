using Npgsql;

namespace AppStore.Web.Database;

/// <summary>Reads explicit settings or the PostgreSQL variables supplied automatically by Norm.</summary>
public static class DatabaseConnection
{
    public static (NpgsqlConnectionStringBuilder Connection, string DatabaseName) Resolve(IConfiguration config)
    {
        try
        {
            var mode=config["AppStore:Environment"]??"Test";
            var prefix=mode=="Production"?"PROD":"TEST";
            var scoped=First(config.GetConnectionString(mode),config[prefix+"_POSTGRES_CONNECTION_STRING"],config[prefix+"_DATABASE_URL"]);
            var external = First(Environment.GetEnvironmentVariable("ConnectionStrings__AppStore"),
                config["POSTGRES_CONNECTION_STRING"],config["DATABASE_URL"],config.GetConnectionString("Postgres"),
                config.GetConnectionString("DefaultConnection"),config.GetConnectionString("Default"));
            var structured=First(config["POSTGRES_HOST"]) is not null&&First(config["POSTGRES_DB"]) is not null&&First(config["POSTGRES_USER"]) is not null;
            var raw=First(scoped,external,structured?null:config.GetConnectionString("AppStore"));
            NpgsqlConnectionStringBuilder connection;
            if (raw is not null)
                connection = raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
                    ? FromUrl(raw) : new NpgsqlConnectionStringBuilder(raw);
            else
            {
                var host = First(config["POSTGRES_HOST"]);
                var database = First(config["POSTGRES_DB"]);
                var user = First(config["POSTGRES_USER"]);
                if (host is null || database is null || user is null)
                    throw new InvalidOperationException("No PostgreSQL connection was supplied. Enable this deployment's Norm PostgreSQL service.");
                connection = new NpgsqlConnectionStringBuilder { Host = host, Database = database, Username = user };
                if (First(config["POSTGRES_PORT"]) is string port) connection.Port = int.Parse(port, System.Globalization.CultureInfo.InvariantCulture);
                if (config["POSTGRES_PASSWORD"] is string password) connection.Password = password;
            }
            var expected = First(config["Database:"+mode+":Name"],scoped is null?config["Database:Name"]:null,connection.Database,scoped is null?config["POSTGRES_DB"]:null);
            if (expected is null || string.IsNullOrWhiteSpace(connection.Host) || string.IsNullOrWhiteSpace(connection.Username))
                throw new InvalidOperationException("The PostgreSQL host, database and username must be configured.");
            if (connection.Database != expected)
                throw new InvalidOperationException("The database name does not match this deployment. Startup was blocked.");
            connection.IncludeErrorDetail = false;
            connection["GSS Encryption Mode"] = "Disable";
            return (connection, expected);
        }
        catch (Exception error) when (error is ArgumentException or FormatException or OverflowException)
        {
            // Do not echo connection strings, URL credentials or parser errors into logs.
            throw new InvalidOperationException("The PostgreSQL connection format is invalid. Check this deployment's Norm database settings.");
        }
    }

    private static string? First(params string?[] values) => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static NpgsqlConnectionStringBuilder FromUrl(string raw)
    {
        var url = new Uri(raw, UriKind.Absolute);
        var credentials = url.UserInfo.Split(':', 2);
        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = url.Host,
            Port = url.Port < 0 ? 5432 : url.Port,
            Database = Uri.UnescapeDataString(url.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length == 2 ? Uri.UnescapeDataString(credentials[1]) : ""
        };
        foreach (var entry in url.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = entry.Split('=', 2);
            var key = Uri.UnescapeDataString(pair[0]).ToLowerInvariant();
            var value = pair.Length == 2 ? Uri.UnescapeDataString(pair[1].Replace("+", " ")) : "";
            switch (key)
            {
                case "sslmode":
                    connection.SslMode = value.ToLowerInvariant() switch
                    {
                        "disable" => SslMode.Disable, "allow" => SslMode.Allow, "prefer" => SslMode.Prefer,
                        "require" => SslMode.Require, "verify-ca" => SslMode.VerifyCA, "verify-full" => SslMode.VerifyFull,
                        _ => throw new FormatException()
                    };
                    break;
                case "sslrootcert": connection.RootCertificate = value; break;
                case "sslcert": connection.SslCertificate = value; break;
                case "sslkey": connection.SslKey = value; break;
                case "connect_timeout": connection.Timeout = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture); break;
                case "application_name": connection.ApplicationName = value; break;
                case "options": connection.Options = value; break;
                default: throw new InvalidOperationException("Unsupported PostgreSQL URL option. Configure a standard Npgsql connection string to retain its connection options.");
            }
        }
        return connection;
    }
}
