# Testing

Run these commands from the project root.

| Check | Command |
| --- | --- |
| Build the frontend | `npm ci` and `npm run build` |
| Build the backend | `dotnet build src/AppStore.Web` |
| Build and test the standalone preview | `python scripts/build-html.py` and `node tests/html-review.cjs` |
| Test the admin workflows against a fresh Test database | `node tests/admin-upgrade.cjs` |
| Test the Test and Production application contracts | `npm test` |

## Database-Backed Checks

Start separate app instances on ports `18088` (Test) and `18091` (Production), each connected to its own disposable PostgreSQL database. The application reads its mode from the deployed `appsettings.json`. For the local Production check, configure a trusted loopback proxy range and the public origin `https://prod.example.test`. These tests use synthetic identity headers and modify their disposable databases. Run the admin workflow suite first, since it checks that reporting starts with zero recorded activity.

The local Production checks cover the gateway header contract. Live Norm deployment and Okta sign-in require the hosting environment.
