# Arçelik App Store — interactive MVP

A working React application catalogue inspired by appstore.arcelik.com. This package includes the complete browser-local MVP, hosting-managed sign-in, and an optional C#/.NET host for serving its static files.

**Current status:** demo for review. Shared database, secure backend APIs, server-side authorization and trusted hosting-identity integration are not implemented. Do not use this package as a production employee portal.

## Try it without installing anything

[Open the hosted MVP](https://arcelik-app-catalogue-mvp-rumeysa.slatepond16.chatgpt.site/). The current hosted preview has its existing private access policy. It is independent of the Docker host supplied here.

[Upload instructions for GitHub](UPLOAD-TO-GITHUB.md).

## What works

- Direct catalogue entry with no additional login page. Hosting handles Okta sign-in.
- Two catalogue tabs: Developed by Arçelik and Other applications.
- Search on Enter/submit, category filters, app details and desktop split browsing.
- Global/multiple-country and employee-type targeting in the demo UI.
- All assigned eligible platform links, sample screenshots and document links.
- Synthetic personas: Rümeysa (Türkiye/Office employee), Zeynep (Türkiye/Office admin), Alex (UK/Field employee).
- App creation/editing, private unsaved live preview, immediate save, multiple categories/countries/types/platforms, links, screenshots and documents.
- Separate active and archived flags; archive/restore retains records.
- Category management, English/Turkish catalogue text and admin grants/revocations to existing demo identities.
- A new distinct activity record for every submitted search, app/document open and admin mutation, including repeated actions.

Only Employee and Admin roles exist. No Super Admin, approval queue, employee creation, binary hosting, analytics/reporting or dashboard roadmap is included.

## Where data lives

All apps, assignments, translations and activity use this browser's localStorage (`arcelik-mvp-v1`). Refresh keeps demo data; another browser has independent data. Website edits are not included in this ZIP. Reset is available under About. Demo permissions and local logs can be modified by the browser user; they are not secure authorization or immutable server audit storage. No company systems are connected.

Sample Arçelik listings link to the public corporate website, and third-party listings link to public services. The preview image and document are labelled samples, not real internal app assets. A click is not proof of installation.

## Sign-in

The app opens directly into the catalogue. Okta sign-in is handled by the hosting platform; there is no second login screen, demo-entry button or frontend Okta SDK flow. Keep Norm's Okta access restriction enabled. Its configuration has not been independently verified here.

[Hosting sign-in notes](OKTA-SETUP.md). The frontend does not yet read real identity/claims. The persona selector is for synthetic demo testing only. Hosting sign-in does not turn these demo roles into secure real-user authorization.

The optional .NET host serves static files and `/healthz`. It does not authenticate requests itself. If deployed elsewhere without hosting access controls, the sample catalogue is directly accessible. Shared data and protected APIs still require the backend.

## Project contents

```text
app.jsx                         Editable React UI source
package.json / package-lock.json Pinned frontend build/test tools
scripts/build.cjs               Compiles JSX to dist/app.js
tests/                          Catalogue/direct-entry interaction checks
dist/                           Complete precompiled browser app
  index.html                    Direct catalogue entry
  react*.js                     Vendored React browser libraries
  style.css / guide.html / sample-screen.svg
src/AppStore.Web/
  AppStore.Web.csproj            .NET 10 LTS project
  Program.cs                    Demo static host and health endpoint
Dockerfile                      Multi-stage .NET demo-host build
.dockerignore                   Adapted uploaded `download` file
OKTA-SETUP.md                    Instructions for IT
UPLOAD-TO-GITHUB.md              Browser upload steps
VALIDATION.md                   Checks performed and known limits
```

The four uploaded templates are adapted as `README.md`, `Dockerfile`, `src/AppStore.Web/AppStore.Web.csproj` and `.dockerignore`. The original templates described an older architecture; unsupported backend claims, Norm dependencies and Super Admin roles have been removed. .NET 10 LTS replaces the template's .NET 8 target. The three original planning documents are in `docs/`, labelled as planned scope rather than delivered functionality. No secret, Git history, hosting-specific project ID or local tooling cache is included.

## For a developer: edit and check the frontend

Requires Node.js 22 or newer. The precompiled site works without this step; run it when editing `app.jsx`.

```sh
npm ci
npm run build
npm test
```

Changes to styles and assets in `dist/` are used directly. `npm run build` only recompiles app.jsx. React 18.3.1 is vendored in dist; Babel and jsdom are development-only dependencies. Optional Google Fonts has a system-font fallback.

## For a developer: run the optional .NET host

Requires .NET 10 SDK. Explicit demo opt-in is required; Production mode always rejects startup.

```sh
dotnet publish src/AppStore.Web/AppStore.Web.csproj -c Release -o ./publish
```

Linux/macOS:

```sh
ASPNETCORE_ENVIRONMENT=Test AppStore__EnableDemo=true ASPNETCORE_URLS=http://localhost:8080 dotnet ./publish/AppStore.Web.dll
```

Windows PowerShell:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Test"
$env:AppStore__EnableDemo="true"
$env:ASPNETCORE_URLS="http://localhost:8080"
dotnet ./publish/AppStore.Web.dll
```

Open http://localhost:8080. A separate terminal can check http://localhost:8080/healthz. Local hosting has no sign-in protection; it is only for testing sample data.

## Docker (optional)

```sh
docker build -t arcelik-appstore:mvp .
docker run --rm -p 8080:8080 -e AppStore__EnableDemo=true arcelik-appstore:mvp
```

The image listens on port 8080 and runs as the image's non-root app user. `/healthz` reports `mode: demo` and `backendConnected: false`. Configure the hosting platform's HTTP health probe for this route. No database connection string or server Okta secret is consumed by this static host.

## Rules chosen for the demo

- Empty country list = Global; employee types/platforms require at least one selection.
- Global and matching-country links are shown together, on every device.
- Search records are created on form submit, not every keystroke.
- Inactive or archived apps are hidden from employees; archive can be reversed.
- Every action gets a UUID and correlation ID. If a click record cannot be saved, the link does not open.
- HTTPS external links only; the bundled sample guide is an explicit local exception.
- At least one demo administrator must remain. Only unused categories can be removed.
- English/Turkish interface, English fallback. Descriptions remain the entered content.

## Still needed for the actual backend

C#/.NET application services and API endpoints; a confirmed shared database and migrations; server-side Okta validation and eligibility checks; first-admin provisioning and secure admin assignments; transactional append-only event/audit records; approved app inventory and URL policy; storage, backups, retention and production/test isolation. The .NET static host here is scaffolding for packaging, not that backend implementation.

See VALIDATION.md for completed checks and verification limits.
