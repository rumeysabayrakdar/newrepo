# What to do

## Review before another ZIP upload

Use the interactive chat preview for layout feedback. It is a labelled browser-local demo, not the Norm database or Okta system. We can keep updating that preview and create another deployment ZIP when changes are ready.

## Deploy these application fixes

1. Upload the complete updated ZIP to the existing appstore-x Test project.
2. Accept the PostgreSQL connection replacement for src/AppStore.Web/appsettings.json. Keep port 8080, optional Setup SQL empty, approve and deploy. Keep the existing database.
3. In Norm Env, set Hosting__TrustedPublicOrigin to https://appstore-x.norm.ar.arcelik if an older value is saved there. Environment settings override JSON values; this is the exact cause of the screenshot's blocked favourite save.
4. Norm's access protection is outside the app: disable the Okta gate on Test if you want no Okta prompt there. In-app Test always uses Zeynep/Admin and Rümeysa/Employee and ignores gateway identity.
5. Open the app. Confirm categories appear immediately and a selected favourite has a filled star after saving and refreshing.

## Production later

Set AppStore:Environment to Production in the deployed base appsettings.json. This switches the code to gateway authentication and hides the Test badge/account switcher automatically. Use a separate Production PostgreSQL connection and public address. Configure Hosting__TrustedPublicOrigin to the real Production website, Authentication__TrustedProxyCidrs with the immediate Norm gateway networks, and Authentication__InitialAdminEmployeeIds with the first Admin's real X-User-Sicil. Norm must protect the Production route with Okta, remove/overwrite client identity headers, and prevent direct access to the backend. These live platform settings cannot be inferred from a ZIP or a screenshot.

Do not change the Test database into Production or share its secret. Explicit expected database names are optional; blank means use the database in the supplied secure connection. The environment marker still prevents mixing Test and Production data.
