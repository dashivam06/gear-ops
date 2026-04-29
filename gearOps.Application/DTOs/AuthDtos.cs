using System;

namespace gearOps.Application.DTOs;

public class RequestOtpDto
{
    public string Email { get; set; } = null!;
}

public class VerifyOtpDto
{
    public string VerificationId { get; set; } = null!;
    public string Otp { get; set; } = null!;
}

public class RegisterUserDto
{
    public string VerificationId { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
    public string? ProfileImageUrl { get; set; }
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public bool EmailSubscribed { get; set; }
}

public class LoginDto
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class RequestPasswordResetDto
{
    public string Email { get; set; } = null!;
}

public class VerifyPasswordResetOtpDto
{
    public string VerificationId { get; set; } = null!;
    public string Otp { get; set; } = null!;
}

public class ResetPasswordDto
{
    public string VerificationId { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public DateTime ExpiresIn { get; set; }
    public object UserProfile { get; set; } = null!;
}
