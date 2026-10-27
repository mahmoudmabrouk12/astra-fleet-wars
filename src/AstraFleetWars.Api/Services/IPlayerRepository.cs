using System.Collections.Concurrent;
using AstraFleetWars.Api.Models.Entities;

namespace AstraFleetWars.Api.Services;

public interface IPlayerRepository
{
    Task<Profile?> GetProfileAsync(Guid playerId);
    Task<Profile?> GetProfileByUsernameAsync(string username);
    Task<Profile> CreateProfileAsync(Profile profile);

    Task<PlayerMetadata?> GetPlayerMetadataAsync(Guid playerId);
    Task<PlayerMetadata> SavePlayerMetadataAsync(PlayerMetadata metadata);

    Task<List<ChapterProgress>> GetChapterProgressAsync(Guid playerId);
    Task SaveChapterProgressAsync(Guid playerId, List<ChapterProgress> chapters);

    Task<List<PlayerAchievement>> GetAchievementsAsync(Guid playerId);
    Task SaveAchievementsAsync(Guid playerId, List<PlayerAchievement> achievements);

    Task<SyncAuditLog?> GetSyncAuditAsync(Guid playerId, string syncId);
    Task SaveSyncAuditAsync(SyncAuditLog audit);

    Task SavePassiveLogAsync(PassiveEventLog log);
    Task<List<PassiveEventLog>> GetPassiveLogsAsync(int limit = 50);
}

public class InMemoryPlayerRepository : IPlayerRepository
{
    private readonly ConcurrentDictionary<Guid, Profile> _profiles = new();
    private readonly ConcurrentDictionary<Guid, PlayerMetadata> _metadata = new();
    private readonly ConcurrentDictionary<Guid, List<ChapterProgress>> _chapters = new();
    private readonly ConcurrentDictionary<Guid, List<PlayerAchievement>> _achievements = new();
    private readonly ConcurrentDictionary<Guid, List<SyncAuditLog>> _syncLogs = new();
    private readonly ConcurrentBag<PassiveEventLog> _passiveLogs = new();

    public InMemoryPlayerRepository()
    {
        SeedDemoData();
    }

    private void SeedDemoData()
    {
        var demoId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        _profiles[demoId] = new Profile
        {
            Id = demoId,
            Username = "commander_reyes",
            DisplayName = "Commander Marcus Reyes",
            AvatarUrl = "https://api.dicebear.com/7.x/bottts/svg?seed=Reyes",
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            UpdatedAt = DateTime.UtcNow
        };

        _metadata[demoId] = new PlayerMetadata
        {
            PlayerId = demoId,
            Level = 5,
            Experience = 4200,
            Credits = 15400,
            DarkMatter = 120,
            HighestUnlockedChapter = 3,
            FleetPower = 1850,
            ActiveFleetLoadout = new FleetLoadout
            {
                Flagship = "Vanguard-II Dreadnought",
                Escorts = new List<string> { "Corvette-Alpha", "Frigate-Beta" },
                Modules = new List<string> { "plasma_cannon_v1", "shields_mk2" }
            },
            RevisionVersion = 10,
            LastSyncTimestamp = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _chapters[demoId] = new List<ChapterProgress>
        {
            new() { PlayerId = demoId, ChapterNumber = 1, Completed = true, Stars = 3, HighScore = 12500, CompletedAt = DateTime.UtcNow.AddDays(-3) },
            new() { PlayerId = demoId, ChapterNumber = 2, Completed = true, Stars = 2, HighScore = 9800, CompletedAt = DateTime.UtcNow.AddDays(-1) },
            new() { PlayerId = demoId, ChapterNumber = 3, Completed = false, Stars = 0, HighScore = 0 }
        };

        _achievements[demoId] = new List<PlayerAchievement>
        {
            new() { PlayerId = demoId, AchievementId = "ACH_FIRST_FLIGHT", UnlockedAt = DateTime.UtcNow.AddDays(-3), Metadata = new() { { "chapter", 1 } } },
            new() { PlayerId = demoId, AchievementId = "ACH_FLEET_EXPANSION", UnlockedAt = DateTime.UtcNow.AddDays(-1), Metadata = new() { { "ships_count", 3 } } }
        };
    }

    public Task<Profile?> GetProfileAsync(Guid playerId) =>
        Task.FromResult(_profiles.TryGetValue(playerId, out var profile) ? profile : null);

    public Task<Profile?> GetProfileByUsernameAsync(string username) =>
        Task.FromResult(_profiles.Values.FirstOrDefault(p => p.Username.Equals(username, StringComparison.OrdinalIgnoreCase)));

    public Task<Profile> CreateProfileAsync(Profile profile)
    {
        _profiles[profile.Id] = profile;
        return Task.FromResult(profile);
    }

    public Task<PlayerMetadata?> GetPlayerMetadataAsync(Guid playerId) =>
        Task.FromResult(_metadata.TryGetValue(playerId, out var meta) ? meta : null);

    public Task<PlayerMetadata> SavePlayerMetadataAsync(PlayerMetadata metadata)
    {
        metadata.UpdatedAt = DateTime.UtcNow;
        _metadata[metadata.PlayerId] = metadata;
        return Task.FromResult(metadata);
    }

    public Task<List<ChapterProgress>> GetChapterProgressAsync(Guid playerId) =>
        Task.FromResult(_chapters.TryGetValue(playerId, out var chapters) ? chapters.ToList() : new List<ChapterProgress>());

    public Task SaveChapterProgressAsync(Guid playerId, List<ChapterProgress> chapters)
    {
        _chapters[playerId] = chapters.ToList();
        return Task.CompletedTask;
    }

    public Task<List<PlayerAchievement>> GetAchievementsAsync(Guid playerId) =>
        Task.FromResult(_achievements.TryGetValue(playerId, out var ach) ? ach.ToList() : new List<PlayerAchievement>());

    public Task SaveAchievementsAsync(Guid playerId, List<PlayerAchievement> achievements)
    {
        _achievements[playerId] = achievements.ToList();
        return Task.CompletedTask;
    }

    public Task<SyncAuditLog?> GetSyncAuditAsync(Guid playerId, string syncId)
    {
        if (_syncLogs.TryGetValue(playerId, out var list))
        {
            return Task.FromResult(list.FirstOrDefault(l => l.SyncId == syncId));
        }
        return Task.FromResult<SyncAuditLog?>(null);
    }

    public Task SaveSyncAuditAsync(SyncAuditLog audit)
    {
        _syncLogs.AddOrUpdate(
            audit.PlayerId,
            new List<SyncAuditLog> { audit },
            (_, list) =>
            {
                lock (list) { list.Add(audit); }
                return list;
            });
        return Task.CompletedTask;
    }

    public Task SavePassiveLogAsync(PassiveEventLog log)
    {
        _passiveLogs.Add(log);
        return Task.CompletedTask;
    }

    public Task<List<PassiveEventLog>> GetPassiveLogsAsync(int limit = 50) =>
        Task.FromResult(_passiveLogs.Take(limit).ToList());
}
