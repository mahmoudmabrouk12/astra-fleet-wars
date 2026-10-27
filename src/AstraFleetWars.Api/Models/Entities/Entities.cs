namespace AstraFleetWars.Api.Models.Entities;

public class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class FleetLoadout
{
    public string Flagship { get; set; } = "Vanguard-I";
    public List<string> Escorts { get; set; } = new();
    public List<string> Modules { get; set; } = new();
}

public class PlayerMetadata
{
    public Guid PlayerId { get; set; }
    public int Level { get; set; } = 1;
    public long Experience { get; set; } = 0;
    public long Credits { get; set; } = 1000;
    public int DarkMatter { get; set; } = 50;
    public int HighestUnlockedChapter { get; set; } = 1;
    public int FleetPower { get; set; } = 100;
    public FleetLoadout ActiveFleetLoadout { get; set; } = new();
    public long RevisionVersion { get; set; } = 1;
    public DateTime LastSyncTimestamp { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ChapterProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public int ChapterNumber { get; set; }
    public bool Completed { get; set; }
    public int Stars { get; set; } // 0..3
    public long HighScore { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}

public class PlayerAchievement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public string AchievementId { get; set; } = string.Empty;
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ConflictRecord
{
    public string Type { get; set; } = string.Empty; // CHAPTER_PROGRESS, ACHIEVEMENT, FLEET_VERSION, CURRENCY_OVERFLOW
    public string Field { get; set; } = string.Empty;
    public object? ClientValue { get; set; }
    public object? ServerValue { get; set; }
    public string Resolution { get; set; } = string.Empty;
    public object? AppliedValue { get; set; }
}

public class SyncAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlayerId { get; set; }
    public string SyncId { get; set; } = string.Empty;
    public string ClientVersion { get; set; } = "1.0.0";
    public Dictionary<string, object> DeviceInfo { get; set; } = new();
    public DateTime ClientTimestamp { get; set; }
    public DateTime ServerTimestamp { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "SUCCESS"; // SUCCESS, CONFLICT_RESOLVED, REJECTED
    public List<ConflictRecord> ConflictsDetected { get; set; } = new();
    public Dictionary<string, object> AppliedChanges { get; set; } = new();
}

public class PassiveEventLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? PlayerId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> Payload { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
