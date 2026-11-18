# Release 3.4 checks

18 November 2026: Node/Babel build and .NET build passed. Local isolated PostgreSQL checks passed for React bootstrap, catalogue save, persisted favourites, shared comment, feedback receipt/inbox, employee write rejection, Gateway headers without any CIDR setting, missing/invalid identity rejection, image/document API upload permissions, PDF bytes and download eligibility, company/mail-group targeting and APK attachment download.

For the local smoke harness only, explicitly set Authentication__Mode=TestProfiles in Test. The delivered Gateway default expects Norm to authenticate and forward the identity headers described in START-HERE.md. The local synthetic forwarded-header check validates the application contract, not a real Okta login.

No exhaustive browser, device, accessibility, load or live Norm deployment test is claimed. Dashboard metrics remain illustrative. The bundled experience-live.cjs expects an isolated TestProfiles application at http://127.0.0.1:18088 and deliberately changes its test catalogue. Never run mutation smoke scripts on a live catalogue.
