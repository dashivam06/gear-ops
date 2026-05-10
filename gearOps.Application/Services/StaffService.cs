using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;
using gearOps.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace gearOps.Application.Services;

public class StaffService : IStaffService
{
    private readonly UserManager<User> _userManager;

    public StaffService(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IEnumerable<StaffResponseDto>> GetAllStaffAsync()
    {
        var staffUsers = _userManager.Users
            .Where(u => u.Role == Role.Staff)
            .ToList();

        var staffList = staffUsers.Select(s => new StaffResponseDto
        {
            StaffId = s.Id,
            FullName = s.FullName,
            Email = s.Email ?? string.Empty,
            PhoneNumber = s.PhoneNumber ?? string.Empty,
            Address = s.Address,
            CreatedAt = s.CreatedAt
        });

        return await Task.FromResult(staffList);
    }

    public async Task<StaffResponseDto> GetStaffByIdAsync(int staffId)
    {
        var staff = await _userManager.FindByIdAsync(staffId.ToString());

        if (staff == null || staff.Role != Role.Staff)
            throw new NotFoundException("Staff not found.");

        return new StaffResponseDto
        {
            StaffId = staff.Id,
            FullName = staff.FullName,
            Email = staff.Email ?? string.Empty,
            PhoneNumber = staff.PhoneNumber ?? string.Empty,
            Address = staff.Address,
            CreatedAt = staff.CreatedAt
        };
    }

    public async Task<StaffResponseDto> CreateStaffAsync(CreateStaffDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new ConflictException("Email already exists.");

        var staff = new User
        {
            UserName = dto.Email,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            FullName = dto.FullName,
            Address = dto.Address,
            Role = Role.Staff,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(staff, "TempPassword123!");
        if (!result.Succeeded)
            throw new BadRequestException("Failed to create staff member.");

        return new StaffResponseDto
        {
            StaffId = staff.Id,
            FullName = staff.FullName,
            Email = staff.Email ?? string.Empty,
            PhoneNumber = staff.PhoneNumber ?? string.Empty,
            Address = staff.Address,
            CreatedAt = staff.CreatedAt
        };
    }

    public async Task<bool> UpdateStaffAsync(int staffId, UpdateStaffDto dto)
    {
        var staff = await _userManager.FindByIdAsync(staffId.ToString());

        if (staff == null || staff.Role != Role.Staff)
            throw new NotFoundException("Staff not found.");

        staff.FullName = dto.FullName;
        staff.PhoneNumber = dto.PhoneNumber;
        staff.Address = dto.Address;

        var result = await _userManager.UpdateAsync(staff);
        return result.Succeeded;
    }

    public async Task<bool> DeleteStaffAsync(int staffId)
    {
        var staff = await _userManager.FindByIdAsync(staffId.ToString());

        if (staff == null || staff.Role != Role.Staff)
            throw new NotFoundException("Staff not found.");

        var result = await _userManager.DeleteAsync(staff);
        return result.Succeeded;
    }
}
