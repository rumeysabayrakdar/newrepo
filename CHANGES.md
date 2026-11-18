# Requested fixes — 18 November 2026

1. Arçelik wordmark, separator and App Store label align on the same vertical centre line.
2. AppStore:Environment alone determines authentication: Test always uses sample Test profiles; Production always uses the protected Norm gateway. Stale Authentication__Mode overrides cannot mix them. Production rejects missing/invalid trusted gateway networks and Test identities.
3. The TEST badge and preview-account switcher are rendered only in Test. Production uses the one signed-in identity supplied by Norm; it never reads a Test persona from browser storage.
4. Desktop categories are open on first render and do not collapse on unrelated clicks. A global click-outside handler was closing the native details element while its desktop summary was hidden. Mobile disclosure behaviour remains available.
5. The favourite error in the screenshot is an origin mismatch, not proof of a missing database. Test now uses https://appstore-x.norm.ar.arcelik. Successful favourite changes persist in PostgreSQL and fill the star. Failed saves leave selection unchanged and allow retry. Tables are created automatically on startup; existing data is retained.
6. Internal web/portal-tool cards now use the same icon/name/badge/star structure as application cards, replacing tall photo panels. The separate company portal section is retained. Shared theme variables support light and dark modes.

Verification: .NET Release publish with offline Npgsql restore; actual Test and simulated trusted-gateway Production services with separate PostgreSQL databases; React DOM checks for initial category visibility, outside clicks, theme changes, persisted favourites/reload, failed-save retry, web-card structure and absence of Test controls in Production. The preview workflow also passes. Native visual browser QA and live Norm/Okta verification are not claimed.
