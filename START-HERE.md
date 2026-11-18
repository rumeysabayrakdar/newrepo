# Start here

## Review the HTML

Download downloads/Arcelik-AppStore-Admin.html and double-click it. Everything needed to display the page is embedded. No Sites publishing is used. The banner clearly identifies browser-local review mode. Its activity starts at zero and reflects only this browser's interactions. It cannot connect directly to PostgreSQL or authenticate with Okta.

When the same HTML is served by the updated .NET app, it reads /runtime-config.js and uses the real same-origin API. An unavailable backend shows an error; it never silently switches a hosted application to sample data.

## Enable the database-backed changes once

1. Upload Arcelik-AppStore-Admin-Update.zip as a new version of the existing appstore-x Test project. Keep its existing database.
2. Accept Norm's PostgreSQL replacement for src/AppStore.Web/appsettings.json. Keep port 8080 and optional Setup SQL empty. Approve and deploy.
3. Keep Hosting__TrustedPublicOrigin=https://appstore-x.norm.ar.arcelik in Norm Env. An older Env value overrides the ZIP and can block saves.
4. Open Administration. Overview and logs now read PostgreSQL. Fresh history shows zero, not sample totals. New link requests are recorded; these are not installation counts.
5. In Appearance & language → Translations, enter a language name and standard code (Español/es, Italiano/it, Polski/pl, etc.). Choose a content group, search, edit and save. Missing translations use English.

The update creates missing schema/indexes and adds a language registry to existing catalogue data. It does not drop, recreate or reset the database. Existing applications, uploads, favourites and logs remain.

## Test and Production

Test uses sample accounts without in-app Okta. If Norm protects the Test route itself, disable that platform gate separately.

Production uses a distinct public URL/database. Set AppStore:Environment=Production, the real Hosting__TrustedPublicOrigin, trusted immediate gateway networks in Authentication__TrustedProxyCidrs and the initial Admin employee ID in Authentication__InitialAdminEmployeeIds. Norm must enforce Okta and overwrite client identity headers. The TEST badge, account switcher and scenario panel disappear automatically in Production.

Future visual-only reviews can use the single HTML. Server/database features still require the corresponding backend version deployed.
