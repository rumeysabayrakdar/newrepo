# Test the employee scenarios

1. Open the application. It opens directly into Test mode, without an application Okta login.
2. Use the **Test employee** selector above the catalogue.
3. Select the **Test scenarios** category to see only the clearly labelled synthetic examples.

| Profile | Country | Employee type | Manager? | Expected test apps |
| --- | --- | --- | --- | --- |
| Rümeysa | Türkiye | White-collar | No | Türkiye Office Tools; Türkiye & Spain Hub |
| Emre | Türkiye | Blue-collar | No | Türkiye Team Tools; Türkiye & Spain Hub |
| Lucía | Spain | White-collar | No | Spain Office Tools; Türkiye & Spain Hub |
| Diego | Spain | Blue-collar | No | Spain Team Tools; Türkiye & Spain Hub |
| Elif | Türkiye | White-collar | Yes | Türkiye Office Tools; Türkiye & Spain Hub; Manager Workspace |
| Carlos | Spain | White-collar | Yes | Spain Office Tools; Türkiye & Spain Hub; Manager Workspace |
| Zeynep (Admin) | Türkiye | White-collar | No | Same employee visibility as Rümeysa; can edit targeting |
| Alex (extra UK scenario) | United Kingdom | Blue-collar | No | No Test scenarios apps; other eligible catalogue apps remain |

All identities are synthetic. Manager is an independent employee attribute, not an application role. It never grants Admin. The two application roles remain Employee and Admin.

To change a restriction, select Zeynep, open Administration, edit an app, choose countries/employee types or **Managers only**, preview, and save. Switch back to an employee to check the result. Empty countries mean Global. Archived/inactive apps remain hidden. The supplied real-app inventory and previous sample apps are retained; their example targeting is not an approved production mapping.

The synthetic example apps have no real download destination and show **Coming soon**. The previously imported screenshot apps still need approved download addresses. Existing browser data is migrated without resetting edited apps, roles or activity. Legacy Office/Field assignments are renamed White-collar/Blue-collar.

## Norm deployment

Upload the full ZIP as a new version. The packaged container already selects ASPNETCORE_ENVIRONMENT=Test; appsettings.Test.json enables the synthetic experience. Any existing Norm environment selector should also remain Test (both ASPNETCORE_ENVIRONMENT and DOTNET_ENVIRONMENT if present). The application forces synthetic mode in Test even when an older AppStore__Mode=AuthGate or EnableDemo=false setting remains.

This changes the application's identity mode only. Norm's own external access policy is separate; if it still requests company sign-in before the app opens, only the platform administrator can change that policy.

## Production remains pending

Production startup is deliberately blocked. Auth-gate integration is retained for developer integration testing in Development. Real country/classification and manager attributes must come from a confirmed trusted gateway contract. No country/classification/manager header name has been invented. A missing manager attribute is not treated as manager access. Shared database, real admin APIs/assignments and production readiness remain future work.

## Shared PostgreSQL mode

The complete package now includes compose.yaml with the app and PostgreSQL. Follow START-HERE.md and DATABASE-SETUP.md. In shared mode the banner says **SHARED TEST DATABASE**. Changes saved as Zeynep appear for other eligible employees/browsers, and activity is stored in PostgreSQL. Draft preview remains private. The static Codex preview is still browser-local. Newly initialized databases use the supplied sample seed, not another browser's saved edits.
