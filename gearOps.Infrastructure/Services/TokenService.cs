using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;

namespace gearOps.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    private readonly SymmetricSecurityKey _key;

    public TokenService(IConfiguration config)
    {
        _config = config;
        // Uses the length requirement for HS512 (at least 64 bytes)
        var secret = _config["JWT_SECRET"] ?? "myVerySecureSecretKeyThatIsAtLeast88CharactersLongForHS512SigningAlgorithmUsageInApplications";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.NameId, user.Email),
            new Claim("userId", user.UserId.ToString()),
            new Claim("role", user.Role.ToString())
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);
        
        var expirationMsStr = _config["JWT_EXPIRATION"] ?? "86400000";
        var expirationMs = double.Parse(expirationMsStr);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMilliseconds(expirationMs),
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken(User user)
    {
        // Simple random token for refresh logic
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
