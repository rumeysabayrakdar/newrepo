# Verification

Run npm ci and npm run build for frontend changes. With an isolated .NET/PostgreSQL Test service on port 18088, run npm test for the API and live React DOM workflow. Override TEST_URL and TEST_ORIGIN for another isolated test service. These suites write synthetic test records; never run them against live employee data.

- tests/experience-api.cjs: feedback persistence, origin checks, revision conflicts, redirects and uploaded Test persona permissions.
- tests/experience-live.cjs: actual React rendering, shared Admin edit, saved favourite, shared comment and Employee write denial against .NET/PostgreSQL.
- tests/norm-deployment.cjs: injected URLs/Npgsql connections and replaced JSON with a generated database name; explicit mismatch and Test/Production database isolation. Requires the documented development Docker fixture; its defaults are not live Norm credentials.

The normal Dockerfile builds .NET and serves the already compiled frontend. The dependency feed is local vendor/nuget, so NuGet does not need internet. Container base images still need the host's registry access.

Phone verification, complete UAT and operational acceptance remain deferred per prior user instructions. No live Norm gateway or database access was available here.
