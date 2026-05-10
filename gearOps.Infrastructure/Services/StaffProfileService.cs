using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;
using gearOps.Infrastructure.Data;

namespace gearOps.Infrastructure.Services;

public class StaffProfileService : IStaffProfileService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StaffProfileService> _logger;

    public StaffProfileService(AppDbContext context, ILogger<StaffProfileService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<StaffProfileResponseDto> GetProfileAsync(int staffId)
    {
        var staff = await _context.Staff.FirstOrDefaultAsync(s => s.StaffId == staffId)
            ?? throw new NotFoundException($"Staff with ID {staffId} not found.");

        return MapToDto(staff);
    }

    public async Task<StaffProfileResponseDto> UpdateProfileAsync(int staffId, UpdateStaffProfileDto dto)
    {
        try
        {
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.StaffId == staffId)
                ?? throw new NotFoundException($"Staff with ID {staffId} not found.");

            // Check phone uniqueness (exclude current staff)
            var phoneExists = await _context.Staff
                .Where(s => s.Phone == dto.Phone && s.StaffId != staffId)
                .FirstOrDefaultAsync();
            if (phoneExists != null)
                throw new ConflictException($"Phone '{dto.Phone}' is already in use by another staff member.");

            staff.FullName = dto.FullName;
            staff.Phone = dto.Phone;
            staff.Address = dto.Address;
            staff.ProfileImageUrl = dto.ProfileImageUrl;

            _context.Staff.Update(staff);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Staff profile updated: {staffId}");

            return MapToDto(staff);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating staff profile");
            throw;
        }
    }

    private StaffProfileResponseDto MapToDto(Staff staff) => new()
    {
        StaffId = staff.StaffId,
        FullName = staff.FullName,
        Email = staff.Email,
        Phone = staff.Phone,
        Address = staff.Address,
        ProfileImageUrl = staff.ProfileImageUrl,
        Position = staff.Position,
        JoinDate = staff.JoinDate,
        IsActive = staff.IsActive,
        CreatedAt = staff.CreatedAt
    };
}
