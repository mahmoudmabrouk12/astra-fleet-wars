# Database Setup & Migrations (BE-01)
### Project: Astra Syndicate: Fleet Wars

This directory contains the PostgreSQL database migrations and seed data designed for the **Supabase Free-Tier** (or any standard PostgreSQL 14+ instance).

---

## Migration Files

1. **`001_initial_schema.sql`**
   - Creates the core schema:
     - `profiles`: Player identity and account records.
     - `player_metadata`: Level, credits, dark matter, fleet power, active fleet loadout, revision version.
     - `chapter_progress`: Tracks progression, star ratings (0-3), and high scores per chapter.
     - `player_achievements`: Set-based achievement tracking with metadata.
     - `sync_audit_logs`: Audit logs for offline-to-online sync operations with conflict tracking and idempotency.
     - `passive_event_logs`: Lightweight passive telemetry logging.

2. **`002_rls_policies.sql`**
   - Configures **Row Level Security (RLS)**:
     - Ensures players can only read and write their own progression, metadata, and achievements.
     - Public profiles allow read-only access for social features (leaderboards/guilds).
     - Passive event logging allows write-only access for client telemetry without exposing historical logs.

3. **`003_seed_data.sql`**
   - Populates initial test player (`commander_reyes`) with sample chapters, fleet loadouts, and achievements for local development and QA testing.

---

## How to Apply on Supabase (Free-Tier)

1. Log into your free [Supabase Dashboard](https://supabase.com/dashboard).
2. Create a new free project: `astra-fleet-wars`.
3. Open the **SQL Editor** tab from the left sidebar.
4. Copy and paste the contents of `001_initial_schema.sql` and click **Run**.
5. Copy and paste the contents of `002_rls_policies.sql` and click **Run**.
6. (Optional) Run `003_seed_data.sql` to populate initial test data.
7. Under **Project Settings -> API**, copy your:
   - `Project URL` -> `SUPABASE_URL`
   - `anon public` key -> `SUPABASE_ANON_KEY`
   - `service_role` key -> `SUPABASE_SERVICE_ROLE_KEY`
8. Paste them into your `.env` file.
