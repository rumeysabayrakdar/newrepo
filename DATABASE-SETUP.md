# PostgreSQL Test setup

## Included services

`compose.yaml` declares an application container, PostgreSQL 17 and a one-shot credential initializer. PostgreSQL's port is private to the Docker network; only the application publishes port 8080. Passwords are randomly generated once in a private volume, not source control or image layers. The app reads its password from a mounted file. Named volumes preserve the password and PostgreSQL data across app restarts/redeploys. PostgreSQL's standard entrypoint performs its own first-start database creation.

The Test backend uses Npgsql 10.0.3 and checks that the connected database name equals `TestDatabase:Name`. It then applies the embedded `src/AppStore.Web/Database/schema.sql` transactionally and verifies/creates the `Test` environment marker. It seeds `test-seed.json` once with `ON CONFLICT DO NOTHING`. Later starts preserve all saved state and logs. No Production schema/seed is executed: Production startup is blocked before database initialization.

## Existing managed PostgreSQL (Norm Dockerfile-only deployments)

A platform administrator must create the actual empty Test database and configure these values securely:

| Setting | Value |
| --- | --- |
| ASPNETCORE_ENVIRONMENT | Test |
| DOTNET_ENVIRONMENT | Test, if the platform sets it |
| Database__Enabled | true |
| ConnectionStrings__AppStore | The platform's secret Npgsql/PostgreSQL connection string |
| TestDatabase__Name | Exact name of the Test database in that connection |

Do not put the connection string/password in the ZIP, GitHub, JavaScript, or chat. Use the platform's TLS settings; this application does not disable certificate validation. `Database__PasswordFile` is an optional alternative for providing the password through a mounted secret; Compose configures it automatically. The first-start database account needs permission to create the dedicated schema, tables, function and triggers. Production must use a different database, credentials and future configuration.

If `Database__Enabled=true`, a missing/mismatched/unavailable database stops initialization. It does not silently substitute local browser storage. With the database deliberately disabled, the standalone static/browser demo is still available and is labelled differently.

## Schema v1

- `appstore_test.catalogue_state`: singleton revisioned JSONB document containing apps, multi-country/category/type/platform arrays, active/archive flags, category reference values, translations and admin assignments.
- `appstore_test.action_logs`: distinct UUID/timestamp/actor/action rows for every search/open request.
- `appstore_test.admin_audit_logs`: distinct UUID/timestamp/actor rows with before/after snapshots for every admin mutation.
- `appstore_test.metadata`: environment and schema version marker.

This is a deliberately simple JSONB snapshot repository for the Test MVP, not a normalized per-app relational schema. API validation enforces app/reference constraints. Catalogue writes use a row lock, revision conflict detection, and audit insert in the same transaction. A failed audit or database write rolls back the change. Events have database triggers rejecting UPDATE, DELETE and TRUNCATE. A privileged database owner can alter/drop those protections; this is not tamper-proof storage against DB administrators. Admins see the latest 200 events in the UI; older records remain in PostgreSQL.

## Test API

In Test database mode, manually defined profiles come from the trusted server `test-seed.json` persona list. `X-Test-Persona` selects a known synthetic identity; country/type/manager status are derived on the server, not accepted from browser claims. Test fake identities are intentionally selectable by testers. This is not Production authentication.

- GET `/api/test/state`: role-aware snapshot. Employees receive eligible apps and no audit history. Admins receive all apps/reference values and audit history.
- POST `/api/test/admin/commands`: validated known admin operations only. Server checks the selected identity against current DB admin assignments and requires the expected catalogue revision. It ignores arbitrary client-provided actor/events.
- POST `/api/test/events/search`: a new row on each submitted search.
- GET `/api/test/open/{appId}/links/{linkId}?persona={testId}` and corresponding documents route: Test-only navigation rechecks server eligibility and resolves a stored destination by ID. It records the event before redirecting. The persona query is explicitly fake Test selection, not a production identity mechanism.

The known operations cover app creation/editing, active/archive/restore, category create/rename/remove-unused, translation updates and admin grants/revocations to existing Test profiles. No employee creation or hard app deletion API exists. The last Admin cannot be revoked. Manager status does not grant Admin.

The frontend uses server state in database mode, does not overwrite browser-local preview data, and refreshes on focus/every 10 seconds. Unsaved preview does not write to the database. A stale open editor is rejected on save even if background refresh has loaded a newer catalogue; the draft stays open.

## Start and verification (developer/IT)

```sh
docker compose up --build -d
```

On the published site, verify the **SHARED TEST DATABASE** banner. `/healthz` reports `mode: testdb`; this means the configured PostgreSQL connection initialized successfully at startup. Later database outages return an error instead of accepting local-only saves. Compose's PostgreSQL health check waits for startup before the app starts.

For synthetic integration tests, create an isolated Compose project and map its app to port 18082 (the included tests accept a custom base URL/project name). Then run:

```sh
npm ci
npm test
python tests/postgres-integration.py http://127.0.0.1:18082 arcelik-package-db-test
node tests/postgres-ui.cjs http://127.0.0.1:18082
python tests/database-guards.py arcelik-appstore:postgres-test arcelik-package-db-test
```

`postgres-integration.py` writes synthetic test records, invokes an app restart and checks trigger protections. Use it only on its isolated disposable Test project. Do not point it at real employee data. Local proxy/CA environments can pass the Dockerfile's optional BuildKit `proxy_ca` secret, using a trusted CA bundle; normal public-network builds need no extra file.

Production connection, migrations beyond v1, employee-data/gateway contract, backup/restore ownership and full production release remain future work. Norm's support for automatically deploying Compose from a ZIP has not been verified by this package.

## Norm appsettings connection discovery

The sole committed connection template now lives at `src/AppStore.Web/appsettings.json` → `ConnectionStrings:AppStore`. It contains only the nonsecret Compose Test host, database and username; there is no password. `appsettings.Test.json` enables database mode and declares the expected Test database name. Compose no longer duplicates or overrides those values; it supplies only the generated password-file reference and environment selection.

An explicit safe `appsettings.Production.json` clears the Test connection and disables the database/demo defaults. It contains no secrets and is included in the image. Production startup is still blocked by the application.

Refresh/reimport the latest GitHub commit in Norm before approving its appsettings option. The detected connection should now be in appsettings.json rather than compose.yaml. Norm must inject its real managed connection/password at deployment or through secret environment settings, never by committing credentials to GitHub. If its replacement changes the Test database name, also configure `TestDatabase__Name` to that exact name; the mismatch guard intentionally remains active. Norm's checkbox alone does not establish that it has provisioned a database.
