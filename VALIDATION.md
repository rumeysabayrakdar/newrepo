# Validation — shared PostgreSQL Test package

## Passed

- Frontend compilation and the full existing catalogue/admin, profile matrix and auth-gate UI suites.
- Shared-mode DOM checks: server state, known Test-profile switching, server-derived admin state, no localStorage overwrite.
- C# PostgreSQL backend compiled in the .NET 10 Alpine container with Npgsql 10.0.3; optional build CA secret used only in the build stage.
- Real PostgreSQL 17 Compose stack: private database service, generated secret file, persistent volumes, automatic schema/seed and startup health ordering.
- API checks across separate employee clients: country/type/manager filtering; employee denial of admin writes; create/edit/archive/restore; category operations; translations; admin grants/revocations and last-admin protection; revision conflict; safe links and ineligible direct-link denial.
- Repeated searches, opens and admin changes each get separate UUID records. SQL triggers rejected UPDATE, DELETE and TRUNCATE attempts against event history.
- App restart retained saved apps, translations and all event IDs. Initialization did not overwrite stored data.
- Live two-browser-DOM workflow: one browser's save appeared in another; a stale open editor was rejected while preserving its draft and the newer database value.
- Wrong expected Test database name and Production startup were blocked before Test seeding.
- ZIP required files and integrity verified; database contents/actual secrets and intermediate tools excluded.

Tests are in tests/postgres-integration.py, tests/postgres-ui.cjs, tests/database-guards.py and the npm test suite. They use isolated synthetic Compose services, not Norm's project or real employee data.

## Limits

Norm's ZIP/GitHub importer has not been verified to run compose.yaml or provision its services. This ZIP cannot grant the importing platform infrastructure permissions. Dockerfile-only hosting requires a separately configured PostgreSQL database.

The published Codex static preview remains browser-local. Shared PostgreSQL mode runs from the .NET package on a suitable host. Database schema v1 uses one revisioned JSONB catalogue/reference/admin document plus separate append-only log tables, rather than normalized per-entity tables. Database owners can change trigger protections. Production, real gateway attributes/admin bootstrap, backups/retention and full visual/mobile QA remain pending.

## Appsettings discovery update

Parsed all appsettings files and validated Compose. The sole committed connection template is now in appsettings.json with Test-only activation/name settings; Compose supplies the mounted password reference without overriding the connection. Verified the Test app against real PostgreSQL using these JSON defaults: shared reads/writes, targeting, admin/reference changes, conflict checks, immutable events and restart persistence passed. Production has an empty connection override and no committed credentials. The standalone synthetic-container check explicitly disables the database when testing browser-only mode. Norm replacement execution itself remains unverified.
