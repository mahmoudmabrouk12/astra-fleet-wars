using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AstraFleetWars.Api.Services;

public interface IJwtService
{
    string GenerateToken(Guid playerId, string username, bool isGuest = false);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(Guid playerId, string username, bool isGuest = false)
    {
        var secret = _configuration["Jwt:Secret"] ?? "astra-syndicate-fleet-wars-super-secret-key-2024-min-32-chars";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, playerId.ToString()),
            new Claim("playerId", playerId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim("isGuest", isGuest.ToString().ToLower()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "AstraFleetWars",
            audience: _configuration["Jwt:Audience"] ?? "AstraFleetWarsClient",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
