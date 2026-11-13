# Agreed scope and remaining setup

## Completed application work

- FND-02: HTTPS links may point directly to APK files. The catalogue stores links; the APK itself must be served by company storage or another download host. APK binary upload/hosting and installation permissions are outside this application.
- FND-03, in plain language: Norm identifies the signed-in employee; the app checks who is an Admin and records activity. Standard Sicil/internal user ID/optional email are supported. The first real Admin must be explicitly configured; later Admins can grant/revoke access. Existing history is retained; no automatic deletion schedule has been configured.
- DB-02: Both new Test and Production databases seed from the exact same catalogue-seed.json: six apps with identical definitions, versions and pending links. Production never receives Test personas or Test Admin assignments. Changes subsequently saved in a live Test database stay there; they cannot be copied from Norm by this workspace. A snapshot/import is required if those later edits must also seed Production. Existing Production data is not overwritten at startup.
- LOG-01: Each authenticated application/document-open attempt gets a distinct durable event, including blocked attempts. Records include actor, app/link IDs, timestamp, result, HTTP status and reason. The Activity screen shows Link opened or Blocked. A redirect means the app handed the browser a stored destination; it cannot confirm an external download completed or an APK was installed. External hosts' later failures are not observable here. No server-side fetching/probing of arbitrary APK hosts is performed.
- EN/TR app definitions, private preview, manual filters, RBAC and gateway save protection remain included.

## User-managed or explicitly deferred

- AUTH-02: User configures Okta access in Norm. Before Production, Norm must also supply trusted gateway source networks, a separate Production database connection and the initial Admin's verified employee ID. Live Production sign-in has not been verified here.
- ADM-06: Selection controls are implemented. Phone verification is removed from current acceptance per user instruction.
- QA-02: Full end-to-end UAT deferred per user instruction.
- OPS-01: Operational monitoring, backup/restore and support checks deferred per user instruction. The optional backup button remains excluded.
- REL-01: Live Production release follows the platform configuration; this package deliberately remains Test. Deferred work is not described as completed or verified.

No country/collar/manager targeting, employee creation, Super Admin or approval queue is added.
