# Your complete Test package

This ZIP includes the application **and a PostgreSQL Test database service**. It also includes automatic table setup, sample users/apps, shared saving and activity records. It has been tested with two independent browser clients and an app restart.

## If you are uploading to Norm

Ask Norm this specific compatibility question:

> Does importing this ZIP deploy the services in compose.yaml (application + PostgreSQL + persistent volumes), or does it only build the Dockerfile?

- If Norm runs Compose services, use this complete package. The database is initialized automatically; there is no database password to type or share.
- If Norm only builds the Dockerfile, its platform must provide a PostgreSQL database separately. The files cannot override Norm permissions. DATABASE-SETUP.md gives the configuration names for the person who manages the platform.

Do not assume the database is connected just because deployment says Active. The catalogue banner must say **SHARED TEST DATABASE**. **TEST ENVIRONMENT** with a note about browser-only changes is the standalone preview, not shared saving.

## On a host with Docker Compose

A developer or hosting administrator can run this project with:

```sh
docker compose up --build -d
```

It serves port 8080. Credentials are generated privately into a Docker volume and reused. The database has its own persistent volume. Regular stop/restart preserves data. Do not remove the database/secrets volumes unless intentionally resetting this disposable Test environment.

You do not need to install anything on your PC to upload the ZIP or use the deployed website.

## What to try

1. Select a Test employee and the Test scenarios category.
2. Select Zeynep, open Administration, edit an application, preview, and save.
3. Open the site in another browser and select an eligible employee. The saved change is shared.
4. Review Activity as an Admin. Each repeated search/open/change has its own database record.

These are synthetic Test profiles. Production remains blocked. Existing browser-only edits are not automatically imported into a newly created database and are not erased from the browser.
