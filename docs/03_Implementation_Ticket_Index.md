# Arçelik App Store — Implementation Ticket Index
Version 3.4 · 18 November 2026

| Ticket | Scope | State |
|---|---|---|
| UI-32 | Approved review 32, compact cards/details/editor, light/dark, responsive layout | Implemented |
| NAV-02 | Hero destinations, catalogue scroll, web return link, browser history, no repeated header tabs | Implemented |
| MOBILE-02 | Hamburger-only header; profile/preferences inside; no mobile admin controls | Implemented |
| VIEW-02 | Cards + two-app comparison; remove List and migrate stored preference | Implemented |
| FAV-02 | Yellow outline/filled star, compact stack, side remove X, red profile avatar | Implemented |
| EDIT-03 | Three editor groups, compact closed multi-selects, auto-growing 150-char description, remove redundant questions | Implemented |
| FILE-01 | Multi-select images/documents, PostgreSQL bytes, authenticated upload/read and card cover selection | Implemented; 10 MB/file |
| APK-01 | Browser APK upload/download and validation | Implemented; 100 MB |
| FEEDBACK-02 | Send suggestion/complaint/application need; PostgreSQL admin inbox | Implemented; no external email/ticket delivery |
| AUDIENCE-01 | Company/country/mail-group metadata and server-side filtering/download enforcement | Implemented; Norm claim mapping pending |
| AUTH-02 | Remove trusted-gateway CIDR code/settings/startup gate as requested | Implemented |
| AUTH-03 | Norm Okta authentication, forwarded headers and bypass prevention, real initial-admin sicil | Norm configuration / real-environment verification pending |
| ENV-01 | One project; appsettings Test/Production; correct database selector/marker | Implemented; actual platform secrets/databases pending |
| DATA-02 | Additive schema, preserve existing catalogue/comments/preferences | Implemented; deployment backup remains operational task |
| METRIC-01 | Real usage/installation analytics | Not implemented; dashboard sample metrics labelled |
| OPS-02 | Orphan upload retention/cleanup | Follow-up |
| RELEASE-02 | Focused local Node/.NET/PostgreSQL/API checks | Passed; Norm deployment/Okta validation pending |

Release is a Test-selected single-project package. No direct Okta OAuth client or credentials are bundled. Norm forwards X-User-Sicil and GUID X-User-Id, plus optional email/name/company/country/groups/site. No CIDR allowlist is required by the application. Identity header overwrite and gateway-only reachability are platform responsibilities. Initial administrator comes from Authentication__InitialAdminEmployeeIds__0.

Acceptance on Norm: configure the correct environment/database/origin; attach Okta with the documented header map; sign in as a real employee and the assigned administrator; save an application; test company/group targeting; upload a document/APK; send feedback and read it in the desktop admin inbox; verify mobile shows only employee tools. Production requires its separate database configuration and the same verified gateway contract.
