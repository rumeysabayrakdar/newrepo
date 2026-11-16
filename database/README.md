# Database setup

PostgreSQL is mandatory. Startup applies `src/AppStore.Web/Database/schema.sql` and initializes the versioned experience once, preserving the original catalogue. `schema.sql` here is the same setup SQL for review, not a live DB backup. Seed files are TEST starter data, not an instruction to overwrite existing rows. The `experience_state`, `user_preferences` and `feedback` tables supplement the original tables. Never share a DB between TEST and PROD.
