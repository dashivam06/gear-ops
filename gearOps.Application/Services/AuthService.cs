using System;
using System.Threading.Tasks;
using BCrypt.Net;
using gearOps.Application.DTOs;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;
using gearOps.Application.Exceptions;

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
        _ = _emailService.SendOtpEmailAsync(dto.Email, verificationId); 
        
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
            throw new BadRequestException("Passwords do not match.");
        }

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
            ProfileImageUrl = dto.ProfileImageUrl
        };

        var createdUser = await _userRepository.AddUserAsync(user);
        await _otpService.ClearOtpAsync(dto.VerificationId);

        var accessToken = _tokenService.GenerateAccessToken(createdUser);
        var refreshToken = _tokenService.GenerateRefreshToken(createdUser);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = DateTime.UtcNow.AddMinutes(60),
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
        var refreshToken = _tokenService.GenerateRefreshToken(user);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = DateTime.UtcNow.AddMinutes(60),
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
        _ = _emailService.SendOtpEmailAsync(dto.Email, verificationId);

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
            throw new BadRequestException("Passwords do not match.");
        }

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

    public Task<bool> LogoutAsync()
    {
        return Task.FromResult(true);
    }
}
