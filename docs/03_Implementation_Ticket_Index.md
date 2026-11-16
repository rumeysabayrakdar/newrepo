# Arçelik App Store — Implementation Ticket Index
Version 3.0 · 16 November 2026 · Local proposed register, no external tickets created

| ID | Work | TEST candidate status | Remaining acceptance |
|---|---|---|---|
| UI-01 | Approved EN/TR responsive catalogue, themes, multi-filters, selected details | Implemented | User acceptance in Norm/mobile |
| UI-02 | Compact featured cards, light-mode rings, burgundy animated perimeter/corners | Implemented | Visual acceptance |
| UI-03 | Internal/external resources, topic expansion, portal photo cards | Implemented | Replace starter links/assets |
| UI-04 | Admin ordering, desktop columns, internal card sizes | Implemented | User acceptance |
| DATA-01 | PostgreSQL-backed experience, versioned save, migration preserving legacy rows | Implemented | Back up deployed DB before rollout |
| DATA-02 | Per-employee favourites and likes | Implemented | Corporate profile acceptance |
| DATA-03 | Shared app comments; web comments disabled | Implemented | UAT / retention policy |
| ADMIN-01 | Existing Admin authority, registered-employee grants, audit | Retained/integrated | Corporate role bootstrap |
| TEST-01 | Synthetic TEST profiles, explicit company/country/site, effective-role checks | Implemented in TEST | Real claim values are not looked up |
| ID-01 | Trusted Okta gateway and real site/company/country mapping | Existing gate retained; mapping pending | Identity/platform owners confirm claims and trust ranges |
| FB-01 | Help/idea/complaint receipt in DB and Admin Activity | Implemented | Notification destination/retention |
| DASH-01 | Dashboard visual sample and actual link-readiness ring | Implemented visual sample | Live download/web metrics remain pending |
| ENV-01 | TEST package, DB alias config, schema initialization, port 8080 | Implemented | Norm ingress/TLS and public-origin settings |
| DOC-01 | General Design / Technical Implementation / Tickets | Updated v3 | User review |
| CLEAN-01 | Consolidate accumulated CSS overrides without changing UI | Pending | Separate approved cleanup |
| RELEASE-01 | Production package and release sign-off | Not included | TEST acceptance, real identity/configuration, operational checks |

Guardrails: no Node backend migration, no EF/TypeORM migration, no employee creation, no Super Admin, no approval workflow. A download/open request does not establish successful installation. Dashboard samples must not be presented as measured counts. Original auth, tables and source remain in the package. This register is an implementation record, not a claim that deployed Norm infrastructure or corporate integrations were changed.
