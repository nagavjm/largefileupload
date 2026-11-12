using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace LargeFileUpload.API.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : BaseApiController
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public record LoginRequest(string Username, string? Role);
    public record TokenResponse(string Token, string Username, string UserId, DateTime Expiration);

    /// <summary>
    /// Generates a test JWT token for the specified user and role.
    /// </summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    public IActionResult GenerateToken([FromBody] LoginRequest request)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var secretKey = jwtSection["Key"] ?? "EnterpriseLargeFileUploadSigningSecretKey2026!MustBeVeryLongAndSecure";
        var issuer = jwtSection["Issuer"] ?? "LargeFileUploadApi";
        var audience = jwtSection["Audience"] ?? "LargeFileUploadClient";
        var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var exp) ? exp : 120;

        var userId = $"usr-{Math.Abs(request.Username.GetHashCode()):X8}".ToLowerInvariant();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Name, request.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ClaimTypes.Role, request.Role ?? "StandardUser")
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return Ok(new TokenResponse(
            Token: new JwtSecurityTokenHandler().WriteToken(token),
            Username: request.Username,
            UserId: userId,
            Expiration: expires));
    }
}
