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
using gearOps.Domain.Entities;
using gearOps.Domain.Enums;
using gearOps.Infrastructure.Data;
using gearOps.Infrastructure.Extensions;

namespace gearOps.Infrastructure.Services;

public class StaffCustomerService : IStaffCustomerService
{
    private readonly AppDbContext _db;
    private readonly ILogger<StaffCustomerService> _logger;
    private readonly IEmailService _emailService;

    public StaffCustomerService(AppDbContext db, ILogger<StaffCustomerService> logger, IEmailService emailService)
    {
        _db = db;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<StaffCustomerDto> RegisterCustomerAsync(StaffRegisterCustomerDto dto)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && !u.IsDeleted);
        if (existing != null)
            throw new ConflictException("Email already registered.");

        var temporaryPassword = GenerateTemporaryPassword();
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                ProfileImageUrl = dto.ProfileImageUrl,
                PasswordHash = passwordHash,
                Role = Role.Customer,
                CreatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(dto.VehicleNumber))
            {
                user.Vehicles.Add(new Vehicle
                {
                    VehicleNumber = dto.VehicleNumber,
                    Brand = dto.Brand ?? string.Empty,
                    Model = dto.Model ?? string.Empty,
                    Year = dto.Year ?? DateTime.UtcNow.Year,
                    ImageUrl = dto.VehicleImageUrl,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await _emailService.SendCustomerWelcomeEmailAsync(user.Email, user.FullName, temporaryPassword);
            await transaction.CommitAsync();

            _logger.LogInformation("Staff registered customer {Email} with ID {UserId}", user.Email, user.UserId);

            return MapToDto(user);
        }
        catch
        {
            await _db.Database.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<StaffCustomerDto?> GetCustomerByIdAsync(int customerId)
    {
        var user = await _db.Users
            .Include(u => u.Vehicles)
            .FirstOrDefaultAsync(u => u.UserId == customerId && u.Role == Role.Customer && !u.IsDeleted);

        return user == null ? null : MapToDto(user);
    }

    public async Task<List<StaffCustomerDto>> SearchCustomersAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<StaffCustomerDto>();

        var lowerQuery = query.ToLower();

        var customers = await _db.Users
            .Include(u => u.Vehicles)
            .Where(u => u.Role == Role.Customer && !u.IsDeleted &&
                (u.FullName.ToLower().Contains(lowerQuery) ||
                 u.Email.ToLower().Contains(lowerQuery) ||
                 u.Phone.Contains(query) ||
                 u.UserId.ToString() == query ||
                 u.Vehicles.Any(v => !v.IsDeleted && v.VehicleNumber.ToLower().Contains(lowerQuery))))
            .Take(50)
            .ToListAsync();

        return customers.Select(MapToDto).ToList();
    }

    public async Task<List<StaffCustomerDto>> GetAllCustomersAsync()
    {
        var customers = await _db.Users
            .Include(u => u.Vehicles)
            .Where(u => u.Role == Role.Customer && !u.IsDeleted)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return customers.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<StaffCustomerDto>> GetAllCustomersAsync(PaginationParams paging)
    {
        var query = _db.Users
            .Include(u => u.Vehicles)
            .Where(u => u.Role == Role.Customer && !u.IsDeleted)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => MapToDto(u));
        return await query.ToPagedResultAsync(paging);
    }

    public async Task<StaffCustomerReportDto> GetCustomerReportsAsync()
    {
        var customers = await _db.Users
            .Include(u => u.Vehicles)
            .Include(u => u.CustomerInvoices)
            .Include(u => u.Appointments)
            .Where(u => u.Role == Role.Customer)
            .ToListAsync();

        StaffCustomerReportRowDto MapReportRow(User customer, int? daysOverdue = null)
        {
            var totalSpend = customer.CustomerInvoices.Sum(i => i.FinalAmount);
            var totalPurchases = customer.CustomerInvoices.Count;
            var visitCount = customer.Appointments.Count;

            DateTime? lastInvoice = customer.CustomerInvoices
                .OrderByDescending(i => i.InvoiceDate)
                .Select(i => (DateTime?)i.InvoiceDate)
                .FirstOrDefault();
            DateTime? lastVisit = customer.Appointments
                .OrderByDescending(a => a.RequestedDate)
                .Select(a => (DateTime?)a.RequestedDate)
                .FirstOrDefault();
            var lastActivity = lastInvoice ?? lastVisit;

            return new StaffCustomerReportRowDto
            {
                UserId = customer.UserId,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                TotalSpend = totalSpend,
                TotalPurchases = totalPurchases,
                VisitCount = visitCount,
                CreditsRemaining = customer.CreditsRemaining,
                DaysOverdue = daysOverdue,
                LastActivity = lastActivity
            };
        }

        // Top spenders: customers ordered by total invoice amount
        var topSpenders = customers
            .OrderByDescending(c => c.CustomerInvoices.Sum(i => i.FinalAmount))
            .Take(10)
            .Select(c => MapReportRow(c))
            .ToList();

        // Regular customers: customers with most invoices
        var regulars = customers
            .OrderByDescending(c => c.CustomerInvoices.Count)
            .Take(10)
            .Select(c => MapReportRow(c))
            .ToList();

        // Pending credits: customers with credits > 0
        var overdueThreshold = DateTime.UtcNow.AddDays(-30);
        var pendingCredits = customers
            .Where(c => c.CreditsRemaining > 0)
            .Select(c =>
            {
                var daysOverdue = c.CustomerInvoices
                    .Where(i => !i.IsPaid && i.DueDate.HasValue && i.DueDate.Value < DateTime.UtcNow)
                    .Select(i => (DateTime.UtcNow - i.DueDate!.Value).Days)
                    .DefaultIfEmpty(0)
                    .Max();
                return MapReportRow(c, daysOverdue);
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

    private static string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@$?*-";
        var bytes = RandomNumberGenerator.GetBytes(12);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
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
            ProfileImageUrl = user.ProfileImageUrl,
            CreditsRemaining = user.CreditsRemaining,
            CreatedAt = user.CreatedAt,
            Vehicles = user.Vehicles?.Where(v => !v.IsDeleted).Select(v => new StaffCustomerVehicleDto
            {
                VehicleId = v.VehicleId,
                VehicleNumber = v.VehicleNumber,
                Brand = v.Brand,
                Model = v.Model,
                Year = v.Year,
                ImageUrl = v.ImageUrl
            }).ToList() ?? new()
        };
    }
}
