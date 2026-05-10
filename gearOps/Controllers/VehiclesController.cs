using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using gearOps.Application.DTOs;
using gearOps.Application.Interfaces;
using System.Threading.Tasks;
using System.Security.Claims;
using System;

namespace gearOps.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    private int GetCurrentUserId()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdStr, out int userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException();
    }

    [HttpGet]
    public async Task<IActionResult> GetMyVehicles()
    {
        try {
            var userId = GetCurrentUserId();
            var vehicles = await _vehicleService.GetCustomerVehiclesAsync(userId);
            return Ok(vehicles);
        } catch (UnauthorizedAccessException) {
            return Unauthorized();
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVehicle(int id)
    {
        try {
            var userId = GetCurrentUserId();
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id, userId);
            return Ok(vehicle);
        } catch (UnauthorizedAccessException) {
            return Unauthorized();
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddVehicle([FromBody] CreateVehicleDto dto)
    {
        try {
            var userId = GetCurrentUserId();
            var vehicle = await _vehicleService.AddVehicleAsync(userId, dto);
            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.VehicleId }, vehicle);
        } catch (UnauthorizedAccessException) {
            return Unauthorized();
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] UpdateVehicleDto dto)
    {
        try {
            var userId = GetCurrentUserId();
            await _vehicleService.UpdateVehicleAsync(id, userId, dto);
            return NoContent();
        } catch (UnauthorizedAccessException) {
            return Unauthorized();
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVehicle(int id)
    {
        try {
            var userId = GetCurrentUserId();
            await _vehicleService.DeleteVehicleAsync(id, userId);
            return NoContent();
        } catch (UnauthorizedAccessException) {
            return Unauthorized();
        }
    }
}
