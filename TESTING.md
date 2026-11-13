# Test the real application

Test uses a running PostgreSQL database, not browser-local saving. The account selector offers Rümeysa (Employee) and Zeynep (initial Admin).

1. Browse both catalogue tabs. Search for an app and choose category/platform filters.
2. Open details. All listed platform options are available independently of device or employee information. Pending addresses show Coming soon.
3. Choose Zeynep → Administration. Add/edit an app and inspect the private preview. Nothing changes until Save.
4. Save and open the site in another browser. Both see the same saved catalogue.
5. Change active/archive state. Employees do not see inactive/archived entries. Restore without deleting history.
6. Manage categories, translations and Admin access for existing accounts. Manager/country/collar fields do not exist.
7. Review Activity. Repeated actions produce separate records. A click is not proof of installation.
8. Open the same editor in two browsers, save one, then attempt to save the older draft. The older draft remains open and does not overwrite the new value.

Development commands:

```sh
npm ci
npm run build
npm test
```

Current automated suites:
- tests/open-outcomes.cjs: real PostgreSQL APK/document redirects, repeated and blocked attempts, persisted outcomes and unsafe legacy URL rejection.
- tests/app-translations.cjs: live React/PostgreSQL EN/TR editing, preview isolation, persisted translations, localized browsing/search, fallback and API validation.
- tests/origin-gateway.cjs: real PostgreSQL saves using the public Origin and an internal Host; rejects wrong origins/cross-site requests and restores the edited test record.
- tests/ui.cjs: real React screens, manual filters, no employment fields/local-storage dependency, save conflict feedback and Production account separation.
- tests/integration.cjs: isolated Test and simulated-gateway Production stacks, actual PostgreSQL, persistence/restart, admin rules, manual filters, environment isolation, safe redirects and append-only events.
- tests/workflow.cjs: two React clients against the running .NET/PostgreSQL Test service.
- tests/isolation.cjs: Production refuses an existing Test database without changing it.
- tests/migration.cjs: isolated legacy schema upgrade preserving custom data and event IDs while removing targeting.

The integration suites expect isolated local Compose stacks named arcelik-real-test and arcelik-real-production. They deliberately write synthetic records and restart the Test application. Never run them against live employee data. Test URLs default to ports 18086 and 8081 and can be overridden with TEST_URL/PRODUCTION_URL. The migration check uses a disposable extra database on the isolated Test cluster.

Older v0.x test files are preserved for historical reference. They cover requirements that were explicitly replaced (demo storage and employee targeting) and are not the current test command.

Phone verification, full UAT (QA-02) and operations checks (OPS-01) are not current acceptance gates per the latest user instructions. Automated focused regression checks still run for implementation changes.
