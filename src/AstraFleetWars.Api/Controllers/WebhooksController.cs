using AstraFleetWars.Api.Models.DTOs;
using AstraFleetWars.Api.Models.Entities;
using AstraFleetWars.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraFleetWars.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IPlayerRepository _playerRepository;

    public WebhooksController(IPlayerRepository playerRepository)
    {
        _playerRepository = playerRepository;
    }

    /// <summary>
    /// Zero-cost passive telemetry logging. Responds immediately with HTTP 202 Accepted.
    /// </summary>
    [HttpPost("log")]
    public IActionResult LogEvent([FromBody] PassiveLogRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return BadRequest(new ApiResponse<object> { Success = false, Error = "EventType is required." });
        }

        // Return 202 Accepted immediately so mobile gameplay loop is not blocked
        var acceptedResponse = Accepted(new ApiResponse<object>
        {
            Success = true,
            Message = "ACCEPTED",
            Data = new { received_at = DateTime.UtcNow }
        });

        // Fire-and-forget asynchronous background logging
        _ = Task.Run(async () =>
        {
            try
            {
                await _playerRepository.SavePassiveLogAsync(new PassiveEventLog
                {
                    PlayerId = request.PlayerId,
                    EventType = request.EventType,
                    Payload = request.Payload,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[PassiveLog Error] {ex.Message}");
            }
        });

        return acceptedResponse;
    }

    /// <summary>
    /// View recent passive events (internal telemetry).
    /// </summary>
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int limit = 50)
    {
        var logs = await _playerRepository.GetPassiveLogsAsync(limit);
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Data = new { count = logs.Count, logs }
        });
    }
}
