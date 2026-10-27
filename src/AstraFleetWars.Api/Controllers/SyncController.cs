using System.Security.Claims;
using AstraFleetWars.Api.Models.DTOs;
using AstraFleetWars.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AstraFleetWars.Api.Controllers;

[ApiController]
[Route("api/v1/sync")]
[Authorize]
public class SyncController : ControllerBase
{
    private readonly ISyncService _syncService;
    private readonly IPlayerRepository _playerRepository;

    public SyncController(ISyncService syncService, IPlayerRepository playerRepository)
    {
        _syncService = syncService;
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
    /// Synchronizes offline chapters, achievements, and fleet loadouts with deterministic conflict resolution.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Sync([FromBody] SyncRequestDto payload)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Error = "Invalid sync payload structure",
                Data = ModelState
            });
        }

        var playerId = GetCurrentPlayerId();
        var result = await _syncService.ExecuteSyncAsync(playerId, payload);

        return Ok(new ApiResponse<SyncResponseDataDto>
        {
            Success = true,
            Data = result
        });
    }

    /// <summary>
    /// Retrieves current server revision version and last sync timestamp.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var playerId = GetCurrentPlayerId();
        var metadata = await _playerRepository.GetPlayerMetadataAsync(playerId);

        if (metadata == null)
        {
            return NotFound(new ApiResponse<object> { Success = false, Error = "Player metadata not found." });
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = new
            {
                player_id = metadata.PlayerId,
                revision_version = metadata.RevisionVersion,
                last_sync_timestamp = metadata.LastSyncTimestamp,
                highest_unlocked_chapter = metadata.HighestUnlockedChapter
            }
        });
    }
}
