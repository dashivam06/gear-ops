using System.Threading.Tasks;
using gearOps.Domain.Entities;

namespace gearOps.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken(User user);
}
