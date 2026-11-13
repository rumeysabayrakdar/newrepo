# Download an application backup

In the deployed application, choose an Admin account, open Administration → Backup, then Download backup. The browser saves a dated JSON file. Keep it private because it includes registered user information and history. No terminal, PostgreSQL client or Norm backup job is required to download it.

## Contents

Versioned format arcelik-appstore-backup, export timestamp, environment, schema version, per-table counts and all rows from appstore.metadata, catalogue_state, users, action_logs and admin_audit_logs. This includes archived applications, roles, translations, reference data and complete audit/event history, not only the 200 rows shown in Activity.

The backend checks the current user's Admin role inside a read-only, repeatable-read PostgreSQL transaction. Export does not modify the catalogue or recreate the database. The POST endpoint /api/admin/backup uses the same identity and origin protections as other Admin requests. Failed downloads produce no success message.

This is application-data recovery material, not a pg_dump or full disaster-recovery backup. It does not include passwords, database roles/grants, hosting settings, other schemas, WAL, or the contents of files referenced by asset/download links. Keep the corresponding project source and database schema too. The feature does not repair Norm's platform backup job.

## Recovery

There is deliberately no overwrite/restore button for a live database. A hosting/database administrator restores into a new isolated database using the matching schema.sql, validates the format/version, environment and counts, then imports all five table arrays with their original typed columns and IDs. PostgreSQL jsonb_populate_recordset can perform these inserts. Preserve the environment marker and original event IDs. Test read/write behavior and correct environment before any controlled replacement of a live database. Never mix Test data into Production.

The automated tests/backup.cjs proves a complete export/restore round trip into a newly created disposable PostgreSQL database, compares every restored row to the export and drops only that disposable database. It also verifies that employees, missing identities and hostile origins cannot export, and that more than 200 activity entries are included. Run it only against the documented isolated development stack, never production. It is validation tooling, not a live restore command.

The current feature is manual export. Schedule regular downloads and keep independent copies; production operations should still arrange monitored full PostgreSQL backups.
