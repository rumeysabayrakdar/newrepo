# Norm auth-gate integration

The application has no separate login page, OAuth flow, password handling or token validation. Norm auth-gate authenticates employees. This backend trusts injected headers only after checking the immediate network peer against an explicitly configured gateway CIDR allowlist. The host must also restrict all inbound app traffic to auth-gate and auth-gate must overwrite/remove all client-supplied identity/targeting headers. An IP allowlist alone is insufficient if another path on that same network bypasses auth-gate.

## Confirmed platform fields

- `X-User-Sicil`: employee ID, kept as a distinct string. This is not the internal user GUID. Default validation requires a nonempty, bounded, non-control, non-whitespace value; set AuthGate:EmployeeIdPattern only after confirming the organisation-specific format.
- `X-User-Id`: internal platform user ID, required and validated as a nonempty GUID.
- `X-User-Email`: optional, validated as a bare email address when supplied.

Absent, malformed or repeated identity headers are rejected. The application does not accept browser/query/body profile replacements. It does not trust X-Forwarded-For. The immediate gateway peer must match AuthGate:TrustedProxyCidrs.

## Country and classification: still unconfigured

Neither field is in the standard platform contract. No header names or default values have been invented. Platform IT must supply these from an authoritative employee-data source, configure auth-gate to inject them, and agree their exact names and allowed values.

Configure the following only after that agreement:

| Configuration key | Meaning |
| --- | --- |
| AuthGate:TrustedProxyCidrs | Exact immediate gateway/proxy source networks; empty rejects identity requests |
| AuthGate:CountryHeader | Agreed gateway-supplied country header name; empty disables catalogue access |
| AuthGate:WorkerClassificationHeader | Agreed gateway-supplied classification header name; empty disables catalogue access |
| AuthGate:CountryMap | Allowlisted ISO alpha-2 uppercase code → exact catalogue country value |
| AuthGate:WorkerClassificationMap | Allowlisted agreed classification value → exact catalogue employee-type value |
| AuthGate:EmployeeIdPattern | Optional organisation-approved employee-ID pattern |

For example, *if IT agrees* TR → Türkiye, white-collar → White-collar and blue-collar → Blue-collar, those can be entered as mappings. These examples are not a platform-defined classification contract. Unknown/lowercase/unmapped country codes and unknown classifications never broaden access. The mapping names currently used by the sample inventory are listed in src/AppStore.Web/catalogue.json.

ASP.NET environment syntax uses double underscores, e.g. AuthGate__CountryHeader and AuthGate__TrustedProxyCidrs__0. Do not put a real token or password in these settings. The included appsettings.json contains empty trust/attribute mappings by design.

## Backend API behavior

- GET /api/me: verifies the peer and standard headers. Returns employeeId and userId as different fields, optional email, profile attributes, canBrowse and profileStatus. When identity is verified but targeting is absent/unconfigured, canBrowse is false. No apps are fetched by the frontend in that case.
- GET /api/apps: returns only active, unarchived apps matching Global/country and employee type. Supports tab, search and category filters. Every request reads the gateway headers again.
- GET /api/apps/{id}: applies the same eligibility check; ineligible/nonexistent apps return 404.
- GET /api/apps/{id}/links/{linkId}/open and /documents/{linkId}/open: resolve server-stored destinations by ID, recheck eligibility/link country, append an event, then redirect. No caller-provided redirect destination is accepted. Inactive, pending and unsafe links are rejected.
- POST /api/events/search: authenticated JSON-only same-origin search events, one UUID per submission.
- Error responses include code, message and correlationId. Missing identity is 401; untrusted peers and ineligible targeting are denied. Missing gateway configuration returns 503 without falling back to demo.

The frontend fetches /api/me then /api/apps, with no identity headers constructed in JavaScript. It hides demo persona/admin controls and does not write company identity or catalogue data to localStorage. It rechecks the profile/catalogue when the window regains focus. The backend remains the authorization boundary even if a caller modifies frontend code.

## Deployment modes

The .NET container now defaults to synthetic Test mode. Test never reads the gateway profile. For developer integration checks, explicitly select Development and AuthGate mode. It can serve its shell/health endpoint before gateway configuration, but cannot provide an unrestricted catalogue. In Test, synthetic mode takes precedence even over stale AuthGate or EnableDemo settings. Production startup remains blocked.

The published Codex static preview uses an explicit browser-local demo configuration because that host cannot run this .NET backend. Deploy the full ZIP/Docker project to Norm to use auth-gate mode. Test/Development only: Production startup is rejected until the complete backend and inventory/storage review are ready.

## Inventory and logs

src/AppStore.Web/catalogue.json is a read-only server snapshot of the current sample catalogue, kept outside wwwroot. Configure AppStore:CataloguePath to an approved inventory snapshot if required. Apps use countries (empty = Global), employeeTypes (empty = no access), categories, isActive and isArchived. The ISO and classification maps translate the gateway contract to those inventory values; no device filtering is applied to links.

Pilot search/open events are appended to AppStore:ActionLogPath (default /app/data/actions.jsonl) with unique IDs and employee IDs. A failed write prevents the redirect. Mount /app/data on persistent storage for a pilot and run one replica: this file logger is not a shared database or a tamper-proof production audit store. Deployment without a volume loses logs on replacement. Admin mutation APIs and real admin grants remain unimplemented in AuthGate mode; browser demo edits are not imported automatically.

## IT handoff

Confirm the gateway's immediate source IP/CIDR, enforce gateway-only reachability, strip/overwrite identity headers, supply the two targeting attributes, and provide their header names plus allowed values/mappings. Verify with pilot employee accounts that user identity, targeting and direct-link denials match the trusted employee source. No passwords, OAuth tokens or client secrets are needed by this integration.

## Manager targeting

Test profiles carry an independent isManager flag. Manager-only apps are supported by the UI/editor. The retained backend also excludes manager-only apps unless a trusted manager attribute is verified; no such live attribute contract is configured yet, so it does not grant manager access. Manager is not an Admin role.
