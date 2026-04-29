using System;
using System.Threading.Tasks;
using gearOps.Application.Interfaces;

namespace gearOps.Infrastructure.Services;

public class OtpDetails
{
    public string Email { get; set; } = null!;
    public string Otp { get; set; } = null!;
    public bool Verified { get; set; } = false;
}

public class OtpService : IOtpService
{
    private readonly IRedisService _redisService;

    public OtpService(IRedisService redisService)
    {
        _redisService = redisService;
    }

    public async Task<string> GenerateAndStoreOtpAsync(string email)
    {
        var verificationId = Guid.NewGuid().ToString();
        var otp = new Random().Next(100000, 999999).ToString();
        
        var otpDetails = new OtpDetails { Email = email, Otp = otp, Verified = false };
        await _redisService.SetAsync(verificationId, otpDetails, TimeSpan.FromMinutes(5));
        
        return verificationId;
    }

    public async Task<bool> VerifyOtpAsync(string verificationId, string otp)
    {
        var details = await _redisService.GetAsync<OtpDetails>(verificationId);
        if (details == null || details.Otp != otp) return false;

        details.Verified = true;
        await _redisService.SetAsync(verificationId, details, TimeSpan.FromMinutes(20)); // Extend TTL
        return true;
    }

    public async Task<string?> GetVerifiedEmailAsync(string verificationId)
    {
        var details = await _redisService.GetAsync<OtpDetails>(verificationId);
        if (details != null && details.Verified)
        {
            return details.Email;
        }
        return null;
    }

    public async Task ClearOtpAsync(string verificationId)
    {
        await _redisService.RemoveAsync(verificationId);
    }
}
