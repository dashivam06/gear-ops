using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;
using gearOps.Infrastructure.Data;

namespace gearOps.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted);
    }

    public async Task<User> AddUserAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetActiveRefreshTokenAsync(string tokenHash)
    {
        var now = DateTime.UtcNow;
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt =>
                rt.TokenHash == tokenHash &&
                !rt.IsRevoked &&
                rt.ExpiresAt > now &&
                !rt.User.IsDeleted);
    }

    public async Task<List<RefreshToken>> GetAllActiveRefreshTokensForUserAsync(int userId)
    {
        var now = DateTime.UtcNow;
        return await _context.RefreshTokens
            .Where(rt =>
                rt.UserId == userId &&
                !rt.IsRevoked &&
                rt.ExpiresAt > now)
            .ToListAsync();
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken refreshToken)
    {
        refreshToken.IsRevoked = true;
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync();
    }
}
