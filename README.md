# Arçelik App Store

The real application catalogue: **.NET 10 + React + PostgreSQL 17**. There is one application and one database-backed implementation. No localStorage database, static demo backend, country targeting, collar-type targeting or manager targeting is used.

## What users can do

Everyone sees the same active, unarchived applications. Browse Developed by Arçelik / Other applications and filter by search, category and platform. Open details, links and documents. Administrators manage applications, versions, links, screenshots, documents, categories, translations, archive/status and Admin assignments. Unsaved preview stays private; saved changes are shared immediately.

The initial inventory contains the six applications you supplied. Download addresses remain pending until an Admin enters verified URLs. No imaginary download destinations are seeded.

## Application content languages

Edit an application and use Application translations → Content language to enter English (en) and Turkish (tr) names, summaries and descriptions. Original fields remain the fallback for each empty translation. Both languages save together in PostgreSQL. The header language selector updates cards, details and catalogue search; Admin search uses the selected name. Private preview follows the editor content language. Version, platforms and destinations stay shared. No automatic translation is performed and existing content is not rewritten.

## Current agreed scope

See [ACCEPTANCE-STATUS.md](ACCEPTANCE-STATUS.md) for ticket decisions. HTTPS APK download addresses are supported; APK binaries are served from company storage or another download host. Every authenticated open attempt records its result (redirect issued or blocked), reason and HTTP status. External download completion or installation cannot be inferred from a redirect. Phone verification, full UAT and operations acceptance are deferred by user instruction. Okta is configured in Norm.

## Test and Production

The **only environment switch** is `AppStore:Environment` in `src/AppStore.Web/appsettings.json`:

```json
"AppStore": { "Environment": "Test" }
```

Set it to `Production` to load `appsettings.Production.json` instead of `appsettings.Test.json`. ASPNETCORE_ENVIRONMENT, DOTNET_ENVIRONMENT and AppStore__Environment variables cannot change this selection. Hosting secrets may still override connection/password/gateway settings; they never switch the selected environment.

- **Test:** PostgreSQL is mandatory. The account selector offers Rümeysa (Employee) and Zeynep (initial Admin). No application Okta flow is used.
- **Production:** Norm's Okta/auth-gate handles sign-in. The backend verifies its trusted source and standard identity headers. No Test selector or synthetic identities are available. All employees can browse the same active catalogue; no employment attributes are requested or required.

A separate database name, environment marker, data volume and generated secret are used for each environment. Cross-environment reuse is rejected.

## Start Test

On a Docker Compose host:

```sh
docker compose up --build -d
```

Open port 8080. PostgreSQL starts automatically, its password is generated privately, tables are created, and the six-app inventory is seeded once. Data survives app restarts. Do not remove data/secrets volumes unless you intentionally want to erase that environment.

You do not need to install anything on your own PC to upload the project to a compatible host or use the deployed website. See [START-HERE.md](START-HERE.md).

## Production deployment

[DEPLOYMENT.md](DEPLOYMENT.md) describes the exact production requirements. Configure the real database, trusted auth-gate networks and the employee ID of the first Admin. Do not guess gateway networks or commit credentials. The code does not implement a second Okta/OAuth login page; it uses the gateway integration you specified.

`compose.production.yaml` supplies a separately isolated database and mounts the Production selector. Its web port is bound to host loopback for use behind a trusted gateway. It will not start until trusted gateway networks are configured. This is deliberate, not a fallback to Test.

Live Norm infrastructure/access settings cannot be provisioned from this repository. Norm must either run the Compose services or supply a managed PostgreSQL connection. ZIP/GitHub import support and gateway policy are hosting responsibilities, not client-side configuration.

## Code

```text
app.jsx / dist/                         React source and built static assets
src/AppStore.Web/Program.cs             .NET API, JSON environment selection and hosting
src/AppStore.Web/Identity/              Verified gateway identity; no employment claims
src/AppStore.Web/Database/              PostgreSQL service, transactions, validation, schema
src/AppStore.Web/catalogue-seed.json    Six user-supplied app listings and reference text
src/AppStore.Web/appsettings*.json      Test / Production settings
compose.yaml / compose.production.yaml Separate database/data/secret services
Dockerfile                             .NET serves the API and React assets
```

Node/Babel and jsdom are development/build/testing tools only. There is no Node/Python backend, extra database, cache service or additional application framework.

## Persistence and access

PostgreSQL stores a revisioned JSONB catalogue/reference/admin document, registered users, separate action logs and separate admin audit logs. Multiple categories and platforms are retained. Each mutation and its audit row commit in the same transaction; conflicting edits are rejected without discarding the draft. User events receive distinct UUID rows, including repeated actions. Log UPDATE/DELETE/TRUNCATE operations are rejected by database triggers; a database administrator can still alter schema/protections.

Roles are Employee and Admin only. Production grants target users who have actually signed in. Initial Admin bootstrap applies only to explicitly configured verified employee IDs and is audited once. Country, worker type and manager fields are not read from identity and do not affect catalogue visibility. Active/archive state still controls availability.

Existing PostgreSQL v0.x state and event IDs migrate into the shared `appstore` schema transactionally. Custom apps/edits are retained; obsolete generated scenario/sample entries are archived rather than deleted. Old targeting fields are removed from the current catalogue model. Browser-only data from the former demo cannot be imported automatically.

## Build and test

```sh
npm ci
npm run build
npm test
```

[TESTING.md](TESTING.md) describes the real PostgreSQL integration, two-client workflow and upgrade checks. [VALIDATION.md](VALIDATION.md) records verification and remaining live-host limitations. Historical planning attachments under docs/ describe older requirements; this README and the latest user requirements supersede their employee-targeting rules.
