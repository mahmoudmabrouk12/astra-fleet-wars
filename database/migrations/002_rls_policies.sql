-- ====================================================================
-- Astra Syndicate: Fleet Wars - Row Level Security (RLS) Policies
-- Deliverable: BE-01 (Supabase PostgreSQL Security Architecture)
-- ====================================================================

-- 1. Enable RLS on all player-owned tables
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.player_metadata ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.chapter_progress ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.player_achievements ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.sync_audit_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.passive_event_logs ENABLE ROW LEVEL SECURITY;

-- 2. Profiles Policies
-- Anyone can view public profile info (for leaderboards/guilds)
CREATE POLICY "Public profiles are viewable by everyone" 
ON public.profiles FOR SELECT 
USING (true);

-- Users can update only their own profile
CREATE POLICY "Users can update their own profile" 
ON public.profiles FOR UPDATE 
USING (auth.uid() = id);

-- 3. Player Metadata Policies
CREATE POLICY "Users can view their own player metadata" 
ON public.player_metadata FOR SELECT 
USING (auth.uid() = player_id);

CREATE POLICY "Users can update their own player metadata" 
ON public.player_metadata FOR UPDATE 
USING (auth.uid() = player_id);

CREATE POLICY "Users can insert their own player metadata" 
ON public.player_metadata FOR INSERT 
WITH CHECK (auth.uid() = player_id);

-- 4. Chapter Progress Policies
CREATE POLICY "Users can read own chapter progress" 
ON public.chapter_progress FOR SELECT 
USING (auth.uid() = player_id);

CREATE POLICY "Users can insert/update own chapter progress" 
ON public.chapter_progress FOR ALL 
USING (auth.uid() = player_id) 
WITH CHECK (auth.uid() = player_id);

-- 5. Achievements Policies
CREATE POLICY "Users can read own achievements" 
ON public.player_achievements FOR SELECT 
USING (auth.uid() = player_id);

CREATE POLICY "Users can insert own achievements" 
ON public.player_achievements FOR INSERT 
WITH CHECK (auth.uid() = player_id);

-- 6. Sync Audit Logs Policies
CREATE POLICY "Users can read own sync logs" 
ON public.sync_audit_logs FOR SELECT 
USING (auth.uid() = player_id);

CREATE POLICY "Users can insert own sync logs" 
ON public.sync_audit_logs FOR INSERT 
WITH CHECK (auth.uid() = player_id);

-- 7. Passive Event Logs (Telemetry)
-- Anyone authenticated can insert telemetry; read restricted to service role
CREATE POLICY "Authenticated users can insert passive event logs" 
ON public.passive_event_logs FOR INSERT 
WITH CHECK (true);
