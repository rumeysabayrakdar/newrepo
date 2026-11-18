# Database Setup

PostgreSQL is required. The application applies the [schema](../src/AppStore.Web/Database/schema.sql) at startup. The canonical [catalogue seed](../src/AppStore.Web/catalogue-seed.json) and [experience seed](../src/AppStore.Web/experience-seed.json) live beside the backend project that loads them.

Startup initializes missing data and preserves the existing catalogue. Use separate databases for Test and Production.
