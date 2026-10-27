using AstraFleetWars.Api.Models.DTOs;
using AstraFleetWars.Api.Models.Entities;

namespace AstraFleetWars.Api.Services;

public class ResolvedSyncResult
{
    public bool HasConflicts => Conflicts.Count > 0;
    public List<ConflictRecord> Conflicts { get; set; } = new();
    public PlayerMetadata ResolvedMetadata { get; set; } = null!;
    public List<ChapterProgress> ResolvedChapters { get; set; } = new();
    public List<PlayerAchievement> ResolvedAchievements { get; set; } = new();
}

public static class ConflictResolver
{
    public const long MaxOfflineCreditsPerSync = 50000;
    public const int MaxOfflineDarkMatterPerSync = 500;

    /// <summary>
    /// Executes deterministic conflict resolution protecting player progress at all costs.
    /// </summary>
    public static ResolvedSyncResult Resolve(
        PlayerMetadata currentMetadata,
        List<ChapterProgress> currentChapters,
        List<PlayerAchievement> currentAchievements,
        SyncRequestDto clientPayload)
    {
        var result = new ResolvedSyncResult();

        // -------------------------------------------------------------
        // 1. Resolve Chapter Progress (Monotonic Progression Rule)
        // -------------------------------------------------------------
        var chapterMap = currentChapters.ToDictionary(
            c => c.ChapterNumber,
            c => new ChapterProgress
            {
                Id = c.Id,
                PlayerId = c.PlayerId,
                ChapterNumber = c.ChapterNumber,
                Completed = c.Completed,
                Stars = c.Stars,
                HighScore = c.HighScore,
                CompletedAt = c.CompletedAt,
                SyncedAt = c.SyncedAt
            });

        int highestUnlocked = currentMetadata.HighestUnlockedChapter;

        foreach (var clientCh in clientPayload.Chapters)
        {
            if (!chapterMap.TryGetValue(clientCh.ChapterNumber, out var serverCh))
            {
                // New offline chapter achievement
                chapterMap[clientCh.ChapterNumber] = new ChapterProgress
                {
                    PlayerId = currentMetadata.PlayerId,
                    ChapterNumber = clientCh.ChapterNumber,
                    Completed = clientCh.Completed,
                    Stars = clientCh.Stars,
                    HighScore = clientCh.HighScore,
                    CompletedAt = clientCh.CompletedAt ?? DateTime.UtcNow,
                    SyncedAt = DateTime.UtcNow
                };

                if (clientCh.Completed && clientCh.ChapterNumber >= highestUnlocked)
                {
                    highestUnlocked = clientCh.ChapterNumber + 1;
                }
            }
            else
            {
                // Existing chapter on server - detect conflicts & protect progress
                bool completed = serverCh.Completed || clientCh.Completed;
                int stars = serverCh.Stars;
                long highScore = serverCh.HighScore;

                if (clientCh.Stars > serverCh.Stars)
                {
                    stars = clientCh.Stars;
                }
                else if (clientCh.Stars < serverCh.Stars && clientCh.Stars > 0)
                {
                    result.Conflicts.Add(new ConflictRecord
                    {
                        Type = "CHAPTER_PROGRESS",
                        Field = $"chapter_{clientCh.ChapterNumber}_stars",
                        ClientValue = clientCh.Stars,
                        ServerValue = serverCh.Stars,
                        Resolution = "Protected higher server stars (retained best score)",
                        AppliedValue = serverCh.Stars
                    });
                }

                if (clientCh.HighScore > serverCh.HighScore)
                {
                    highScore = clientCh.HighScore;
                }
                else if (clientCh.HighScore < serverCh.HighScore && clientCh.HighScore > 0)
                {
                    result.Conflicts.Add(new ConflictRecord
                    {
                        Type = "CHAPTER_PROGRESS",
                        Field = $"chapter_{clientCh.ChapterNumber}_high_score",
                        ClientValue = clientCh.HighScore,
                        ServerValue = serverCh.HighScore,
                        Resolution = "Protected higher server high-score",
                        AppliedValue = serverCh.HighScore
                    });
                }

                serverCh.Completed = completed;
                serverCh.Stars = stars;
                serverCh.HighScore = highScore;
                serverCh.CompletedAt = clientCh.CompletedAt ?? serverCh.CompletedAt;
                serverCh.SyncedAt = DateTime.UtcNow;

                if (completed && clientCh.ChapterNumber >= highestUnlocked)
                {
                    highestUnlocked = clientCh.ChapterNumber + 1;
                }
            }
        }

        result.ResolvedChapters = chapterMap.Values.OrderBy(c => c.ChapterNumber).ToList();

        // -------------------------------------------------------------
        // 2. Resolve Achievements (Additive Set Union Rule)
        // -------------------------------------------------------------
        var achMap = currentAchievements.ToDictionary(
            a => a.AchievementId,
            a => new PlayerAchievement
            {
                Id = a.Id,
                PlayerId = a.PlayerId,
                AchievementId = a.AchievementId,
                UnlockedAt = a.UnlockedAt,
                Metadata = new Dictionary<string, object>(a.Metadata)
            });

        foreach (var clientAch in clientPayload.Achievements)
        {
            if (!achMap.ContainsKey(clientAch.AchievementId))
            {
                achMap[clientAch.AchievementId] = new PlayerAchievement
                {
                    PlayerId = currentMetadata.PlayerId,
                    AchievementId = clientAch.AchievementId,
                    UnlockedAt = clientAch.UnlockedAt,
                    Metadata = clientAch.Metadata
                };
            }
        }

        result.ResolvedAchievements = achMap.Values.ToList();

        // -------------------------------------------------------------
        // 3. Resolve Fleet Loadout (Optimistic Revision Locking)
        // -------------------------------------------------------------
        var activeFleet = new FleetLoadout
        {
            Flagship = currentMetadata.ActiveFleetLoadout.Flagship,
            Escorts = new List<string>(currentMetadata.ActiveFleetLoadout.Escorts),
            Modules = new List<string>(currentMetadata.ActiveFleetLoadout.Modules)
        };
        long revisionVersion = currentMetadata.RevisionVersion;

        if (clientPayload.Fleet != null)
        {
            long clientRev = clientPayload.Fleet.RevisionVersion;
            long serverRev = currentMetadata.RevisionVersion;

            if (clientRev >= serverRev)
            {
                activeFleet = new FleetLoadout
                {
                    Flagship = clientPayload.Fleet.Flagship,
                    Escorts = new List<string>(clientPayload.Fleet.Escorts),
                    Modules = new List<string>(clientPayload.Fleet.Modules)
                };
                revisionVersion = clientRev + 1;
            }
            else
            {
                result.Conflicts.Add(new ConflictRecord
                {
                    Type = "FLEET_VERSION",
                    Field = "active_fleet_loadout",
                    ClientValue = clientPayload.Fleet,
                    ServerValue = currentMetadata.ActiveFleetLoadout,
                    Resolution = $"Rejected stale fleet loadout (client version {clientRev} < server version {serverRev})",
                    AppliedValue = currentMetadata.ActiveFleetLoadout
                });
            }
        }

        // -------------------------------------------------------------
        // 4. Resolve Currencies & XP (Anti-Cheat Bounded Ceiling)
        // -------------------------------------------------------------
        long creditsToAdd = clientPayload.CurrencyDelta?.CreditsEarned ?? 0;
        int darkMatterToAdd = clientPayload.CurrencyDelta?.DarkMatterEarned ?? 0;
        long xpToAdd = clientPayload.CurrencyDelta?.ExperienceEarned ?? 0;

        if (creditsToAdd > MaxOfflineCreditsPerSync)
        {
            result.Conflicts.Add(new ConflictRecord
            {
                Type = "CURRENCY_OVERFLOW",
                Field = "credits",
                ClientValue = creditsToAdd,
                ServerValue = MaxOfflineCreditsPerSync,
                Resolution = $"Capped offline credit gain to maximum threshold ({MaxOfflineCreditsPerSync})",
                AppliedValue = MaxOfflineCreditsPerSync
            });
            creditsToAdd = MaxOfflineCreditsPerSync;
        }

        if (darkMatterToAdd > MaxOfflineDarkMatterPerSync)
        {
            result.Conflicts.Add(new ConflictRecord
            {
                Type = "CURRENCY_OVERFLOW",
                Field = "dark_matter",
                ClientValue = darkMatterToAdd,
                ServerValue = MaxOfflineDarkMatterPerSync,
                Resolution = $"Capped offline dark matter gain to maximum threshold ({MaxOfflineDarkMatterPerSync})",
                AppliedValue = MaxOfflineDarkMatterPerSync
            });
            darkMatterToAdd = MaxOfflineDarkMatterPerSync;
        }

        long resolvedExperience = currentMetadata.Experience + xpToAdd;
        int resolvedLevel = Math.Max(1, (int)(resolvedExperience / 1000) + 1);

        result.ResolvedMetadata = new PlayerMetadata
        {
            PlayerId = currentMetadata.PlayerId,
            Level = resolvedLevel,
            Experience = resolvedExperience,
            Credits = currentMetadata.Credits + creditsToAdd,
            DarkMatter = currentMetadata.DarkMatter + darkMatterToAdd,
            HighestUnlockedChapter = Math.Max(highestUnlocked, currentMetadata.HighestUnlockedChapter),
            FleetPower = 100 + (resolvedLevel * 50) + (activeFleet.Escorts.Count * 100),
            ActiveFleetLoadout = activeFleet,
            RevisionVersion = revisionVersion,
            LastSyncTimestamp = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return result;
    }
}
