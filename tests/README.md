# Active test suites

Run npm test for ui.cjs. Real database suites are integration.cjs, workflow.cjs, migration.cjs and isolation.cjs; see the root TESTING.md for isolated stack prerequisites. Tests use Node only as development tooling; the running backend is .NET.

Previously committed .py/.cjs suites are retained unchanged as historical v0.x regression references. Their demo modes, endpoint paths and employment filtering were replaced by the latest user requirements, so they are not run as current acceptance tests.
