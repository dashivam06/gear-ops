using System.Threading.Tasks;

namespace gearOps.Application.Interfaces;

public interface IOtpService
{
    Task<string> GenerateAndStoreOtpAsync(string email);
    Task<bool> VerifyOtpAsync(string verificationId, string otp);
    Task<string?> GetVerifiedEmailAsync(string verificationId);
    Task ClearOtpAsync(string verificationId);
}
