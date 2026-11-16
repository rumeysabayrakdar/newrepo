# Upload this TEST package to Norm

1. Open the existing TEST project in Norm and upload this complete ZIP.
2. Select PostgreSQL infrastructure if Norm asks. Accept the connection replacement for src/AppStore.Web/appsettings.json. No database-name editing is needed: the app reads the name from the supplied connection.
3. Keep container port 8080. Leave optional Setup SQL empty; the app creates its own tables. Approve the replacement and choose Confirm & Start.
4. Open /healthz on the deployed website. A working app returns JSON with database connected and environment Test.
5. Open the website. Zeynep is the Test Admin and Rümeysa is the Test employee. Save an edit and refresh to check persistence.

## Website address

This package preserves the address in your uploaded file:
https://arcelik-appstore-test-environment.norm.ar.arcelik

If Norm uses a different public website address, set Hosting__TrustedPublicOrigin in the project's Env settings to that exact address. This protects saving through the gateway. Do not use a database address here.

## What is ready

The ZIP includes the .NET API, built React interface and assets/fonts, pinned PostgreSQL driver package, database schema, seed data, migrations and Docker/Compose setup. Startup adds the required tables and seeds new stores once, retaining existing data. Temporary database-startup failures retry automatically. Test/Production selection is still exclusively AppStore:Environment in appsettings.json.

## What Norm supplies

Norm must create/attach the running PostgreSQL service and provide its secret connection. This is the platform's infrastructure step; no source ZIP can provision a managed database without that permission. The ZIP includes no real password, live data or deployment token. The included Compose setup also creates PostgreSQL and generates its password on a Compose-capable host.

This is a Test release. Production needs its own database, real auth-gate settings and verified first Admin. Do not change the working Test database to Production.
