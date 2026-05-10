using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;
using gearOps.Domain.Enums;
using gearOps.Infrastructure.Data;
using gearOps.Infrastructure.Extensions;

namespace gearOps.Infrastructure.Services;

public class StaffCustomerService : IStaffCustomerService
{
    private readonly AppDbContext _db;
    private readonly ILogger<StaffCustomerService> _logger;

    public StaffCustomerService(AppDbContext db, ILogger<StaffCustomerService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<StaffCustomerDto> RegisterCustomerAsync(StaffRegisterCustomerDto dto)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existing != null)
            throw new ConflictException("Email already registered.");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            ProfileImageUrl = dto.ProfileImageUrl,
            PasswordHash = string.Empty, // Staff-registered customers set password later
            Role = Role.Customer,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Staff registered customer {Email} with ID {UserId}", user.Email, user.UserId);

        return MapToDto(user);
    }

    public async Task<StaffCustomerDto?> GetCustomerByIdAsync(int customerId)
    {
        var user = await _db.Users
            .Include(u => u.Vehicles)
            .FirstOrDefaultAsync(u => u.UserId == customerId && u.Role == Role.Customer);

        return user == null ? null : MapToDto(user);
    }

    public async Task<List<StaffCustomerDto>> SearchCustomersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<StaffCustomerDto>();

        var lowerQuery = query.ToLower();

        var customers = await _db.Users
            .Include(u => u.Vehicles)
            .Where(u => u.Role == Role.Customer &&
                (u.FullName.ToLower().Contains(lowerQuery) ||
                 u.Email.ToLower().Contains(lowerQuery) ||
                 u.Phone.Contains(query) ||
                 u.UserId.ToString() == query ||
                 u.Vehicles.Any(v => v.VehicleNumber.ToLower().Contains(lowerQuery))))
            .Take(50)
            .ToListAsync();

        return customers.Select(MapToDto).ToList();
    }

    public async Task<List<StaffCustomerDto>> GetAllCustomersAsync()
    {
        var customers = await _db.Users
            .Include(u => u.Vehicles)
            .Where(u => u.Role == Role.Customer)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return customers.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<StaffCustomerDto>> GetAllCustomersAsync(PaginationParams paging)
    {
        var query = _db.Users
            .Include(u => u.Vehicles)
            .Where(u => u.Role == Role.Customer)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => MapToDto(u));
        return await query.ToPagedResultAsync(paging);
    }

    public async Task<StaffCustomerReportDto> GetCustomerReportsAsync()
    {
        var customers = await _db.Users
            .Include(u => u.Vehicles)
            .Include(u => u.CustomerInvoices)
            .Where(u => u.Role == Role.Customer)
            .ToListAsync();

        // Top spenders: customers ordered by total invoice amount
        var topSpenders = customers
            .OrderByDescending(c => c.CustomerInvoices.Sum(i => i.FinalAmount))
            .Take(10)
            .Select(MapToDto)
            .ToList();

        // Regular customers: customers with most invoices
        var regulars = customers
            .OrderByDescending(c => c.CustomerInvoices.Count)
            .Take(10)
            .Select(MapToDto)
            .ToList();

        // Pending credits: customers with credits > 0
        var overdueThreshold = DateTime.UtcNow.AddDays(-30);
        var pendingCredits = customers
            .Where(c => c.CreditsRemaining > 0)
            .Select(c => new StaffCustomerCreditDto
            {
                UserId = c.UserId,
                FullName = c.FullName,
                Phone = c.Phone,
                CreditsRemaining = c.CreditsRemaining,
                DaysOverdue = c.CustomerInvoices
                    .Where(i => !i.IsPaid && i.DueDate.HasValue && i.DueDate.Value < DateTime.UtcNow)
                    .Select(i => (DateTime.UtcNow - i.DueDate!.Value).Days)
                    .DefaultIfEmpty(0)
                    .Max()
            })
            .OrderByDescending(c => c.CreditsRemaining)
            .ToList();

        return new StaffCustomerReportDto
        {
            TopSpenders = topSpenders,
            RegularCustomers = regulars,
            PendingCredits = pendingCredits
        };
    }

    private static StaffCustomerDto MapToDto(User user)
    {
        return new StaffCustomerDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Address = user.Address,
            CreditsRemaining = user.CreditsRemaining,
            CreatedAt = user.CreatedAt,
            Vehicles = user.Vehicles?.Select(v => new StaffCustomerVehicleDto
            {
                VehicleId = v.VehicleId,
                VehicleNumber = v.VehicleNumber,
                Brand = v.Brand,
                Model = v.Model,
                Year = v.Year
            }).ToList() ?? new()
        };
    }
}
