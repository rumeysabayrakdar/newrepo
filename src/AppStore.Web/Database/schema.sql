-- Shared application schema v2. Test and Production require separate databases.
CREATE SCHEMA IF NOT EXISTS appstore;
CREATE TABLE IF NOT EXISTS appstore.metadata (
 key text PRIMARY KEY, value text NOT NULL
);
CREATE TABLE IF NOT EXISTS appstore.catalogue_state (
 id integer PRIMARY KEY CHECK (id = 1), revision bigint NOT NULL DEFAULT 1,
 payload jsonb NOT NULL CHECK (jsonb_typeof(payload) = 'object'),
 updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS appstore.action_logs (
 id uuid PRIMARY KEY, occurred_at timestamptz NOT NULL DEFAULT now(),
 actor_id text NOT NULL, action text NOT NULL, entity text NOT NULL,
 payload jsonb NOT NULL
);
CREATE TABLE IF NOT EXISTS appstore.admin_audit_logs (
 id uuid PRIMARY KEY, occurred_at timestamptz NOT NULL DEFAULT now(),
 actor_id text NOT NULL, action text NOT NULL, entity text NOT NULL,
 payload jsonb NOT NULL
);
CREATE OR REPLACE FUNCTION appstore.reject_log_change() RETURNS trigger
LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Event records are append-only'; END $$;
DROP TRIGGER IF EXISTS action_logs_immutable ON appstore.action_logs;
CREATE TRIGGER action_logs_immutable BEFORE UPDATE OR DELETE OR TRUNCATE ON appstore.action_logs
FOR EACH STATEMENT EXECUTE FUNCTION appstore.reject_log_change();
DROP TRIGGER IF EXISTS admin_logs_immutable ON appstore.admin_audit_logs;
CREATE TRIGGER admin_logs_immutable BEFORE UPDATE OR DELETE OR TRUNCATE ON appstore.admin_audit_logs
FOR EACH STATEMENT EXECUTE FUNCTION appstore.reject_log_change();

CREATE TABLE IF NOT EXISTS appstore.users(id text PRIMARY KEY,name text NOT NULL,internal_user_id text,email text);

-- Approved experience model. Additive migration; legacy catalogue is retained.
CREATE TABLE IF NOT EXISTS appstore.experience_state (
 id integer PRIMARY KEY CHECK(id=1), revision bigint NOT NULL DEFAULT 1,
 payload jsonb NOT NULL, updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS appstore.user_preferences (
 user_id text PRIMARY KEY, payload jsonb NOT NULL DEFAULT '{"favorites":[],"likes":[]}'::jsonb,
 updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS appstore.feedback (
 id uuid PRIMARY KEY, user_id text NOT NULL, topic text NOT NULL,
 message text NOT NULL, created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS appstore.apk_packages (
 id uuid PRIMARY KEY, file_name text NOT NULL, content bytea NOT NULL,
 uploaded_by text NOT NULL, created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS appstore.experience_files (
 id uuid PRIMARY KEY,file_name text NOT NULL,content_type text NOT NULL,content bytea NOT NULL,
 uploaded_by text NOT NULL,created_at timestamptz NOT NULL DEFAULT now()
);

-- Additive indexes for date/user/action log queries.
CREATE INDEX IF NOT EXISTS action_logs_time_id ON appstore.action_logs(occurred_at DESC,id DESC);
CREATE INDEX IF NOT EXISTS admin_logs_time_id ON appstore.admin_audit_logs(occurred_at DESC,id DESC);
CREATE INDEX IF NOT EXISTS action_logs_actor_time ON appstore.action_logs(actor_id,occurred_at DESC);
CREATE INDEX IF NOT EXISTS admin_logs_actor_time ON appstore.admin_audit_logs(actor_id,occurred_at DESC);
CREATE INDEX IF NOT EXISTS action_logs_action_time ON appstore.action_logs(action,occurred_at DESC);
