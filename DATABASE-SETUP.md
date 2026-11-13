# PostgreSQL

PostgreSQL is required in both Test and Production. There is no data-store switch or localStorage fallback. Start Test with docker compose up --build -d on a compatible host. Persistent named volumes hold the database and generated password.

Connection templates live in appsettings.json and appsettings.Production.json. Set Database:Name to the exact selected database name. A real password belongs in the hosting secret connection or a mounted secret file, never GitHub. Norm can replace/inject its managed connection during deployment; the expected database name must match.

The application automatically initializes the appstore schema, checks its Test/Production marker and seeds the six user-supplied app listings only if no catalogue exists. Existing edits are not overwritten on restart. State and reference data use revisioned PostgreSQL JSONB; users and immutable application/audit events have dedicated tables. Writes and audit records commit together; stale editors receive 409.

Test and Production must have separate databases and credentials. The supplied Compose files use separate projects, volumes, database names and usernames. Production contains no synthetic user seed; identities are registered only after trusted gateway verification. No employment attribute is used for visibility.

See DEPLOYMENT.md for managed hosting, auth-gate trust, bootstrap, backups and API details.
