using AstraFleetWars.Api.Models.DTOs;
using AstraFleetWars.Api.Models.Entities;

namespace AstraFleetWars.Api.Services;

public interface ISyncService
{
    Task<SyncResponseDataDto> ExecuteSyncAsync(Guid playerId, SyncRequestDto payload);
}

public class SyncService : ISyncService
{
    private readonly IPlayerRepository _playerRepository;

    public SyncService(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    public async Task<SyncResponseDataDto> ExecuteSyncAsync(Guid playerId, SyncRequestDto payload)
    {
        string syncIdStr = payload.SyncId.ToString();

        // 1. Idempotency Check
        var existingAudit = await _playerRepository.GetSyncAuditAsync(playerId, syncIdStr);
        if (existingAudit != null)
        {
            var meta = await _playerRepository.GetPlayerMetadataAsync(playerId) ?? new PlayerMetadata { PlayerId = playerId };
            var chs = await _playerRepository.GetChapterProgressAsync(playerId);
            var achs = await _playerRepository.GetAchievementsAsync(playerId);

            return new SyncResponseDataDto
            {
                SyncId = payload.SyncId,
                Status = "ALREADY_SYNCED",
                ServerTimestamp = existingAudit.ServerTimestamp,
                HasConflicts = existingAudit.ConflictsDetected.Count > 0,
                Conflicts = existingAudit.ConflictsDetected,
                AuthoritativeState = new AuthoritativeStateDto
                {
                    Metadata = meta,
                    Chapters = chs,
                    Achievements = achs
                }
            };
        }

        // 2. Fetch Authoritative Server State
        var currentMetadata = await _playerRepository.GetPlayerMetadataAsync(playerId) ?? new PlayerMetadata
        {
            PlayerId = playerId,
            Level = 1,
            Experience = 0,
            Credits = 1000,
            DarkMatter = 50,
            HighestUnlockedChapter = 1,
            FleetPower = 100,
            ActiveFleetLoadout = new FleetLoadout { Flagship = "Vanguard-I" },
            RevisionVersion = 1,
            LastSyncTimestamp = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var currentChapters = await _playerRepository.GetChapterProgressAsync(playerId);
        var currentAchievements = await _playerRepository.GetAchievementsAsync(playerId);

        // 3. Run Conflict Resolution Engine
        var resolution = ConflictResolver.Resolve(
            currentMetadata,
            currentChapters,
            currentAchievements,
            payload);

        // 4. Persist Resolved State
        await _playerRepository.SavePlayerMetadataAsync(resolution.ResolvedMetadata);
        await _playerRepository.SaveChapterProgressAsync(playerId, resolution.ResolvedChapters);
        await _playerRepository.SaveAchievementsAsync(playerId, resolution.ResolvedAchievements);

        // 5. Audit Log for Traceability & Idempotency
        var serverTimestamp = DateTime.UtcNow;
        var audit = new SyncAuditLog
        {
            PlayerId = playerId,
            SyncId = syncIdStr,
            ClientVersion = payload.ClientVersion,
            DeviceInfo = payload.DeviceInfo,
            ClientTimestamp = payload.ClientTimestamp,
            ServerTimestamp = serverTimestamp,
            Status = resolution.HasConflicts ? "CONFLICT_RESOLVED" : "SUCCESS",
            ConflictsDetected = resolution.Conflicts,
            AppliedChanges = new Dictionary<string, object>
            {
                { "level", resolution.ResolvedMetadata.Level },
                { "credits", resolution.ResolvedMetadata.Credits },
                { "chapters_count", resolution.ResolvedChapters.Count },
                { "achievements_count", resolution.ResolvedAchievements.Count }
            }
        };

        await _playerRepository.SaveSyncAuditAsync(audit);

        return new SyncResponseDataDto
        {
            SyncId = payload.SyncId,
            Status = resolution.HasConflicts ? "CONFLICT_RESOLVED" : "SUCCESS",
            ServerTimestamp = serverTimestamp,
            HasConflicts = resolution.HasConflicts,
            Conflicts = resolution.Conflicts,
            AuthoritativeState = new AuthoritativeStateDto
            {
                Metadata = resolution.ResolvedMetadata,
                Chapters = resolution.ResolvedChapters,
                Achievements = resolution.ResolvedAchievements
            }
        };
    }
}
