# Arçelik App Store — complete Test package

React catalogue and C#/.NET 10 backend, now with **shared PostgreSQL saving**. The ZIP includes the app, PostgreSQL service, automatic table setup, generated credentials, persistent Docker volumes and Test seed data.

**Start with [START-HERE.md](START-HERE.md).** Norm must support running the bundled Compose services or provide a managed PostgreSQL database. Importing source code alone does not grant platform infrastructure permissions.

## What works

- Test profiles for Türkiye/Spain, white-collar/blue-collar employees and managers; Zeynep is the initial Test Admin. Manager is not an application role.
- Country/type/manager filtering on both the shared Test backend and frontend; inactive/archived apps are hidden from employees.
- Catalogue tabs, search, categories, details, platform links, screenshots and documents.
- Shared app creation/editing, private unsaved preview, immediate save, active/archive/restore, categories, translations and Admin assignment to existing Test profiles.
- Separate database rows for every repeated search/open/admin action, with unique IDs and actor/time metadata. Admin mutations and their audit rows commit together.
- Concurrent-editor conflict checks, including an editor kept open during a background refresh. Conflicting drafts stay open.
- Shared changes visible to other browsers and preserved on app restart.

Production startup remains blocked. Test users are synthetic; there is no application Okta login. The retained Development auth-gate pilot is documented in [OKTA-SETUP.md](OKTA-SETUP.md); real employee-data and production rollout remain pending.

## Start the app and database together

On a host with Docker Compose:

```sh
docker compose up --build -d
```

The app publishes port 8080. PostgreSQL stays on the private Compose network. The initializer generates a password privately in a Docker volume; no password needs to be pasted into chat or source control. A separate named volume holds the database. Regular container restarts preserve the data.

Look for the **SHARED TEST DATABASE** banner. [Database and hosting instructions](DATABASE-SETUP.md).

## Norm deployment

Ask whether Norm's ZIP import deploys `compose.yaml`. Its support has not been verified. If supported, this package can start both services. If Norm only builds the Dockerfile, an administrator must provide a Test PostgreSQL database and configure Database__Enabled=true, secret ConnectionStrings__AppStore, and exact TestDatabase__Name. The app initializes the dedicated Test schema and seed automatically after a valid connection.

When database mode is enabled, unavailable/mismatched configuration stops initialization; saves never silently fall back to browser storage. With the database intentionally disabled, the static/browser preview remains available with a different banner.

## Preview and Test acceptance

[Open the static preview](https://arcelik-app-catalogue-mvp-rumeysa.slatepond16.chatgpt.site/). This host cannot run the .NET/PostgreSQL service, so this preview still saves its own data in the browser. Shared database mode is supplied in the complete ZIP for a suitable host.

Use the **Test employee** selector and **Test scenarios** category. [Expected app/profile matrix](TESTING.md). The original screenshot apps are included: Perakende Akademi, Servis Akademi, Perakende Akademi AR, Okta Verify for China, BizPuan iOS/Android and SiriusTech. Their download addresses are pending, and screenshot version numbers are historical examples. Previous browser-only edits are retained in the browser but are not automatically imported into a newly seeded database.

## Main files

```text
compose.yaml                         Application + PostgreSQL + secret initializer
Dockerfile / .dockerignore            Tested .NET container build
app.jsx / dist/                      React source and precompiled site
src/AppStore.Web/
  AppStore.Web.csproj                 .NET 10 with Npgsql 10.0.3
  Program.cs                         Environment, routes and database bootstrap
  Database/TestDatabase.cs            Transactional PostgreSQL repository/service
  Database/schema.sql                Embedded v1 schema and append-only triggers
  test-seed.json                     Synthetic Test profiles/catalogue/reference seed
  appsettings.json                   Nonsecret PostgreSQL connection template
  appsettings.Test.json              Synthetic Test environment defaults
  Identity/                          Retained trusted auth-gate identity reader
  Catalogue/                         Retained gateway-pilot read-only catalogue
tests/                              DOM, Docker, PostgreSQL and two-client checks
START-HERE.md / DATABASE-SETUP.md      Upload/hosting and database guide
TESTING.md / VALIDATION.md            Profile matrix and completed checks
docs/                               Original planning documents (planned scope)
```

Your original four templates are adapted as README.md, Dockerfile, src/AppStore.Web/AppStore.Web.csproj and .dockerignore (the original file named download). No actual database contents, credentials, Git history, node_modules or tool caches are bundled.

## Development and tests

Node.js 22+:

```sh
npm ci
npm run build
npm test
```

The shipped dist is precompiled. Static preview uses explicit local-demo runtime settings; the .NET host overrides runtime-config.js with its actual configured mode. Database mode uses server state and does not write shared data into localStorage.

[PostgreSQL integration and two-browser test instructions](DATABASE-SETUP.md). [Completed verification and limits](VALIDATION.md).

## Storage design and limits

The Test MVP stores catalogue/reference/admin state in a revisioned JSONB document, with separate action and admin-audit tables. Arrays preserve multiple categories, countries, employee types and platforms. SQL row locks and revision checks prevent lost updates. Logs reject UPDATE, DELETE and TRUNCATE via triggers; a privileged database owner can still change schema/protections. This is not production tamper-proof auditing. Admins see the latest 200 events; older rows remain stored.

Future work includes the real gateway attribute contract, a separate Production database/configuration, reviewed real inventory, production identity/role assignment, backup/restore ownership and release validation. No employee creation, Super Admin role, approval queue or binary hosting is included.

Database connection discovery for Norm now uses the template in `src/AppStore.Web/appsettings.json`. Test enables the shared database via `appsettings.Test.json`; Production has a separate empty override. Passwords remain in mounted secrets or platform secret settings. See DATABASE-SETUP.md.
