using System.ComponentModel.DataAnnotations;
using AstraFleetWars.Api.Models.Entities;

namespace AstraFleetWars.Api.Models.DTOs;

public class ClientChapterDto
{
    [Required]
    [Range(1, 1000)]
    public int ChapterNumber { get; set; }

    [Required]
    public bool Completed { get; set; }

    [Range(0, 3)]
    public int Stars { get; set; }

    [Range(0, long.MaxValue)]
    public long HighScore { get; set; }

    public DateTime? CompletedAt { get; set; }
}

public class ClientAchievementDto
{
    [Required]
    [MinLength(3)]
    public string AchievementId { get; set; } = string.Empty;

    [Required]
    public DateTime UnlockedAt { get; set; }

    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ClientFleetDto
{
    [Required]
    public string Flagship { get; set; } = string.Empty;

    public List<string> Escorts { get; set; } = new();

    public List<string> Modules { get; set; } = new();

    [Range(0, long.MaxValue)]
    public long RevisionVersion { get; set; }
}

public class ClientCurrencyDeltaDto
{
    [Range(0, int.MaxValue)]
    public long CreditsEarned { get; set; } = 0;

    [Range(0, int.MaxValue)]
    public int DarkMatterEarned { get; set; } = 0;

    [Range(0, int.MaxValue)]
    public long ExperienceEarned { get; set; } = 0;
}

public class SyncRequestDto
{
    [Required]
    public Guid SyncId { get; set; }

    [Required]
    public DateTime ClientTimestamp { get; set; }

    public string ClientVersion { get; set; } = "1.0.0";

    public Dictionary<string, object> DeviceInfo { get; set; } = new();

    public List<ClientChapterDto> Chapters { get; set; } = new();

    public List<ClientAchievementDto> Achievements { get; set; } = new();

    public ClientFleetDto? Fleet { get; set; }

    public ClientCurrencyDeltaDto? CurrencyDelta { get; set; }
}

public class AuthoritativeStateDto
{
    public PlayerMetadata Metadata { get; set; } = null!;
    public List<ChapterProgress> Chapters { get; set; } = new();
    public List<PlayerAchievement> Achievements { get; set; } = new();
}

public class SyncResponseDataDto
{
    public Guid SyncId { get; set; }
    public string Status { get; set; } = "SUCCESS"; // SUCCESS, CONFLICT_RESOLVED, ALREADY_SYNCED
    public DateTime ServerTimestamp { get; set; } = DateTime.UtcNow;
    public bool HasConflicts { get; set; }
    public List<ConflictRecord> Conflicts { get; set; } = new();
    public AuthoritativeStateDto AuthoritativeState { get; set; } = null!;
}

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public string? Error { get; set; }
    public T? Data { get; set; }
}

public class GuestLoginRequestDto
{
    public string? DeviceId { get; set; }
}

public class LoginRequestDto
{
    [Required]
    public string Username { get; set; } = string.Empty;
}

public class RegisterRequestDto
{
    [Required]
    [MinLength(3)]
    public string Username { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public Profile Player { get; set; } = null!;
}

public class PassiveLogRequestDto
{
    [Required]
    public string EventType { get; set; } = string.Empty;

    public Guid? PlayerId { get; set; }

    public Dictionary<string, object> Payload { get; set; } = new();
}
