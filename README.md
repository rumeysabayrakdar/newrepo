# Arçelik App Store — Norm TEST package

Start with [START-HERE.md](START-HERE.md). Upload the ZIP root as a .NET backend project on port 8080.

Runtime: .NET 10, React frontend and PostgreSQL 17 via Npgsql 10.0.3. There is no Python, Node server or TypeORM. The uploaded approved interface, images and fonts are retained. Node/Babel/jsdom are optional development tools, not deployment runtime dependencies.

The frontend is already built. NuGet.Config points at the included vendor/nuget package feed; publish was verified with networking disabled and an empty package cache. The .NET SDK/runtime and PostgreSQL container images are still supplied by the host/registry, not embedded in the ZIP. The Dockerfile retains your Norm registry mirrors.

## Database connection

The single discoverable connection template is src/AppStore.Web/appsettings.json. Norm can replace it with the project's actual private connection during upload. A blank Database:Name follows the selected connection's database instead of an old project's hard-coded name. An explicitly configured expected name is still enforced, and database environment markers prevent Test/Production reuse.

Supported hosting inputs: ConnectionStrings__AppStore, POSTGRES_CONNECTION_STRING, DATABASE_URL, standard connection aliases or a complete POSTGRES_HOST/POSTGRES_DB/POSTGRES_USER set. A supplied connection takes precedence over the bundled template. Passwords may be in the hosting secret connection, POSTGRES_PASSWORD, Database__PasswordFile or POSTGRES_PASSWORD_FILE. PostgreSQL URL TLS settings are preserved. Unknown URL options fail rather than silently dropping connection settings.

Startup applies the embedded additive schema and initializes the catalogue and experience model once. There is no browser-local data fallback. PostgreSQL stores shared edits, favourites, comments, feedback and activity. Local storage is used only for UI preferences. Existing saved experience content is retained. Test and Production start with the same six approved catalogue definitions; Test-only extra demonstration portal/profile content remains as supplied in the uploaded experience.

## Environment

AppStore:Environment in the base appsettings.json selects Test or Production. Environment variables cannot switch it. Production rejects missing trusted gateway networks, requires verified identity and must use a separate database. The supplied production Compose selector exists, but real gateway ranges/admin IDs/Production origin are intentionally not fabricated.

## Build and tests

Frontend edits: npm ci, npm run build. The deployed app does not run npm.

.NET publish: dotnet publish src/AppStore.Web/AppStore.Web.csproj -c Release. NuGet restore uses the bundled pinned package.

A Compose host can run docker compose up --build -d to create the Test app, PostgreSQL, persistent volumes and generated private password. This does not imply Norm runs Compose automatically; its Dockerfile-only workflow must attach PostgreSQL infrastructure.

Tests and practical verification limits are in TESTING.md and VALIDATION.md. Historical planning files in docs are reference documents, not instructions to reinstate superseded deployment settings. Dashboard sample numbers and pending external/Okta integrations remain as labelled in the supplied UI.
