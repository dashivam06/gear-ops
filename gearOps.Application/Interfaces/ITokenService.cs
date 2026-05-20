using System;
using gearOps.Domain.Entities;

namespace gearOps.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(User user);
    DateTime GetAccessTokenExpiry();
    DateTime GetRefreshTokenExpiry();
}
