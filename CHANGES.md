# Admin and HTML update — 18 November 2026

- Favourite and edit buttons sit next to one another with separate click targets, including web cards and portals, in both themes. The Web/portals hero globe is circular and retains its proportions.
- Overview reads real PostgreSQL link-request counts, web/portal visits, top requested applications, daily activity and current catalogue readiness. Empty history displays zero. Accepted requests do not imply completed installations.
- Activity and change history query complete stored logs, with date, user, action, application/item and result filters, text search and 50-row paging. Admin changes record the affected sections. Blocked link attempts are logged separately and excluded from successful-request totals.
- Admins register additional languages by name/code, edit interface/application/web/category/page text, search and show only missing translations. English is the fallback. Added languages appear in the page selector and persist in PostgreSQL. Comments accept registered languages.
- Access management selects registered users, protects the signed-in Admin's access and supports searching. Test accounts and optional scenario attributes have a separate, clearer Test-only screen.
- One self-contained HTML supports offline browser-local review. Hosted by the updated .NET service, it uses the real API. No Sites update is required.

Existing Test/Production authentication separation, origin checks, database environment markers, favourites, uploaded APK/image/document storage and existing catalogue data are preserved. Schema changes are additive.
