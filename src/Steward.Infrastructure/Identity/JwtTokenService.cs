using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Steward.Application.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Steward.Infrastructure.Identity;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string GenerateToken(JwtTokenRequest request)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is required.");
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var expiryMinutes = configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 30;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, request.Email),
            new("name", request.DisplayName ?? request.Email),
        };
        claims.AddRange(request.Roles.Select(role => new Claim("role", role)));

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
