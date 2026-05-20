using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
    private readonly string _issuer;
    private readonly string _audience;

    public TokenService(IConfiguration config)
    {
        _config = config;
        var secret = _config["JWT_SECRET"] ?? "gearOps-super-secret-key-change-in-production-2024!";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        _issuer = _config["JWT_ISSUER"] ?? "gearOps";
        _audience = _config["JWT_AUDIENCE"] ?? "gearOps-client";
    }

    public string GenerateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = GetAccessTokenExpiry(),
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken(User user)
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    public DateTime GetAccessTokenExpiry()
    {
        var expirationMsStr = _config["JWT_EXPIRATION"] ?? "3600000"; // 1 hour default
        var expirationMs = double.Parse(expirationMsStr);
        return DateTime.UtcNow.AddMilliseconds(expirationMs);
    }

    public DateTime GetRefreshTokenExpiry()
    {
        var expirationDaysStr = _config["REFRESH_TOKEN_EXPIRATION_DAYS"] ?? "30";
        var expirationDays = double.Parse(expirationDaysStr);
        return DateTime.UtcNow.AddDays(expirationDays);
    }
}
