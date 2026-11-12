-- Test schema v1. Application data uses JSONB arrays for multi-targeting fields.
-- Schema changes and all writes are transactional. Production is not initialized by this package.
CREATE SCHEMA IF NOT EXISTS appstore_test;
CREATE TABLE IF NOT EXISTS appstore_test.metadata (
 key text PRIMARY KEY, value text NOT NULL
);
CREATE TABLE IF NOT EXISTS appstore_test.catalogue_state (
 id integer PRIMARY KEY CHECK (id = 1), revision bigint NOT NULL DEFAULT 1,
 payload jsonb NOT NULL CHECK (jsonb_typeof(payload) = 'object'),
 updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS appstore_test.action_logs (
 id uuid PRIMARY KEY, occurred_at timestamptz NOT NULL DEFAULT now(),
 actor_id text NOT NULL, action text NOT NULL, entity text NOT NULL,
 payload jsonb NOT NULL
);
CREATE TABLE IF NOT EXISTS appstore_test.admin_audit_logs (
 id uuid PRIMARY KEY, occurred_at timestamptz NOT NULL DEFAULT now(),
 actor_id text NOT NULL, action text NOT NULL, entity text NOT NULL,
 payload jsonb NOT NULL
);
CREATE OR REPLACE FUNCTION appstore_test.reject_log_change() RETURNS trigger
LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Event records are append-only'; END $$;
DROP TRIGGER IF EXISTS action_logs_immutable ON appstore_test.action_logs;
CREATE TRIGGER action_logs_immutable BEFORE UPDATE OR DELETE OR TRUNCATE ON appstore_test.action_logs
FOR EACH STATEMENT EXECUTE FUNCTION appstore_test.reject_log_change();
DROP TRIGGER IF EXISTS admin_logs_immutable ON appstore_test.admin_audit_logs;
CREATE TRIGGER admin_logs_immutable BEFORE UPDATE OR DELETE OR TRUNCATE ON appstore_test.admin_audit_logs
FOR EACH STATEMENT EXECUTE FUNCTION appstore_test.reject_log_change();
