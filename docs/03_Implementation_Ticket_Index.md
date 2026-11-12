# Arçelik App Store — Phase & Implementation Ticket Index
**Version:** 2.0 · **Date:** 2026-11-09 · **Status:** proposed tickets, proposed work items, not created in an external tracker  
**Sources:** [General Design](01_General_Design_Document.html) · [Technical Implementation Plan](02_Technical_Implementation_Plan.md)

## Rules for execution
Backend **C#/.NET**, frontend **React**. Roles: Employee/User and Admin only. Admins may grant/revoke Admin to existing employees. **No Super Admin, employee creation, draft approval or approval workflow.** Unsaved preview is private; validated saved changes take effect immediately. `isArchived` is Boolean. Every repeat event gets a separate log row. Keep V1 search logs, two tabs, translations, screenshots and document links. Ticket IDs are placeholders; dependencies are logical gates, not assignments.

## Phase gates
| Phase | Focus | Exit criteria |
|---|---|---|
| P0 Discovery | Environment, identity, inventory, behavior decisions | Open blockers resolved or explicitly tracked |
| P1 Foundation | .NET/React, data, auth, roles | Test/Prod isolation, persistence and access checks work |
| P2 Functional employee experience | Minimal React catalogue, details, links, events | End-to-end browsing and per-event search/download logging |
| P3 Functional Admin | Minimal React CRUD, preview, targeting, archive, Admin access, audit | Immediate saves, private preview and audit pass |
| P4 Verification/release | Security, UAT, operations, deployment | Production readiness signed off |

## Ticket register
| Phase | ID | Ticket | Priority | Dependencies | Minimum acceptance criteria |
|---|---|---|---|---|---|
| P0 | FND-01 | Confirm local .NET/React toolchain, DB, repo and deployment approach | P0 | — | Supported versions/runtime/database and workflow documented |
| P0 | FND-02 | Confirm Production app inventory, targeting, categories and link rules | P0 | — | Country/Global, type, platform, link override, archive and empty-selection semantics agreed |
| P0 | FND-03 | Confirm Okta claims, initial Admin provisioning, audit/privacy and search event definition | P0 | — | First Admin bootstrap, claim map, retention, search trigger and redirect failure policy recorded |
| P1 | ENV-01 | Set up .NET backend and React frontend with Test/Prod appsettings | P0 | FND-01 | Test fake login impossible in Prod; secrets outside source; clean project structure |
| P1 | DB-01 | Implement schema, relationships and migrations | P0 | FND-01, FND-02 | Multi-category/country/type/platform, Boolean archive, unique event IDs and isolation supported |
| P1 | DB-02 | Seed Test personas and reviewed Production inventory | P1 | DB-01, FND-02 | Test-only synthetic mappings; approved Prod data only |
| P1 | AUTH-01 | Implement Test fake login and role/persona switching | P0 | ENV-01, DB-01 | `Zeynep-Admin` and `Rümeysa-Employee` synthetic personas; switching refreshes permissions/context without restart |
| P1 | AUTH-02 | Integrate Production Okta and required user context | P0 | ENV-01, FND-03 | Token/claims validated; missing context produces explicit error |
| P1 | AUTH-03 | Implement Admin-only RBAC and bootstrap | P0 | DB-01, AUTH-02, FND-03 | Only Admin role exists; initial Admin established by approved method; server-side checks |
| P2 | CAT-01 | Implement eligibility engine and catalogue APIs | P0 | DB-01, AUTH-01, AUTH-02 | Country/Global, type, category and active/archive rules; direct URL guarded |
| P2 | CAT-02 | Build functional two-tab catalogue, search and category controls | P1 | CAT-01 | Arçelik-developed/Other tabs; search/filter and mobile cards work |
| P2 | CAT-03 | Build working application detail with essential mobile usability | P1 | CAT-01 | Version, images, documents and eligible links shown; basic navigation works; detailed styling deferred |
| P2 | LINK-01 | Resolve multiple links and safe redirects | P0 | CAT-01, DB-01 | All eligible links visible on all devices; link reauthorized; invalid URL blocked |
| P2 | LOG-01 | Record each download/open action separately | P0 | LINK-01, DB-01 | Repeated clicks create distinct ActionLog rows with actor, app, link, time and outcome |
| P2 | LOG-02 | Record V1 search actions separately | P0 | CAT-02, DB-01, FND-03 | Each agreed search action creates a separate record, including repeated searches |
| P3 | ADM-01 | Build Admin application creation/edit form and multi-select targeting | P0 | AUTH-03, DB-01 | New apps; multi-country/category dropdowns; platform/type selections; valid saved changes immediate |
| P3 | ADM-02 | Implement responsive private preview | P0 | ADM-01 | Unsaved changes visible in preview only; not public until saved; no approval flow |
| P3 | ADM-03 | Manage versions, links, screenshots, documents and translations | P1 | ADM-01 | All V1 content editable by Admin with validation and audit |
| P3 | ADM-04 | Implement active status and Boolean archiving | P0 | ADM-01, CAT-01, FND-02 | `isArchived` persisted; history retained; employee visibility follows confirmed rules |
| P3 | ADM-05 | Implement Admin assignment by another Admin | P0 | AUTH-03, FND-03 | Existing employee ID only; grant/revoke audited; no employee creation or Super Admin |
| P3 | LOG-03 | Implement append-only AdminAuditLog for every mutation | P0 | ADM-01, ADM-04, ADM-05 | Every create/edit/status/archive/assignment event creates distinct actor/time/diff row |
| P3 | ADM-06 | Implement usable mobile multi-select controls | P1 | ADM-01 | Searchable full-screen/sheet selection with checkboxes, Apply/Cancel, selected count |
| P4 | QA-01 | Unit/integration/security tests | P0 | CAT-01, LINK-01, ADM-05, LOG-03 | Eligibility, RBAC, redirects, archive, logs and Prod isolation pass |
| P4 | QA-02 | End-to-end UAT including all V1 screens and audit | P0 | CAT-03, LOG-01, LOG-02, ADM-02, ADM-03, ADM-06 | Preview/save, repeated logs, two tabs, translations, mobile links and Admin grants pass |
| P4 | OPS-01 | Monitoring, backup, rollback and support runbook | P1 | FND-01, DB-01 | Error tracing, restore and ownership documented |
| P4 | REL-01 | Review Production data and deploy | P0 | QA-01, QA-02, OPS-01, DB-02 | Okta, initial Admin, production mappings, secrets and smoke tests approved |

## Definition of Done
- Ticket references corresponding design/technical sections and documented decisions.
- Backend logic is C#/.NET with clear service boundaries; frontend is React.
- Server-side authorization and input validation tested.
- All applicable user and Admin events are **new separate durable records**; repeated actions never overwrite previous events.
- Preview does not mutate public state; save applies immediately without approval.
- `isArchived` Boolean retains historical records; no hard-delete dependency.
- Test persona switching is available only in Test; Prod Okta and real inventory remain isolated.
- Essential mobile usability and accessibility verified; pixel-perfect styling is not a release gate; tests and migration evidence attached.

## Critical path
`FND-01 → ENV-01/DB-01 → AUTH-01/AUTH-02 → CAT-01 → LINK-01 → LOG-01 → QA-02 → REL-01`  
Parallel Admin path: `FND-03 → AUTH-03 → ADM-01 → ADM-02/ADM-05/LOG-03 → QA-02`.

## Optional post-MVP ideas (not approved)
Additional service integration, app-store image retrieval, external-source data fetching, device-aware filtering, automated link health checks and improved audit browsing may be considered after MVP. V2 is not defined as Reports and V3 is not defined as Dashboards. No tickets are created for these ideas until approved.

## Collaboration without Norm
Rümeysa and ChatGPT will develop together. ChatGPT can generate/review code, tests and documentation and help troubleshoot. Local execution, internal access, credentials and deployment remain user-controlled. Build functionality before visual refinement.

## Out of scope
Super Admin, approval/publishing queue, employee master-data creation, binary hosting, install permissions, unapproved post-MVP enhancements, advanced analytics. The V1 **search event log itself is in scope**; its reporting UI is not.
