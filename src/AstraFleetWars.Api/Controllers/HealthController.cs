using AstraFleetWars.Api.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace AstraFleetWars.Api.Controllers;

[ApiController]
[Route("")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public HealthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        bool hasSupabase = !string.IsNullOrWhiteSpace(_configuration["Supabase:Url"]);
        return Ok(new
        {
            status = "UP",
            service = "Astra Syndicate: Fleet Wars API (.NET 9)",
            sprint = "Sprint 1",
            supabase_configured = hasSupabase,
            timestamp = DateTime.UtcNow
        });
    }
}
