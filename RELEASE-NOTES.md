# Release 3.4 — 18 November 2026

Single-project TEST-selected package with the approved HTML review 32 integrated into the React / .NET / PostgreSQL application.

- Trusted gateway CIDR checks and settings removed from request handling and Production startup. Norm still authenticates Okta, forwards validated identity and prevents header spoofing/bypass. Read START-HERE.md.
- Compact cards/details, grid/compare only, hamburger-only mobile header, no mobile administration, red round profile avatar, compact web workspace hierarchy.
- Three-section editor, one 150-character description, closed multi-selects, multiple image/document upload to PostgreSQL (10 MB each), APK upload retained (100 MB).
- Company/country/mail-group targeting stored and enforced server-side. Norm maps the corresponding Okta fields into configurable headers.
- Feedback reaches the desktop administrator inbox. Shared state does not rely on preview local storage.
- All three documents updated to v3.4. Additive SQL included. No credentials, user DB export, actual installable APK or synthetic smoke-upload fixtures are bundled.

Passed focused local Node/.NET/PostgreSQL checks: real React render/save, favourites, comments, feedback, employee write denial, Gateway without CIDR, invalid identity rejection, document upload/download and company/group access, APK browser attachment download. Norm deployment, real Okta mapping and pixel/device/load acceptance were not performed here.

The same source runs Test or Production through AppStore:Environment in appsettings.json. Default Authentication:Mode is Gateway. Separate per-environment PostgreSQL connections and the matching public origin must be configured by Norm. TestProfiles is explicit Test-only, never a Production login mode.
