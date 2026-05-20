using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BCrypt.Net;
using gearOps.Application.DTOs;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;
using gearOps.Domain.Enums;
using gearOps.Application.Exceptions;
using gearOps.Application.Helpers;

namespace gearOps.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpService _otpService;
    private readonly IEmailService _emailService;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository userRepository, IOtpService otpService, IEmailService emailService, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _otpService = otpService;
        _emailService = emailService;
        _tokenService = tokenService;
    }

    public async Task<string> RequestRegistrationOtpAsync(RequestOtpDto dto)
    {
        var existingUser = await _userRepository.GetUserByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            throw new ConflictException("Email already exists.");
        }

        var verificationId = await _otpService.GenerateAndStoreOtpAsync(dto.Email);
        try
        {
            await _emailService.SendOtpEmailAsync(dto.Email, verificationId);
        }
        catch
        {
            await _otpService.ClearOtpAsync(verificationId);
            throw;
        }
        
        return verificationId;
    }

    public async Task<bool> VerifyRegistrationOtpAsync(VerifyOtpDto dto)
    {
        return await _otpService.VerifyOtpAsync(dto.VerificationId, dto.Otp);
    }

    public async Task<AuthResponseDto> FinalizeRegistrationAsync(RegisterUserDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
        {
            throw new PasswordMismatchException();
        }

        PasswordValidator.ValidateOrThrow(dto.Password);

        var email = await _otpService.GetVerifiedEmailAsync(dto.VerificationId);
        if (string.IsNullOrEmpty(email))
        {
            throw new BadRequestException("OTP verification not complete or expired.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            FullName = dto.FullName,
            Email = email,
            Phone = dto.Phone,
            Address = dto.Address,
            PasswordHash = passwordHash,
            ProfileImageUrl = dto.ProfileImageUrl,
            Role = Role.Customer
        };

        var createdUser = await _userRepository.AddUserAsync(user);
        await _otpService.ClearOtpAsync(dto.VerificationId);

        var accessToken = _tokenService.GenerateAccessToken(createdUser);
        var refreshToken = await CreateAndStoreRefreshTokenAsync(createdUser);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _tokenService.GetAccessTokenExpiry(),
            UserProfile = new { createdUser.FullName, createdUser.Email, createdUser.Role, createdUser.ProfileImageUrl }
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetUserByEmailAsync(dto.Email);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            if (user != null && string.IsNullOrEmpty(user.PasswordHash))
                throw new BadRequestException("Please login via Social Provider.");
                
            throw new UnauthorizedException("Invalid credentials.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Invalid credentials.");
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await CreateAndStoreRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _tokenService.GetAccessTokenExpiry(),
            UserProfile = new { user.FullName, user.Email, user.Role, user.ProfileImageUrl }
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        var existingToken = await _userRepository.GetActiveRefreshTokenAsync(HashToken(dto.RefreshToken));
        if (existingToken == null)
        {
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        await _userRepository.RevokeRefreshTokenAsync(existingToken);

        var user = existingToken.User;
        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await CreateAndStoreRefreshTokenAsync(user);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = _tokenService.GetAccessTokenExpiry(),
            UserProfile = new { user.FullName, user.Email, user.Role, user.ProfileImageUrl }
        };
    }

    public async Task<string> RequestPasswordResetOtpAsync(RequestPasswordResetDto dto)
    {
        var user = await _userRepository.GetUserByEmailAsync(dto.Email);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new BadRequestException("Password reset is not available for Social Provider accounts.");
        }

        var verificationId = await _otpService.GenerateAndStoreOtpAsync(dto.Email);
        try
        {
            await _emailService.SendPasswordResetOtpEmailAsync(dto.Email, verificationId);
        }
        catch
        {
            await _otpService.ClearOtpAsync(verificationId);
            throw;
        }

        return verificationId;
    }

    public async Task<bool> VerifyPasswordResetOtpAsync(VerifyPasswordResetOtpDto dto)
    {
        return await _otpService.VerifyOtpAsync(dto.VerificationId, dto.Otp);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
        {
            throw new PasswordMismatchException();
        }

        PasswordValidator.ValidateOrThrow(dto.NewPassword);

        var email = await _otpService.GetVerifiedEmailAsync(dto.VerificationId);
        if (string.IsNullOrEmpty(email))
        {
            throw new BadRequestException("OTP verification not complete or expired.");
        }

        var user = await _userRepository.GetUserByEmailAsync(email);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new BadRequestException("Password reset is not available for Social Provider accounts.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _userRepository.UpdateUserAsync(user);
        await _otpService.ClearOtpAsync(dto.VerificationId);

        return true;
    }

    public async Task<bool> LogoutAsync(RefreshTokenRequestDto dto)
    {
        var existingToken = await _userRepository.GetActiveRefreshTokenAsync(HashToken(dto.RefreshToken));
        if (existingToken == null)
        {
            return true;
        }

        await _userRepository.RevokeRefreshTokenAsync(existingToken);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        if (dto.NewPassword != dto.ConfirmPassword)
        {
            throw new PasswordMismatchException();
        }

        PasswordValidator.ValidateOrThrow(dto.NewPassword);

        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new BadRequestException("Password change is not available for Social Provider accounts.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedException("Current password is incorrect.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _userRepository.UpdateUserAsync(user);

        return true;
    }

    public async Task<bool> DeleteAccountAsync(int userId, DeleteAccountDto dto)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            throw new BadRequestException("Account deletion is not available for Social Provider accounts.");
        }

        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Password is incorrect.");
        }

        // Soft delete the user
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await _userRepository.UpdateUserAsync(user);

        // Revoke all active refresh tokens for this user
        var refreshTokens = await _userRepository.GetAllActiveRefreshTokensForUserAsync(userId);
        foreach (var token in refreshTokens)
        {
            await _userRepository.RevokeRefreshTokenAsync(token);
        }

        return true;
    }

    private async Task<string> CreateAndStoreRefreshTokenAsync(User user)
    {
        var refreshToken = _tokenService.GenerateRefreshToken(user);
        await _userRepository.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = _tokenService.GetRefreshTokenExpiry()
        });

        return refreshToken;
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }
}
