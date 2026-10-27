-- ====================================================================
-- Astra Syndicate: Fleet Wars - Database Schema (Sprint 1)
-- Deliverable: BE-01 (Supabase / PostgreSQL Initial Schema)
-- ====================================================================

-- 1. Enable UUID extension if not already enabled
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- 2. Profiles Table (Linked to Auth or standalone player accounts)
CREATE TABLE IF NOT EXISTS public.profiles (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username VARCHAR(50) UNIQUE NOT NULL,
    display_name VARCHAR(100),
    avatar_url TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Index for fast username lookup
CREATE INDEX IF NOT EXISTS idx_profiles_username ON public.profiles(username);

-- 3. Player Metadata Table (Currencies, Fleet stats, Campaign status)
CREATE TABLE IF NOT EXISTS public.player_metadata (
    player_id UUID PRIMARY KEY REFERENCES public.profiles(id) ON DELETE CASCADE,
    level INT NOT NULL DEFAULT 1,
    experience BIGINT NOT NULL DEFAULT 0,
    credits BIGINT NOT NULL DEFAULT 1000,
    dark_matter INT NOT NULL DEFAULT 50,
    highest_unlocked_chapter INT NOT NULL DEFAULT 1,
    fleet_power INT NOT NULL DEFAULT 100,
    active_fleet_loadout JSONB NOT NULL DEFAULT '{"flagship": "Vanguard-I", "escorts": []}'::jsonb,
    revision_version BIGINT NOT NULL DEFAULT 1,
    last_sync_timestamp TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 4. Chapter Progress Table (Tracks offline and online chapter completions)
CREATE TABLE IF NOT EXISTS public.chapter_progress (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    player_id UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    chapter_number INT NOT NULL,
    completed BOOLEAN NOT NULL DEFAULT FALSE,
    stars INT NOT NULL DEFAULT 0 CHECK (stars >= 0 AND stars <= 3),
    high_score BIGINT NOT NULL DEFAULT 0,
    completed_at TIMESTAMPTZ,
    synced_at TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT uq_player_chapter UNIQUE (player_id, chapter_number)
);

CREATE INDEX IF NOT EXISTS idx_chapter_progress_player ON public.chapter_progress(player_id);

-- 5. Player Achievements Table (Monotonically additive set)
CREATE TABLE IF NOT EXISTS public.player_achievements (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    player_id UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    achievement_id VARCHAR(100) NOT NULL,
    unlocked_at TIMESTAMPTZ NOT NULL,
    metadata JSONB DEFAULT '{}'::jsonb,
    CONSTRAINT uq_player_achievement UNIQUE (player_id, achievement_id)
);

CREATE INDEX IF NOT EXISTS idx_achievements_player ON public.player_achievements(player_id);

-- 6. Sync Audit Logs (For idempotency, conflict tracking, and offline sync auditing)
CREATE TABLE IF NOT EXISTS public.sync_audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    player_id UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    sync_id VARCHAR(128) NOT NULL,
    client_version VARCHAR(30),
    device_info JSONB DEFAULT '{}'::jsonb,
    client_timestamp TIMESTAMPTZ NOT NULL,
    server_timestamp TIMESTAMPTZ DEFAULT NOW(),
    status VARCHAR(50) NOT NULL, -- 'SUCCESS', 'CONFLICT_RESOLVED', 'REJECTED'
    conflicts_detected JSONB DEFAULT '[]'::jsonb,
    applied_changes JSONB DEFAULT '{}'::jsonb,
    CONSTRAINT uq_player_sync_id UNIQUE (player_id, sync_id)
);

CREATE INDEX IF NOT EXISTS idx_sync_audit_player ON public.sync_audit_logs(player_id, server_timestamp DESC);

-- 7. Passive Event Logs (Zero-cost asynchronous telemetry logging)
CREATE TABLE IF NOT EXISTS public.passive_event_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    player_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
    event_type VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_passive_logs_event_type ON public.passive_event_logs(event_type, created_at DESC);
