# What to do next

The project is now one real .NET / React / PostgreSQL application. All application changes are saved to PostgreSQL and shared across users. Search, category and platform filters replace automatic employee-information filtering.

## Your Test version

1. Import the latest main branch from GitHub, or upload the latest complete ZIP.
2. Keep `AppStore:Environment` as `Test` in appsettings.json.
3. Start the app with its PostgreSQL service, or use the managed PostgreSQL connection Norm supplies.
4. Open the site. Choose Rümeysa to browse and Zeynep to manage applications.
5. Save an app change, then check it from another browser. Refreshing or restarting the app does not erase the change.

If Norm runs the included Compose services, the database and its private password are set up automatically. If it only runs the Dockerfile, its platform must provide the database and secure connection. Uploading code cannot override Norm infrastructure permissions or fix an empty repository-checkout path in the importer.

## When you are ready for Production

A hosting administrator supplies the Production database connection, trusted Norm gateway network and the first Admin's real employee ID. Then the appsettings environment selector is changed to Production. Test and Production use separate data and credentials.

You do not need country, white-/blue-collar or manager claims. Okta is used only for sign-in and identifying the user. No extra application login page is shown.

Do not paste passwords into chat or GitHub. DEPLOYMENT.md contains the operator settings; you do not need to understand them to test the application.
