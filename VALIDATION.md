# Norm TEST package validation — 16 November 2026

- Preserved the uploaded approved frontend, local images, fonts and experience assets; rebuilt app.jsx.
- Published the changed .NET backend with the official .NET 10 Alpine SDK/runtime.
- Restored/published again with an empty package cache and networking disabled using bundled Npgsql 10.0.3.
- Started against a fresh, differently named PostgreSQL database using standard POSTGRES_* environment settings, proving the JSON template does not override hosting settings.
- /healthz returned status ok, database connected, environment Test, even with contrary runtime environment variables.
- Norm-style JSON replacement, DATABASE_URL and Npgsql environment injection all started successfully with a generated database name; explicit mismatches and Test-database reuse in Production were rejected.
- Shared title, personal preferences and app comments survived an application restart.
- Both Compose files pass docker compose config validation.
- API and real React DOM tests passed: Admin save, persistent favourite, shared comment, feedback, stale-save rejection, wrong-origin rejection, redirects and persona/RBAC checks.

The workspace could not start a third PostgreSQL container due to disk limits. Verification used a new isolated database on the already running development PostgreSQL service. Existing live Norm projects and databases were not accessed. The supplied Compose configuration is validated separately, and startup schema creation is the application's responsibility.

Native visual browser QA, live Norm importer/registry/ingress behavior, live Okta configuration and production operations have not been verified. Norm must attach its PostgreSQL service and supply the secret connection; the archive itself cannot grant infrastructure permissions. The supplied public origin must match the deployed website.
