using Microsoft.AspNetCore.Mvc;
using gearOps.Application.DTOs;
using gearOps.Application.Interfaces;
using gearOps.Application.Exceptions;
using System.Threading.Tasks;

namespace gearOps.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("request-otp")]
    public async Task<IActionResult> RequestRegistrationOtp([FromBody] RequestOtpDto dto)
    {
        var verificationId = await _authService.RequestRegistrationOtpAsync(dto);
        return Ok(new { verificationId });
    }

    [HttpPost("verify-otp")]
    public async Task<IActionResult> VerifyRegistrationOtp([FromBody] VerifyOtpDto dto)
    {
        var isValid = await _authService.VerifyRegistrationOtpAsync(dto);
        if (!isValid) return BadRequest("Invalid OTP");
        return Ok(new { success = true });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var response = await _authService.FinalizeRegistrationAsync(dto);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var response = await _authService.LoginAsync(dto);
        return Ok(response);
    }
}
