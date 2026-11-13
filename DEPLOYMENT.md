# Hosting and configuration

## JSON-only environment selection

The application reads `AppStore:Environment` directly from the deployed base appsettings.json before building configuration. Allowed values are Test and Production. It then loads that environment's JSON file. The selector is locked after secret/environment providers are added, so an old DOTNET_ENVIRONMENT, ASPNETCORE_ENVIRONMENT or AppStore__Environment cannot select another mode. Hosting:Port defaults to 8080 and Kestrel explicitly listens on all container interfaces at that port.

Normal hosting environment variables may inject secrets, PostgreSQL connection details, trusted gateway ranges and bootstrap employee IDs. They do not select Test/Production. No passwords are committed.

## Test

`compose.yaml` runs PostgreSQL 17 plus the .NET app. An initializer generates a password into a private named volume; app and database mount it read-only. A separate named volume stores PostgreSQL data. The database has no published host port. Test users and initial Test Admin IDs are defined in appsettings.Test.json. They have no country/type/manager attributes.

For a managed database, configure ConnectionStrings__AppStore and Database__Name to the exact same database name. Do not use a Production database for Test. If a mounted password secret is used, Database__PasswordFile supplies its path. Otherwise put the password only in the hosting secret connection string. Use the hosting platform's TLS certificate requirements; the app preserves TLS settings. GSS/Kerberos negotiation is disabled because this application uses PostgreSQL password credentials, independently of Okta web sign-in.

## Production

1. Provision a dedicated PostgreSQL database and persistent storage. appsettings.Production.json contains only a nonsecret template, never a real password.
2. Configure the secure connection and expected Database:Name.
3. Keep the application reachable only through Norm's auth-gate. The gateway must remove/overwrite client-supplied identity headers.
4. Configure Authentication:TrustedProxyCidrs with the immediate gateway/proxy source networks. Missing, malformed or unrestricted /0 ranges block startup. The app checks the immediate peer and does not trust X-Forwarded-For.
5. Configure Authentication:InitialAdminEmployeeIds with the real X-User-Sicil of the first Admin. An exact allowlisted user receives Admin only after verified gateway sign-in, with a one-time audited bootstrap. Revoking that Admin later does not automatically regrant access on the next login.
6. Set AppStore:Environment to Production in the deployed base JSON selector. The provided compose.production.yaml mounts deployment/production/appsettings.json, which already selects Production and uses separate volumes/secrets. Its port is loopback-only, ready to sit behind the real gateway.

Production identity contract:
- X-User-Sicil: employee ID string (the role/audit identity).
- X-User-Id: distinct internal platform GUID.
- X-User-Email: optional validated display email.

No country, worker classification or manager header is read. All signed-in users see the same active/unarchived catalogue; manual filters are user choices. No Test account header/query can authenticate a Production request. No tokens/passwords are handled by the frontend.

## Public website address behind a gateway

Set Hosting:TrustedPublicOrigin to the exact public website origin, currently https://final.norm.ar.arcelik. Browser mutation requests are compared to that configured scheme, hostname and port, independently of the internal request Host. Other origins and Sec-Fetch-Site: cross-site are rejected. Forwarded host headers do not extend the allow-list. When no public origin is configured, direct-host deployments retain same-origin validation.

## Health, data and recovery

GET /healthz performs an actual PostgreSQL query and returns 503 on database failure. There is no browser storage/file fallback. If PostgreSQL is unavailable at startup, the app stops with a configuration message. Startup diagnostics log exception type/source location and PostgreSQL error code, not secrets or connection strings.

The schema is appstore: catalogue_state (revisioned JSONB), users (signed-in identity registry), action_logs, admin_audit_logs and metadata. Bootstrap/seeding/migration is transactional and environment markers reject cross-environment databases. Legacy appstore_test state/logs migrate without deleting the original schema. Custom entries survive; outdated generated demos/scenarios are archived. Audits are retained.

Use your platform's PostgreSQL backup/restore tooling. Named volumes preserve data across ordinary Compose down/up, but down --volumes intentionally deletes it. Review backup ownership, retention and monitored storage before company-wide rollout. Database owners can alter event-protection triggers, so these are application-level append-only records rather than tamper-proof records against a DBA.

## API

- GET /runtime-config.js: selected environment and Test accounts only in Test.
- GET /api/me: verified current user with DB-backed role.
- GET /api/state: role-aware catalogue/reference snapshot and last 200 events for Admins.
- GET /api/apps: active/unarchived catalogue; optional tab, search, category and platform filters.
- GET /api/apps/{id}: available app detail.
- POST /api/admin/commands: validated, authorized CRUD/reference/access operations with expected revision; writes and audit are atomic.
- POST /api/events/search: one record per submitted search.
- GET /api/open/{appId}/links/{linkId} and corresponding documents route: recheck app/link active/archive state, resolve stored HTTPS destination, log and redirect. Test navigation appends a known testUser; Production ignores it and verifies gateway identity.

Production live Norm/Okta configuration has not been accessed by this project. A passing isolated Production-mode test does not prove the actual Norm tenant/gateway is connected.
