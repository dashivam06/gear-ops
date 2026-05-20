using System.Threading.Tasks;
using gearOps.Domain.Entities;

namespace gearOps.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(int userId);
    Task<User> AddUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetActiveRefreshTokenAsync(string tokenHash);
    Task<List<RefreshToken>> GetAllActiveRefreshTokensForUserAsync(int userId);
    Task RevokeRefreshTokenAsync(RefreshToken refreshToken);
}
