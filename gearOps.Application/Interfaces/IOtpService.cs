using System.Threading.Tasks;

namespace gearOps.Application.Interfaces;

public interface IOtpService
{
    Task<string> GenerateAndStoreOtpAsync(string email);
    Task<bool> VerifyOtpAsync(string verificationId, string otp);
    Task<string?> GetOtpAsync(string verificationId);
    Task<string?> GetVerifiedEmailAsync(string verificationId);
    Task ClearOtpAsync(string verificationId);
}
