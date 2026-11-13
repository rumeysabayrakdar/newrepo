# Upload or deploy

The maintained source is https://github.com/rumeysabayrakdar/newrepo on main. The complete ZIP contains the same project files without Git history, generated database contents, node_modules or credentials.

For GitHub browser upload, unzip and upload the contents of the arcelik-app-store folder into the repository root. Dockerfile, compose.yaml, app.jsx, package.json, dist and src must remain at the root/paths supplied. For Norm ZIP import, upload the ZIP itself according to its importer instructions.

Norm must either launch the included Compose services or provide a PostgreSQL connection. A blank checkout/source-path import failure happens before application startup and cannot be repaired by application code; the repository root build context is a dot (.). Ask platform support to fix that importer path if it persists.

Production sign-in uses Norm's existing Okta/auth-gate. It is not established by uploading files alone. Follow START-HERE and DEPLOYMENT for the Test/Production settings.
