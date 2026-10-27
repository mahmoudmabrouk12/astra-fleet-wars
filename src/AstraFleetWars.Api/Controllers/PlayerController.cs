using System.Security.Claims;
using AstraFleetWars.Api.Models.DTOs;
using AstraFleetWars.Api.Models.Entities;
using AstraFleetWars.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstraFleetWars.Api.Controllers;

[ApiController]
[Route("api/v1/player")]
[Authorize]
public class PlayerController : ControllerBase
{
    private readonly IPlayerRepository _playerRepository;

    public PlayerController(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    private Guid GetCurrentPlayerId()
    {
        var claim = User.FindFirst("playerId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim != null && Guid.TryParse(claim.Value, out var id))
        {
            return id;
        }
        throw new UnauthorizedAccessException("Invalid or missing player identifier claim in token.");
    }

    /// <summary>
    /// Returns authoritative profile, metadata, chapters, and achievements.
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var playerId = GetCurrentPlayerId();
        var profile = await _playerRepository.GetProfileAsync(playerId);

        if (profile == null)
        {
            return NotFound(new ApiResponse<object> { Success = false, Error = "Player profile not found." });
        }

        var metadata = await _playerRepository.GetPlayerMetadataAsync(playerId) ?? new PlayerMetadata
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

        var chapters = await _playerRepository.GetChapterProgressAsync(playerId);
        var achievements = await _playerRepository.GetAchievementsAsync(playerId);

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = new
            {
                profile,
                metadata,
                chapters,
                achievements
            }
        });
    }
}
