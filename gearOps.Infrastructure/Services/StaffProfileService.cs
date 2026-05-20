using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;
using gearOps.Domain.Enums;
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
        // staffId is the User ID (from JWT token)
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == staffId && u.Role == Role.Staff)
            ?? throw new NotFoundException($"Staff user with ID {staffId} not found.");

        // Get Staff entity linked via UserId
        var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == staffId);

        return MapToDto(user, staff);
    }

    public async Task<StaffProfileResponseDto> UpdateProfileAsync(int staffId, UpdateStaffProfileDto dto)
    {
        try
        {
            // staffId is the User ID (from JWT token)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == staffId && u.Role == Role.Staff)
                ?? throw new NotFoundException($"Staff user with ID {staffId} not found.");

            // Check phone uniqueness (exclude current staff)
            var phoneExists = await _context.Users
                .Where(u => u.Phone == dto.Phone && u.UserId != staffId && u.Role == Role.Staff)
                .FirstOrDefaultAsync();
            if (phoneExists != null)
                throw new ConflictException($"Phone '{dto.Phone}' is already in use by another staff member.");

            user.FullName = dto.FullName;
            user.Phone = dto.Phone;
            user.Address = dto.Address;
            user.ProfileImageUrl = dto.ProfileImageUrl;

            // Staff entity read for display purposes (position, join date, etc.)
            var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == staffId);

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Staff profile updated: {staffId}");

            return MapToDto(user, staff);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating staff profile");
            throw;
        }
    }

    private StaffProfileResponseDto MapToDto(User user, Staff? staff) => new()
    {
        UserId = user.UserId,
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone,
        Address = user.Address,
        ProfileImageUrl = user.ProfileImageUrl,
        Position = staff?.Position ?? "Staff",
        Status = (staff?.IsActive ?? true) ? "Active" : "Inactive",
        JoinDate = staff?.JoinDate ?? user.CreatedAt,
        EmailSubscribed = true
    };
}
