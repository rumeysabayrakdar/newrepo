# Validation — real project 1.0

Validated on November 13, 2026 against isolated, running PostgreSQL 17 and .NET 10 containers in the development workspace.

## Passed

- React compilation and current UI tests (`npm run build`, `npm test`).
- .NET Release publish using the official Alpine .NET SDK/runtime and Npgsql 10.0.3.
- Separate Test and Production-mode PostgreSQL instances, generated secret files and persistent volumes; both `/healthz` endpoints report a connected database.
- JSON-only environment selection: contrary DOTNET_ENVIRONMENT, ASPNETCORE_ENVIRONMENT and AppStore__Environment values did not change the appsettings-selected environment.
- Real API integration (`tests/integration.cjs`): shared catalogue writes; admin permissions; categories, translations and roles; last-admin protection; revision conflicts; archive/restore; stored HTTPS links; repeated events; SQL append-only protections; data and event persistence after application restart.
- Production-mode identity tests used a simulated trusted gateway, not a live Okta tenant. Missing identity was rejected, Test identities were unavailable, verified identities were persisted, and users saw the same active catalogue without country, collar or manager claims.
- Live React DOM workflow (`tests/workflow.cjs`): one client's save appeared in another; stale saves were rejected while retaining the draft.
- Legacy database migration (`tests/migration.cjs`): custom catalogue data and event IDs survived; obsolete targeting fields were removed and scenario entries archived.
- Environment isolation (`tests/isolation.cjs`): Production refused a Test-marked database even when its configured expected name matched; the Test catalogue revision remained unchanged.

## Gateway-origin fix

The save check now uses Hosting:TrustedPublicOrigin when configured, so a public browser Origin can differ from the backend Host. The exact configured origin is required; hostile origins and cross-site requests remain rejected. The Release publish and current React UI tests passed. tests/origin-gateway.cjs passed against the running .NET/PostgreSQL Test service: public-origin/internal-Host save, fresh-read persistence, hostile-origin rejection without writes and restoration of the test app. This change must be redeployed in Norm before it affects the live site.

## Application translations

Release publish and existing React UI suite passed. tests/app-translations.cjs passed against live .NET/PostgreSQL: EN/TR draft switching and private preview, saving both dictionaries, fresh-client reads, localized cards/details/search, original and whitespace fallback, rejection of unsupported languages/invalid types/lengths, and Employee write denial. No destructive database migration is required; optional translations are stored with existing JSONB catalogue data. The declined application-backup feature is excluded from this update. Norm deployment and visual browser QA remain unverified.

## Practical limits

These running databases belong to this development workspace. This does not provision a database in Norm or verify Norm's GitHub importer, live routing or live Okta configuration. Hosting must run the supplied services or provide PostgreSQL and its secret connection settings. Production additionally needs the actual trusted gateway network ranges and initial administrator employee IDs.

The historical Sites preview is an older static demo, not this .NET/PostgreSQL application. Visual browser/mobile QA was not available; current frontend checks use React DOM tests and live HTTP services.

Catalogue data currently uses one revisioned JSONB document plus separate persistent user and append-only event tables. Schema changes and initial seed are transactional; initialization does not replace existing catalogue data. PostgreSQL owners can alter trigger protections. Platform backups, retention and production operations remain the hosting operator's responsibility.

Earlier test files are preserved for reference; `tests/README.md` identifies the current suites.
