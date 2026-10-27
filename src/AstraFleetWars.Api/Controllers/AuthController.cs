using AstraFleetWars.Api.Models.DTOs;
using AstraFleetWars.Api.Models.Entities;
using AstraFleetWars.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AstraFleetWars.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IJwtService _jwtService;

    public AuthController(IPlayerRepository playerRepository, IJwtService jwtService)
    {
        _playerRepository = playerRepository;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Guest login for mobile players booting up for the first time or starting offline.
    /// </summary>
    [HttpPost("guest")]
    public async Task<IActionResult> GuestLogin([FromBody] GuestLoginRequestDto? request)
    {
        var randomSuffix = Random.Shared.Next(1000, 9999);
        var username = $"pilot_{randomSuffix}";
        var playerId = Guid.NewGuid();

        var profile = new Profile
        {
            Id = playerId,
            Username = username,
            DisplayName = $"Cadet {username}",
            AvatarUrl = $"https://api.dicebear.com/7.x/bottts/svg?seed={username}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _playerRepository.CreateProfileAsync(profile);
        var token = _jwtService.GenerateToken(profile.Id, profile.Username, isGuest: true);

        return Ok(new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Message = "Guest session initialized successfully",
            Data = new AuthResponseDto
            {
                Token = token,
                Player = profile
            }
        });
    }

    /// <summary>
    /// Login with existing username (e.g. demo account 'commander_reyes').
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new ApiResponse<object> { Success = false, Error = "Username is required." });
        }

        var profile = await _playerRepository.GetProfileByUsernameAsync(request.Username);
        if (profile == null)
        {
            return NotFound(new ApiResponse<object> { Success = false, Error = "Player profile not found." });
        }

        var token = _jwtService.GenerateToken(profile.Id, profile.Username);

        return Ok(new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Message = "Logged in successfully",
            Data = new AuthResponseDto
            {
                Token = token,
                Player = profile
            }
        });
    }

    /// <summary>
    /// Register a new commander account.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new ApiResponse<object> { Success = false, Error = "Username is required." });
        }

        var existing = await _playerRepository.GetProfileByUsernameAsync(request.Username);
        if (existing != null)
        {
            return Conflict(new ApiResponse<object> { Success = false, Error = "Username is already taken." });
        }

        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Username : request.DisplayName,
            AvatarUrl = $"https://api.dicebear.com/7.x/bottts/svg?seed={request.Username}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _playerRepository.CreateProfileAsync(profile);
        var token = _jwtService.GenerateToken(profile.Id, profile.Username);

        return CreatedAtAction(nameof(Login), new ApiResponse<AuthResponseDto>
        {
            Success = true,
            Message = "Commander profile created successfully",
            Data = new AuthResponseDto
            {
                Token = token,
                Player = profile
            }
        });
    }
}
