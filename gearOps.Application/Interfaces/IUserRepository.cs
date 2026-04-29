using System.Threading.Tasks;
using gearOps.Domain.Entities;

namespace gearOps.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> AddUserAsync(User user);
    Task UpdateUserAsync(User user);
}
