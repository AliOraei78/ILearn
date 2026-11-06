\set ON_ERROR_STOP on
\getenv app_password ILEARN_APP_DB_PASSWORD
BEGIN;
SELECT format('CREATE ROLE ilearn_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='ilearn_app')
\gexec
SELECT format('ALTER ROLE ilearn_app WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS PASSWORD %L', :'app_password')
\gexec
SELECT format('GRANT CONNECT ON DATABASE %I TO ilearn_app', current_database())
\gexec
GRANT USAGE, CREATE ON SCHEMA public TO ilearn_app;
-- Transfer only ILearn tables, preserving existing profiles and progress on older local volumes.
SELECT format('ALTER TABLE public.%I OWNER TO ilearn_app', tablename)
FROM pg_tables WHERE schemaname='public' AND tablename IN
 ('profiles','progress','pending_signups','email_deliveries','password_resets','email_challenges','player_sessions','password_grants','login_failures')
\gexec
COMMIT;
