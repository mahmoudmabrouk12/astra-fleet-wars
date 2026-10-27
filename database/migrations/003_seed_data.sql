-- ====================================================================
-- Astra Syndicate: Fleet Wars - Seed Data for Testing & Development
-- Deliverable: BE-01 (Database Schema & Local Development Setup)
-- ====================================================================

-- 1. Insert a demo commander profile
INSERT INTO public.profiles (id, username, display_name, avatar_url)
VALUES (
    'a0000000-0000-0000-0000-000000000001',
    'commander_reyes',
    'Commander Marcus Reyes',
    'https://api.dicebear.com/7.x/bottts/svg?seed=Reyes'
) ON CONFLICT (id) DO NOTHING;

-- 2. Insert metadata for demo commander
INSERT INTO public.player_metadata (
    player_id,
    level,
    experience,
    credits,
    dark_matter,
    highest_unlocked_chapter,
    fleet_power,
    active_fleet_loadout,
    revision_version
) VALUES (
    'a0000000-0000-0000-0000-000000000001',
    5,
    4200,
    15400,
    120,
    3,
    1850,
    '{
        "flagship": "Vanguard-II Dreadnought",
        "escorts": ["Corvette-Alpha", "Frigate-Beta"],
        "modules": ["plasma_cannon_v1", "shields_mk2"]
    }'::jsonb,
    10
) ON CONFLICT (player_id) DO NOTHING;

-- 3. Insert baseline chapter progress
INSERT INTO public.chapter_progress (player_id, chapter_number, completed, stars, high_score, completed_at)
VALUES 
('a0000000-0000-0000-0000-000000000001', 1, TRUE, 3, 12500, NOW() - INTERVAL '3 days'),
('a0000000-0000-0000-0000-000000000001', 2, TRUE, 2, 9800, NOW() - INTERVAL '1 day'),
('a0000000-0000-0000-0000-000000000001', 3, FALSE, 0, 0, NULL)
ON CONFLICT (player_id, chapter_number) DO NOTHING;

-- 4. Insert baseline achievements
INSERT INTO public.player_achievements (player_id, achievement_id, unlocked_at, metadata)
VALUES 
('a0000000-0000-0000-0000-000000000001', 'ACH_FIRST_FLIGHT', NOW() - INTERVAL '3 days', '{"chapter": 1}'::jsonb),
('a0000000-0000-0000-0000-000000000001', 'ACH_FLEET_EXPANSION', NOW() - INTERVAL '1 day', '{"ships_count": 3}'::jsonb)
ON CONFLICT (player_id, achievement_id) DO NOTHING;
