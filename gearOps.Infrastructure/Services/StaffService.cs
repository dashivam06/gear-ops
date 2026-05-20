using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Interfaces;
using gearOps.Application.Helpers;
using gearOps.Domain.Entities;
using gearOps.Domain.Enums;
using gearOps.Infrastructure.Data;

namespace gearOps.Infrastructure.Services;

public class StaffService : IStaffService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StaffService> _logger;
    private readonly IEmailService _emailService;

    public StaffService(AppDbContext context, ILogger<StaffService> logger, IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<CreateStaffResponseDto> CreateStaffAsync(CreateStaffDto dto)
    {
        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var existingUserEmail = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == dto.Email && !u.IsDeleted);

            if (existingUserEmail != null)
                throw new ConflictException($"Email '{dto.Email}' is already in use.");

            var existingUserPhone = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == dto.Phone && !u.IsDeleted);

            if (existingUserPhone != null)
                throw new ConflictException($"Phone '{dto.Phone}' is already in use.");

            var temporaryPassword = GenerateTemporaryPassword();
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);

            var staffUser = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                Role = Role.Staff,
                PasswordHash = passwordHash,
                ProfileImageUrl = dto.ProfileImageUrl,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(staffUser);
            await _context.SaveChangesAsync();

            var staff = new Staff
            {
                UserId = staffUser.UserId,
                Position = dto.Position,
                JoinDate = DateTime.UtcNow,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            await _emailService.SendStaffOnboardingEmailAsync(
                staffUser.Email,
                staffUser.FullName,
                staff.Position,
                temporaryPassword);

            await transaction.CommitAsync();

            _logger.LogInformation("Staff member created: {StaffId} (User ID: {UserId}, Email: {Email})", staff.StaffId, staffUser.UserId, staffUser.Email);

            return new CreateStaffResponseDto
            {
                StaffId = staff.StaffId,
                FullName = staffUser.FullName,
                Email = staffUser.Email,
                Phone = staffUser.Phone,
                Address = staffUser.Address,
                Position = staff.Position,
                ProfileImageUrl = staffUser.ProfileImageUrl,
                JoinDate = staff.JoinDate,
                IsActive = staff.IsActive,
                CreatedAt = staff.CreatedAt,
                OnboardingEmailSent = true,
                Message = "Staff member created successfully. A temporary password has been sent to the staff email address."
            };
        }
        catch (ConflictException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await _context.Database.RollbackTransactionAsync();
            _logger.LogError(ex, "Error creating staff member");
            throw new BadRequestException("Staff member could not be created. Please verify the details and try again.");
        }
    }

    public async Task<StaffResponseDto> UpdateStaffAsync(UpdateStaffDto dto)
    {
        try
        {
            var staff = await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffId == dto.StaffId && !s.User.IsDeleted)
                ?? throw new NotFoundException($"Staff member with ID {dto.StaffId} not found.");

            var staffUser = staff.User;

            // Check email uniqueness (exclude current user)
            var emailExists = await _context.Users
                .Where(u => u.Email == dto.Email && u.UserId != staffUser.UserId)
                .Where(u => !u.IsDeleted)
                .FirstOrDefaultAsync();
            if (emailExists != null)
                throw new ConflictException($"Email '{dto.Email}' is already in use by another staff member.");

            // Check phone uniqueness (exclude current user)
            var phoneExists = await _context.Users
                .Where(u => u.Phone == dto.Phone && u.UserId != staffUser.UserId)
                .Where(u => !u.IsDeleted)
                .FirstOrDefaultAsync();
            if (phoneExists != null)
                throw new ConflictException($"Phone '{dto.Phone}' is already in use by another staff member.");

            // Update User record
            staffUser.FullName = dto.FullName;
            staffUser.Email = dto.Email;
            staffUser.Phone = dto.Phone;
            staffUser.Address = dto.Address;
            staffUser.ProfileImageUrl = dto.ProfileImageUrl;

            // Update Staff record
            staff.Position = dto.Position;
            staff.IsActive = dto.IsActive;

            _context.Users.Update(staffUser);
            _context.Staff.Update(staff);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Staff member updated: {staff.StaffId} (User ID: {staffUser.UserId})");

            return MapToDto(staff, staffUser);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating staff member");
            throw;
        }
    }

    public async Task<StaffResponseDto?> GetStaffByIdAsync(int staffId)
    {
        var staff = await _context.Staff
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.StaffId == staffId && !s.User.IsDeleted);

        return staff == null ? null : MapToDto(staff, staff.User);
    }

    public async Task<List<StaffResponseDto>> GetAllStaffAsync()
    {
        var staffList = await _context.Staff
            .Include(s => s.User)
            .Where(s => !s.User.IsDeleted)
            .ToListAsync();
        return staffList.Select(s => MapToDto(s, s.User)).ToList();
    }

    public async Task<PagedResult<StaffResponseDto>> GetAllStaffAsync(PaginationParams paging)
    {
        var query = _context.Staff
            .Include(s => s.User)
            .Where(s => !s.User.IsDeleted);

        if (!string.IsNullOrWhiteSpace(paging.Search))
        {
            var search = paging.Search.ToLower();
            query = query.Where(s => 
                s.User.FullName.ToLower().Contains(search) ||
                s.User.Email.ToLower().Contains(search) ||
                (s.User.Phone != null && s.User.Phone.ToLower().Contains(search)) ||
                s.Position.ToLower().Contains(search)
            );
        }

        query = query.OrderByDescending(s => s.CreatedAt);

        var totalItems = await query.CountAsync();
        var staff = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync();

        return new PagedResult<StaffResponseDto>
        {
            Items = staff.Select(s => MapToDto(s, s.User)).ToList(),
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)paging.PageSize)
        };
    }

    public async Task<bool> DeleteStaffAsync(int staffId)
    {
        try
        {
            var staff = await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffId == staffId && !s.User.IsDeleted)
                ?? throw new NotFoundException($"Staff member with ID {staffId} not found.");

            var now = DateTime.UtcNow;
            staff.IsActive = false;
            staff.User.IsDeleted = true;
            staff.User.DeletedAt = now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Staff member soft deleted: {StaffId} (User ID: {UserId})", staffId, staff.UserId);
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error deleting staff member");
            throw;
        }
    }

    public async Task<bool> ToggleStaffStatusAsync(int staffId, bool isActive)
    {
        try
        {
            var staff = await _context.Staff
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StaffId == staffId && !s.User.IsDeleted)
                ?? throw new NotFoundException($"Staff member with ID {staffId} not found.");

            staff.IsActive = isActive;
            _context.Staff.Update(staff);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Staff member status updated: {staffId} - IsActive: {isActive}");
            return true;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating staff status");
            throw;
        }
    }

    private static StaffResponseDto MapToDto(Staff staff, User user) => new()
    {
        StaffId = staff.StaffId,
        FullName = user.FullName,
        Email = user.Email,
        Phone = user.Phone,
        Address = user.Address,
        Position = staff.Position,
        ProfileImageUrl = user.ProfileImageUrl,
        JoinDate = staff.JoinDate,
        IsActive = staff.IsActive,
        CreatedAt = staff.CreatedAt
    };

    private static string GenerateTemporaryPassword()
    {
        const int length = 14;
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string symbols = "!@#$%^&*()-_=+?";
        var allCharacters = upper + lower + digits + symbols;

        var password = new char[length];
        password[0] = GetRandomCharacter(upper);
        password[1] = GetRandomCharacter(lower);
        password[2] = GetRandomCharacter(digits);
        password[3] = GetRandomCharacter(symbols);

        for (var i = 4; i < length; i++)
        {
            password[i] = GetRandomCharacter(allCharacters);
        }

        for (var i = password.Length - 1; i > 0; i--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[swapIndex]) = (password[swapIndex], password[i]);
        }

        return new string(password);
    }

    private static char GetRandomCharacter(string source)
    {
        var index = RandomNumberGenerator.GetInt32(source.Length);
        return source[index];
    }
}
