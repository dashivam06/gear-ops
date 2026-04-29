using System.Threading.Tasks;
using gearOps.Application.DTOs;

namespace gearOps.Application.Interfaces;

public interface IAuthService
{
    Task<string> RequestRegistrationOtpAsync(RequestOtpDto dto);
    Task<bool> VerifyRegistrationOtpAsync(VerifyOtpDto dto);
    Task<AuthResponseDto> FinalizeRegistrationAsync(RegisterUserDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<string> RequestPasswordResetOtpAsync(RequestPasswordResetDto dto);
    Task<bool> VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpDto dto);
    Task<bool> ResetPasswordAsync(ResetPasswordDto dto);
    Task<bool> LogoutAsync();
}
