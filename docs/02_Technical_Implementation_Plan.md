# Arçelik App Store — Technical Implementation
Version 3.0 · 16 November 2026 · TEST candidate

## Retained stack
.NET 10 / C# Minimal API, React, PostgreSQL with Npgsql 10. No backend switch to Node.js, EF Core or TypeORM. Node >=22 and Babel compile app.jsx. The Docker image serves compiled assets on 8080.

## Frontend structure
- app.jsx: React shell owns authenticated bootstrap, loading/error/retry and the renderer lifetime.
- dist/experience-view.js: isolated approved UI renderer and delegated interactions, mounted/unmounted by React. It is an imperative view module retained to port the approved design without rebuilding every screen; it is not a second application or local-data fallback. Listeners use AbortController cleanup.
- dist/experience-template.js: trusted static markup for the renderer root and native dialogs.
- dist/experience.css and dist/assets/: approved responsive presentation/assets. Incremental style overrides remain; CSS consolidation is still pending, not silently performed in this release.
- app.jsx builds to dist/app.js. Prebuilt renderer/template/CSS are checked-in source assets; npm build recompiles the React shell. legacy-app.jsx retains the previous React implementation for reference; it is not the main entry.

## Repository and schema
Database/AppStoreDatabase.cs remains authoritative for existing users/Admin roles, original catalogue and append-only logs. Database/ExperienceDatabase.cs adds a versioned JSONB experience repository. schema.sql creates:
- existing metadata, catalogue_state, users, action_logs, admin_audit_logs;
- experience_state(id=1, revision, payload, updated_at);
- user_preferences(user_id, payload, updated_at);
- feedback(id, user_id, topic, message, created_at).

Startup executes idempotent schema SQL, then initializes experience_state once from the current legacy catalogue. Existing application IDs are retained as legacyId while the approved UI uses stable mapped identifiers for known starter apps. Existing names, explicit translations, state flags, categories, links, documents and images are carried forward; original rows remain untouched. New web starter entries exist only in TEST. Future restarts do not overwrite experience_state. Back up both original and experience tables together. There is no automatic reverse synchronization to the legacy UI.

## API and authorization
All /api routes reuse the original authenticated identity and same-origin JSON gate. The new routes are:
GET /api/experience; POST /api/experience/admin, /personal, /comments, /feedback, /event; GET /api/experience/open.
Admin saves lock the catalogue and experience rows, check revision, authorize server-side and validate HTTPS links, image formats/limits, identifiers and colours. Stale revision returns 409. Employee comments use their own endpoint and cannot overwrite catalogue snapshots. Authors are assigned server-side. Web comments are rejected. Personal selections are keyed by authenticated employee plus optional authorized TEST persona. Shared data has no local-storage persistence fallback.

Redirects resolve stored resources and recheck active/archive/office eligibility; success logs a link-open event, not an install. The gateway remains responsible for genuine Production identity; clients cannot select TEST identities in Production. In TEST, synthetic persona selection requires the real actor to be Admin, and effective employee role blocks admin writes. Real Okta office/country/company claims remain an integration task.

Feedback is persisted and shown under Administration > Activity. There is no external mail/ticket delivery. Dashboard illustrative metrics remain marked as sample data.

## Configuration and deploy
AppStore:Environment in appsettings.json selects Test; keep this archive in a TEST project. PostgreSQL configuration supports ConnectionStrings__AppStore, ConnectionStrings__Postgres, POSTGRES_CONNECTION_STRING, ConnectionStrings__DefaultConnection, ConnectionStrings__Default, DATABASE_URL, or POSTGRES_HOST/PORT/DB/USER/PASSWORD. Credentials remain platform secrets. Norm must provision/attach PostgreSQL and inject its variables. No code replacement mapping should be needed when those variables are provided. The selected database name is checked, and Test/Production markers prevent cross-environment reuse.

Hosting__TrustedPublicOrigin must match the actual public HTTPS origin behind a TLS gateway. This release uses the previously supplied TEST hostname as its template; update it if Norm assigns a different hostname. Docker EXPOSE, Kestrel and platform targetPort must all be 8080. GET /healthz verifies database availability. A generic nginx 404 is still a hosting route issue, not solved by schema import.

SQL files and seed data are included. They are schema/setup artifacts, not a live PostgreSQL server or an export of the user's deployed database. Tables are created by startup; do not also import starter SQL/data into an existing catalogue without reviewing it.

## Checks and remaining work
Bounded checks: npm build, dotnet build, live local PostgreSQL integration of React bootstrap/save/favourite/comment and employee write denial; focused API checks for conflicts, feedback, redirects and TEST persona authorization. Existing deployment, real Okta, browser pixel geometry, accessibility audit and load/security testing are not claimed complete. Production release remains separate.
