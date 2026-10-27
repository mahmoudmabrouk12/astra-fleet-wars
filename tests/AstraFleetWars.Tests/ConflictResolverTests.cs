using AstraFleetWars.Api.Models.DTOs;
using AstraFleetWars.Api.Models.Entities;
using AstraFleetWars.Api.Services;
using Xunit;

namespace AstraFleetWars.Tests;

public class ConflictResolverTests
{
    private readonly PlayerMetadata _mockMetadata = new()
    {
        PlayerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
        Level = 5,
        Experience = 4000,
        Credits = 10000,
        DarkMatter = 100,
        HighestUnlockedChapter = 3,
        FleetPower = 1500,
        ActiveFleetLoadout = new FleetLoadout
        {
            Flagship = "Vanguard-I",
            Escorts = new List<string> { "Corvette-Alpha" }
        },
        RevisionVersion = 5,
        LastSyncTimestamp = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private readonly List<ChapterProgress> _mockChapters = new()
    {
        new ChapterProgress { ChapterNumber = 1, Completed = true, Stars = 3, HighScore = 15000 },
        new ChapterProgress { ChapterNumber = 2, Completed = true, Stars = 2, HighScore = 10000 }
    };

    private readonly List<PlayerAchievement> _mockAchievements = new()
    {
        new PlayerAchievement { AchievementId = "ACH_STARTER", UnlockedAt = DateTime.UtcNow }
    };

    [Fact]
    public void UpgradesChapterProgression_WhenClientAchievesHigherStars()
    {
        var payload = new SyncRequestDto
        {
            SyncId = Guid.NewGuid(),
            ClientTimestamp = DateTime.UtcNow,
            Chapters = new List<ClientChapterDto>
            {
                new() { ChapterNumber = 2, Completed = true, Stars = 3, HighScore = 18000 },
                new() { ChapterNumber = 3, Completed = true, Stars = 1, HighScore = 5000 }
            },
            CurrencyDelta = new ClientCurrencyDeltaDto { CreditsEarned = 500, ExperienceEarned = 200 }
        };

        var result = ConflictResolver.Resolve(_mockMetadata, _mockChapters, _mockAchievements, payload);

        Assert.False(result.HasConflicts);
        var ch2 = result.ResolvedChapters.FirstOrDefault(c => c.ChapterNumber == 2);
        Assert.NotNull(ch2);
        Assert.Equal(3, ch2.Stars);
        Assert.Equal(18000, ch2.HighScore);
        Assert.Equal(4, result.ResolvedMetadata.HighestUnlockedChapter);
    }

    [Fact]
    public void ProtectsServerStars_WhenClientReplaysWithLowerScore()
    {
        var payload = new SyncRequestDto
        {
            SyncId = Guid.NewGuid(),
            ClientTimestamp = DateTime.UtcNow,
            Chapters = new List<ClientChapterDto>
            {
                new() { ChapterNumber = 1, Completed = true, Stars = 1, HighScore = 4000 } // Lower than server (3 stars, 15000 score)
            }
        };

        var result = ConflictResolver.Resolve(_mockMetadata, _mockChapters, _mockAchievements, payload);

        Assert.True(result.HasConflicts);
        var ch1 = result.ResolvedChapters.FirstOrDefault(c => c.ChapterNumber == 1);
        Assert.NotNull(ch1);
        Assert.Equal(3, ch1.Stars); // Server's 3 stars strictly preserved!
        Assert.Equal(15000, ch1.HighScore);
    }

    [Fact]
    public void PerformsAdditiveSetUnion_OnAchievements()
    {
        var payload = new SyncRequestDto
        {
            SyncId = Guid.NewGuid(),
            ClientTimestamp = DateTime.UtcNow,
            Achievements = new List<ClientAchievementDto>
            {
                new() { AchievementId = "ACH_OFFLINE_HERO", UnlockedAt = DateTime.UtcNow }
            }
        };

        var result = ConflictResolver.Resolve(_mockMetadata, _mockChapters, _mockAchievements, payload);

        Assert.Equal(2, result.ResolvedAchievements.Count);
        Assert.Contains(result.ResolvedAchievements, a => a.AchievementId == "ACH_STARTER");
        Assert.Contains(result.ResolvedAchievements, a => a.AchievementId == "ACH_OFFLINE_HERO");
    }

    [Fact]
    public void RejectsStaleFleetLoadout_WithLowerRevisionVersion()
    {
        var payload = new SyncRequestDto
        {
            SyncId = Guid.NewGuid(),
            ClientTimestamp = DateTime.UtcNow,
            Fleet = new ClientFleetDto
            {
                Flagship = "Old-Shuttle",
                RevisionVersion = 3 // Stale! Server is version 5
            }
        };

        var result = ConflictResolver.Resolve(_mockMetadata, _mockChapters, _mockAchievements, payload);

        Assert.True(result.HasConflicts);
        Assert.Equal("Vanguard-I", result.ResolvedMetadata.ActiveFleetLoadout.Flagship);
    }

    [Fact]
    public void EnforcesAntiCheatCeiling_OnExcessiveOfflineCurrency()
    {
        var payload = new SyncRequestDto
        {
            SyncId = Guid.NewGuid(),
            ClientTimestamp = DateTime.UtcNow,
            CurrencyDelta = new ClientCurrencyDeltaDto
            {
                CreditsEarned = 99999999, // Exploit attempt
                DarkMatterEarned = 99999
            }
        };

        var result = ConflictResolver.Resolve(_mockMetadata, _mockChapters, _mockAchievements, payload);

        Assert.True(result.HasConflicts);
        // Capped to MaxOfflineCreditsPerSync (50,000) => 10,000 + 50,000 = 60,000
        Assert.Equal(60000, result.ResolvedMetadata.Credits);
        // Capped to MaxOfflineDarkMatterPerSync (500) => 100 + 500 = 600
        Assert.Equal(600, result.ResolvedMetadata.DarkMatter);
    }

    [Fact]
    public async Task SyncService_EnforcesIdempotency_OnRepeatedSyncId()
    {
        var repo = new InMemoryPlayerRepository();
        var syncService = new SyncService(repo);
        var playerId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        var syncId = Guid.NewGuid();

        var payload = new SyncRequestDto
        {
            SyncId = syncId,
            ClientTimestamp = DateTime.UtcNow,
            CurrencyDelta = new ClientCurrencyDeltaDto { CreditsEarned = 2500 }
        };

        // First execution
        var firstResult = await syncService.ExecuteSyncAsync(playerId, payload);
        Assert.Equal("SUCCESS", firstResult.Status);
        long creditsAfterFirst = firstResult.AuthoritativeState.Metadata.Credits;

        // Second execution (same sync_id)
        var secondResult = await syncService.ExecuteSyncAsync(playerId, payload);
        Assert.Equal("ALREADY_SYNCED", secondResult.Status);
        Assert.Equal(creditsAfterFirst, secondResult.AuthoritativeState.Metadata.Credits); // Must not double-credit!
    }
}
