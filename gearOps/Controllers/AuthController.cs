using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using gearOps.Application.Interfaces;
using gearOps.Application.DTOs;

namespace gearOps.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Request OTP for user registration</summary>
    [HttpPost("request-otp")]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpDto request)
    {
        var verificationId = await _authService.RequestRegistrationOtpAsync(request);
        return Ok(new { VerificationId = verificationId });
    }

    /// <summary>Verify OTP sent to user's email</summary>
    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto request)
    {
        var verified = await _authService.VerifyRegistrationOtpAsync(request);
        if (!verified)
            throw new gearOps.Application.Exceptions.BadRequestException("Invalid or expired OTP.");
        return Ok(new { Verified = true });
    }

    /// <summary>Complete user registration</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto request)
    {
        var response = await _authService.FinalizeRegistrationAsync(request);
        return Ok(response);
    }

    /// <summary>Authenticate user and get tokens</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }

    /// <summary>Use a refresh token to get a new access token and refresh token</summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
    {
        var response = await _authService.RefreshTokenAsync(request);
        return Ok(response);
    }

    /// <summary>Request OTP for password reset</summary>
    [HttpPost("request-password-reset")]
    public async Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetDto request)
    {
        var verificationId = await _authService.RequestPasswordResetOtpAsync(request);
        return Ok(new { VerificationId = verificationId });
    }

    /// <summary>Verify OTP for password reset</summary>
    [HttpPost("verify-password-reset-otp")]
    public async Task<IActionResult> VerifyPasswordResetOtp([FromBody] VerifyPasswordResetOtpDto request)
    {
        var verified = await _authService.VerifyPasswordResetOtpAsync(request);
        if (!verified)
            throw new gearOps.Application.Exceptions.BadRequestException("Invalid or expired OTP.");
        return Ok(new { Verified = true });
    }

    /// <summary>Reset user password</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        var updated = await _authService.ResetPasswordAsync(request);
        return Ok(new { Updated = updated });
    }

    /// <summary>Logout user (invalidate tokens)</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
    {
        var loggedOut = await _authService.LogoutAsync(request);
        return Ok(new { LoggedOut = loggedOut });
    }

    /// <summary>Change user password (authenticated users only)</summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        var userId = GetUserIdFromToken();
        await _authService.ChangePasswordAsync(userId, request);
        return Ok(new { message = "Password changed successfully." });
    }

    /// <summary>Delete user account (authenticated users only)</summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("delete-account")]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto request)
    {
        var userId = GetUserIdFromToken();
        await _authService.DeleteAccountAsync(userId, request);
        return Ok(new { message = "Account deleted successfully." });
    }

    // GetUserIdFromToken() inherited from BaseApiController
}
