# Arçelik App Store — Technical Implementation Plan
**Version:** 2.0 · **Date:** 2026-11-09 · **Status:** updated implementation baseline; unresolved items explicitly gated  
**Companions:** [General Design](01_General_Design_Document.html) · [Phase & Ticket Index](03_Implementation_Ticket_Index.md)

## 1. Scope and technology
Build the V1 lightweight internal application catalogue and external-link redirect website. **Backend: C# / .NET. Frontend: React.** Do not introduce another backend language. The application does not host installation binaries, install software, or administer post-install permissions. Follow the original V1 product documentation for the two catalogue tabs, search/category filters, responsive detail browsing, translations, application/document management and V1 event logging. Do not add post-MVP enhancements.

Code organization must be straightforward: thin API endpoints/controllers, explicit application services for business rules, typed models/DTOs, clearly named repositories/data access, validation, centralized error handling, and tests. Use .NET-supported libraries only for backend logic. Database engine, .NET/React versions, deployment conventions, and supported selected hosting capabilities require confirmation; do not invent them.

## 2. Confirmed business rules
- **Roles:** Employee/User and Admin only. **No Super Admin or Approver role.** Any existing Admin can grant Admin access to an existing employee identity; this action is audited. No employee creation or employee master-data maintenance in this application.
- **Publishing:** No approval queue or approval gate. An Admin can preview an unsaved application/edit, then save; validated changes become effective immediately on save. The preview is not publicly visible and does not persist or publish by itself.
- **Archive:** use an explicit Boolean `isArchived` for archival state; preserve archived records and history. Distinguish archive from `isActive` (visibility/availability) unless business owners confirm a unified rule.
- **Test:** fake login, selectable demo personas for each role, convenient switching between personas; production uses Okta. No fake login or synthetic data in Production. Test and Production use separate data/configuration.
- **Targeting:** applications support multiple countries, employee types and platforms; country selection empty means Global. Show every eligible assigned link regardless of current device. Missing required user context is an error.
- **Logs:** every action is a new immutable record, including repeated actions by the same person. Record download/open clicks, search actions (V1 source), application creation, admin edits/status/archive changes and Admin grants/revocations. Do not confuse clicks with completed installations.
- **Admin controls:** country and category selectors are searchable multi-select dropdowns with checkboxes; on mobile, use a full-screen or bottom-sheet multi-select with Apply/Cancel and selected-item summary. The catalogue category model must support the selected category cardinality; do not assume single category.

## 3. Component boundaries
1. **React employee experience:** two tabs (Arçelik-developed / Other), search, category chips, catalogue cards, desktop split-detail browsing, mobile full-screen detail, platform/link options, documents and localized text.
2. **React Admin experience:** application list and filters, create/edit form, live local preview, country/category multi-select, platforms, employee-type targeting, versions/links, documents, translations, active/archive actions, Admin access assignment and audit view (if authorized).
3. **.NET identity layer:** production Okta validation; test-only persona selector; normalized identity context (`userId`, `country`, `employeeType`, `language`, `isAdmin`).
4. **.NET catalogue service:** visibility, targeting, search, category and two-tab filtering; never rely on React for authorization.
5. **.NET admin service:** validate and save changes, admin grants, archive/active state and append audit rows in the same transaction when supported.
6. **.NET redirect/event service:** recheck eligibility, validate configured link, append a unique click record and redirect or report an error according to confirmed policy.
7. **Persistence and observability:** durable separate Test/Prod datasets, migrations, structured error logs, per-event business logs, correlation IDs.

## 4. Logical data model (database implementation to confirm)
| Entity | Minimum fields / relationships | Rules |
|---|---|---|
| Application | id, name, shortDescription, description, catalogueTab, version, iconRef, isActive, **isArchived**, createdAt, updatedAt | Archived rows retained; only published saved records visible to employees |
| ApplicationCategory | applicationId, categoryId | Many-to-many, supports Admin multi-select |
| Category | id, name, isActive, isArchived | Admin-managed reference data; clarify category archival effect |
| ApplicationCountry | applicationId, countryCode | Zero rows = Global; multiple countries allowed |
| ApplicationEmployeeType | applicationId, employeeTypeCode | Multiple; zero-assignment semantics require confirmation |
| ApplicationPlatform | applicationId, platformCode | Multiple; not a device-based link hiding rule |
| ApplicationLink | id, applicationId, label, platformCode?, countryCode?, version?, url, isActive, sortOrder | Multiple links; country-specific behavior requires explicit rules |
| ApplicationDocument | id, applicationId, title, url, languageCode? | URL/reference; hosting policy unconfirmed |
| ApplicationImage | id, applicationId, imageRef, sortOrder | Supports V1 detail screenshots; storage method unconfirmed |
| Translation | id, languageCode, key, value | Admin-editable page text; fallback policy unconfirmed |
| AdminAssignment | id, employeeUserId, grantedBy, grantedAt, revokedBy?, revokedAt? | Existing identity only; no employee record creation |
| ActionLog | id (unique per event), actorUserId, applicationId?, linkId?, actionType, searchTerm?, occurredAt, outcome, correlationId | One row per search or download/open action, even if repeated |
| AdminAuditLog | id (unique per event), actorUserId, entityType, entityId, action, occurredAt, beforeJson?, afterJson?, correlationId | One row per admin mutation, including Admin assignments |

Use independent event IDs; never upsert by actor/user. Archive Boolean is not a deletion mechanism. Preserve referential history. Define whether log records are strictly append-only, who may read them, retention and sensitive-data redaction before Production.

## 5. Configuration and authentication
Use environment-specific `appsettings` for environment mode and identity behavior, with secrets in approved secure storage. **Test:** fake login with clearly labeled demo personas, including at minimum `Zeynep-Admin` and `Rümeysa-Employee` (illustrative synthetic identities; names are not production accounts); optionally add employee variants for country/type eligibility tests. Switching personas replaces the test session and refreshes catalogue/permissions; no restart required. **Production:** Okta only, no synthetic seed or fake login. Fail startup for invalid Production auth configuration.

Production user identity, country, employee type and language are read from confirmed Okta claims. Admin authorization is server-side using the confirmed Admin-assignment mechanism; an Admin can grant/revoke Admin access for an existing employee ID. Do not seed or implement Super Admin. Confirm how the first Admin is provisioned before implementation. Missing required context must return an explicit error rather than broaden access.

## 6. Eligibility, browsing and downloads
For every catalogue/detail/link request: require identity context, exclude archived and inactive applications, apply country (Global or matching), employee-type and any confirmed link-specific targeting rules. Return all eligible links without device detection suppression. Country-specific links should appear only to the corresponding users; define Global fallback and overlap priority before coding. The two tabs, search and category filtering must be consistently applied. Record each search action as its own event (agree on the precise search-trigger event before coding).

Download/open flow: reauthorize server-side, resolve stored link by ID, validate safe destination, write a new event with actor/application/link/time/outcome, then redirect. Log failures according to agreed policy. No arbitrary client-provided redirect URL. Admin writes are immediate after successful validation and commit; preview reads only unsaved React form state, never the public catalogue.

## 7. Proposed API surface (illustrative; confirm routes)
| Method | Route | Behavior / permission |
|---|---|---|
| GET | `/api/apps` | Eligible catalogue; tab, search and categories |
| GET | `/api/apps/{id}` | Eligible app detail, images, documents, links |
| GET | `/api/apps/{id}/links/{linkId}/open` | Recheck, event, redirect |
| POST | `/api/events/search` | One search event per defined search action |
| GET | `/api/reference` | Categories, country/platform options, translations |
| GET/POST/PUT | `/api/admin/apps` / `/api/admin/apps/{id}` | Admin-only create/update; immediate save + audit |
| PATCH | `/api/admin/apps/{id}/status` | Active/inactive + audit |
| PATCH | `/api/admin/apps/{id}/archive` | Boolean archive + audit |
| GET/POST/PUT | `/api/admin/categories` | Admin-only category management + audit |
| GET/POST/PUT | `/api/admin/translations` | Admin-only translation management + audit |
| GET/POST/DELETE | `/api/admin/assignments` | Existing employee ID Admin grants/revocations + audit |
| GET | `/api/admin/audit` | Authorized audit access, pagination |
| POST | `/api/test/session/persona` | Test only; switch synthetic identity |

Admin app updates must support multi-category/country/platform associations. No draft-approval or employee-creation endpoints. Define pagination, validation, optimistic concurrency, error envelope and idempotency for mutations before implementation.

## 8. UI and responsive behavior
**Employee desktop:** three-column card grid at available width; opening a card switches to single-column list beside a half-page detail pane; selecting another card updates the pane. **Employee mobile:** single-column list and full-screen detail with prominent link selection/action near the top, plus screenshots and documents. Preserve both catalogue tabs and category chips.

**Admin desktop:** searchable multi-select dropdowns with checkboxes, selected chips/count, Clear, keyboard navigation and Apply/Cancel as appropriate. **Admin mobile:** full-screen/bottom-sheet selector, searchable checkbox list, Apply/Cancel and clear selected count; avoid tiny native multiple-select boxes. Category and country selections must persist correctly. Preview card/detail from current form state; saving applies changes immediately without an approval stage. Provide clear unsaved-change and save-error feedback. React frontend may use HTML/CSS/TypeScript or JavaScript as needed; no other backend language.

## 9. Security, privacy and operational requirements
Use .NET authentication/authorization middleware, HTTPS, secure sessions, CSRF safeguards where relevant, server validation, safe redirects and input/output sanitization. Restrict admin and audit endpoints. Log immutable distinct business events with correlation IDs, not secrets or access tokens. Separate application diagnostics from business/audit logs. Plan migrations, backup/restore, monitoring, rollback and support ownership using confirmed selected hosting capabilities.

## 10. Test and release strategy
- **Unit:** Global/multi-country, multi-category, employee-type targeting, two-tab filters, archive/active state, admin permissions, URL validation.
- **Integration:** Test fake login and switching, Production Okta validation, Test/Prod isolation, .NET persistence, atomic mutation/audit, per-event IDs.
- **E2E:** search log; list/detail/mobile browsing; all eligible links visible across devices; repeat download logs; add/edit app → preview (not public) → save (immediately public when eligible); category/country multi-select; archive; Admin grants by another Admin; repeat admin mutation logs; translations and documents.
- **Security:** unauthorized admin actions, direct URL/IDOR, malicious redirect, missing claims, fake login disabled in Production.
- **Release:** production inventory/links verified, claims and first-Admin provisioning confirmed, backup/rollback rehearsal, UAT and smoke tests.

## 11. Delivery phases
**P0 Discovery gates:** confirm runtime, database, repo, identity/first Admin, targeting and logging semantics.  
**P1 Foundation:** .NET/React skeleton, environments, schema, test personas, Okta, Admin RBAC.  
**P2 Functional employee experience:** APIs and minimal React two-tab catalogue, search/categories, detail, links, search and click logs. Functionality first; visual refinement later.  
**P3 Functional administration:** application/category/translation management, preview + immediate save, Admin assignment, archive and audit, using simple accessible controls first.  
**P4 Verification and usability:** security, functional UAT, essential mobile usability, migration, monitoring and Production deployment. Pixel-perfect visual design is not a launch prerequisite.  
See [Phase & Ticket Index](03_Implementation_Ticket_Index.md) for work items and dependencies.

## 12. Questions / decisions still open (do not assume)
1. Supported .NET and React versions, database engine, local development, hosting/deployment and repo decisions.
2. Production application inventory, country/employee-type/platform/category values and link-specific targeting/global fallback semantics.
3. Exact Okta claims, authoritative Admin-assignment store and **initial Admin bootstrap** mechanism.
4. Meaning of empty employee-type/platform selections; category archiving effects; archive vs inactive behavior and whether archived records can be restored.
5. Which user interaction counts as a search event (typing, submit or debounce); redirect logging fail-open/fail-closed behavior; allowed URL domains.
6. Translation languages/fallback, image/document storage, audit access/retention, backup and rollback ownership.

**Source limitation:** The legacy website's observed frontend assets do not establish its backend architecture. Do not infer reusable backend endpoints or database design from them.

## 13. Collaboration and execution without Norm
- Rümeysa and ChatGPT will build the application together. ChatGPT can author/review .NET and React source, tests, migrations, Dockerfile/build configuration where applicable, and run code/tests in an available sandbox. Rümeysa controls local environment setup, access to Okta/internal systems, secrets, infrastructure, source-control credentials and actual deployments; do not claim ChatGPT has direct access.
- Start with a working API, persistence and test personas; then minimal functional React screens and admin flows. Keep basic accessibility and mobile link access, but defer detailed visual styling and mockup fidelity until functionality is validated.
- No Norm token, PAT, Norm chat, Norm hosting or Norm database assumptions/dependencies. Decide development environment, database and deployment platform before committing related implementation choices.

## 14. Optional post-MVP candidate enhancements (not in MVP tickets)
After MVP acceptance, review possible additional service integrations, app-store image retrieval, external data fetching, and device-aware application filtering. Other suggestions to consider: automated link health checking and improved audit browsing. None is approved scope, and V2 does not specifically mean reports or dashboards.
