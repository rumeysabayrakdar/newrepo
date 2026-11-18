# Arçelik App Store — Technical Implementation
Version 3.4 · 18 November 2026 · HTML review 32 integration

## Stack and source
.NET 10 Minimal API / Npgsql 10 / PostgreSQL, React shell compiled by Node >=22 and Babel. Dockerfile uses the existing approved Norm .NET base images and serves port 8080. No framework or database replacement.
- app.jsx -> dist/app.js: authenticated bootstrap, retry, request/upload bridge and renderer lifecycle.
- dist/experience-view.js: approved view mounted inside the React-owned subtree; shared operations call the API. AbortController cleans up delegated listeners.
- dist/experience-template.js, experience.css, assets/: checked-in approved UI, local fonts and assets. npm build compiles the shell; the other view assets are already source/static build outputs.
- src/AppStore.Web/Identity/GateIdentityService.cs: Norm forwarded identity contract.
- Database/ExperienceDatabase.cs: JSONB catalogue, role checks, audience visibility, comments, preferences, feedback and redirects.
- Database/ExperienceFiles.cs: authenticated document/image storage and eligible retrieval.
- Database/ApkPackages.cs: existing APK storage/download.
Legacy source/schema records remain for compatibility; the legacy React entry is not loaded by dist/index.html.

## Environment and database
AppStore:Environment in src/AppStore.Web/appsettings.json is Test by default. Allowed values: Test, Production. appsettings.{Environment}.json follows the committed selector. Secrets come from Norm. Preferred variables: ConnectionStrings__Test and ConnectionStrings__Production (or TEST_POSTGRES_CONNECTION_STRING / PROD_POSTGRES_CONNECTION_STRING). Generic Norm PostgreSQL aliases remain supported. The database environment marker prevents accidental reuse across modes. Startup creates tables inside a provisioned database; it does not provision the PostgreSQL server or databases.
Hosting__Port=8080. Hosting__TrustedPublicOrigin must be the actual public origin. /healthz checks database connectivity. Ingress host/service/targetPort configuration remains a Norm task.

## Norm / Okta contract — CIDR removed
Authentication:Mode=Gateway is the default in both environments. There is no TrustedProxyCidrs setting, request peer-network check or Production CIDR startup requirement in this release.
Required headers: X-User-Sicil (nonempty employee ID), X-User-Id (nonzero GUID). Optional: X-User-Email (validated email). Additional claims have configurable names under Authentication:Headers:
| Setting | Default header | Meaning |
|---|---|---|
| Name | X-User-Name | Display name |
| CompanyCode | X-User-Company-Code | Company code |
| Country | X-User-Country | Country code, treated as an opaque string |
| Groups | X-User-Groups | Comma-separated mail-group IDs/addresses |
| Site | X-User-Site | Existing site/office targeting |
Norm authenticates Okta and MUST replace client-provided identity headers and prevent an unauthenticated direct backend path. Header syntax checks do not independently validate an Okta token. No client secret is in this package. Set Authentication__InitialAdminEmployeeIds__0 to the real initial administrator sicil. Missing identity returns 401 with a readable shell error; browser-supplied Test user selection cannot override Gateway mode.
Authentication__Mode=TestProfiles remains an explicit Test-only local smoke option and is rejected in Production. Synthetic personas require an actual Test administrator and cannot be used in Production. No mobile administrator UI is exposed.

## Targeting and access
Audience is JSONB on each app/web record: {scope: all|selected, companyCodes:[], countries:[], mailGroups:[]}. Verified request claims produce the employee profile; synthetic profiles can supply these values in Test. OR within each array; AND between populated arrays; case-insensitive. Empty selected audience denies everyone. Non-admin bootstrap excludes ineligible apps and web entries. Open/download, comment and file endpoints recheck eligibility server-side; admin mutation roles are checked independently of the UI. Existing targetLocation is retained unless replaced when editing audience.

## Endpoints and persistence
All API paths use the identity filter. Mutations require same-origin policy and JSON except the two authenticated multipart upload routes.
GET /api/experience; POST /api/experience/admin, personal, comments, feedback, event; GET /api/experience/open.
Admin snapshots lock catalogue rows, validate data and check revision; stale saves return 409. Comments are assigned a server author and cannot be overwritten by admin snapshots. Web-only comments are rejected. Feedback records are read by the admin feedback inbox. Browser storage holds only view preferences and optional feedback drafts, never the source of shared saves.
POST /api/experience/files accepts one multipart file (the UI queues a multiple selection). Extensions: PNG/JPEG/WebP/PDF/DOC/DOCX/XLS/XLSX, maximum 10 MB each. Content signature is checked; OOXML must contain its document structure. Bytes and MIME are stored in appstore.experience_files. Only the internal /api/experience/files/UUID reference is in catalogue JSON. GET of that path requires admin access or a reference from currently eligible content. Images render inline, other documents download as attachments, nosniff enabled. TestProfiles-only query identity is used for native browser image/download requests, never Gateway authentication.
POST /api/experience/packages retains APK up to 100 MB, structure/manifest validation and appstore.apk_packages storage. The apk:UUID reference is published on catalogue save. Open rechecks eligibility and returns bytes with the original download name. Server multipart/Kestrel limits are 110 MiB; Norm should allow at least 110 MB upload bodies. Unpublished uploads are retained; add lifecycle cleanup if needed.

## Migrations and limits
schema.sql adds experience_files; existing catalogue_state, experience_state, user_preferences, feedback, apk_packages and append-only logs remain. Original rows and resource labels/notes/colours are not bulk-deleted by UI simplification. Existing data is not reseeded on restart. Back up original and experience tables together before an upgrade. This ZIP contains source/schema/starter data, not a dump of a running database. Dashboard metrics remain sample values; no installation verification, mail delivery or external ticket integration is claimed.

## Checks
npm run build and dotnet build passed. Focused real local PostgreSQL checks cover authenticated render/save, favourite, shared comment, feedback inbox, employee write denial, no-CIDR Gateway contract, invalid/missing identity rejection, document storage/download and company/mail-group access, plus APK browser download. UI checks cover mobile hamburger, absence of mobile administration, list-preference migration, compact edit fields, navigation and close/discard logic. No full browser/device/load audit or live Norm/Okta deployment was performed. These are bounded package checks, not Production acceptance.
