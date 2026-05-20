using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Interfaces;
using gearOps.Domain.Enums;
using gearOps.Infrastructure.Data;

namespace gearOps.Infrastructure.Services;

public class StaffReportService : IStaffReportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StaffReportService> _logger;
    private readonly IStaffProfileService _profileService;
    private readonly IStaffScheduleService _scheduleService;

    public StaffReportService(
        AppDbContext context,
        ILogger<StaffReportService> logger,
        IStaffProfileService profileService,
        IStaffScheduleService scheduleService)
    {
        _context = context;
        _logger = logger;
        _profileService = profileService;
        _scheduleService = scheduleService;
    }

    public async Task<StaffPerformanceReportDto> GetPerformanceReportAsync(int staffId)
    {
        // staffId is the User ID from JWT token
        var staffUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == staffId && u.Role == Role.Staff)
            ?? throw new NotFoundException($"Staff user with ID {staffId} not found.");

        var staff = await _context.Staff.FirstOrDefaultAsync(s => s.UserId == staffId);

        var serviceRecords = await _context.ServiceRecords
            .Where(sr => sr.StaffId == staffId)
            .Include(sr => sr.Appointment)
            .ThenInclude(a => a.Reviews)
            .ToListAsync();

        var totalCompletedAppointments = serviceRecords.Count;
        var totalRevenue = serviceRecords.Sum(sr => sr.ServiceCost);
        var averageCost = totalCompletedAppointments > 0 ? totalRevenue / totalCompletedAppointments : 0;

        var allRatings = serviceRecords
            .SelectMany(sr => sr.Appointment.Reviews)
            .Select(r => r.Rating)
            .ToList();
        var averageRating = allRatings.Count > 0 ? allRatings.Average(r => (double)r) : 0;

        var pendingAppointments = await _context.Appointments
            .Where(a => a.Status == AppointmentStatus.Pending)
            .CountAsync();

        _logger.LogInformation($"Performance report generated for staff {staffId}");

        return new StaffPerformanceReportDto
        {
            StaffId = staffId,
            StaffName = staffUser.FullName,
            Position = staff?.Position ?? "Staff",
            TotalAppointmentsCompleted = totalCompletedAppointments,
            TotalServiceRecords = serviceRecords.Count,
            TotalRevenueGenerated = totalRevenue,
            AverageServiceCost = averageCost,
            AverageCustomerRating = averageRating,
            PendingAppointments = pendingAppointments,
            ReportGeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<StaffMonthlyReportDto> GetMonthlyReportAsync(int staffId, int year, int month)
    {
        var serviceRecords = await _context.ServiceRecords
            .Where(sr => sr.StaffId == staffId &&
                         sr.ServiceDate.Year == year &&
                         sr.ServiceDate.Month == month)
            .Include(sr => sr.Appointment)
            .ThenInclude(a => a.Reviews)
            .ToListAsync();

        var totalRevenue = serviceRecords.Sum(sr => sr.ServiceCost);
        var completedServices = serviceRecords.Count;
        var averageCostPerService = completedServices > 0 ? totalRevenue / completedServices : 0;

        var allRatings = serviceRecords
            .SelectMany(sr => sr.Appointment.Reviews)
            .Select(r => r.Rating)
            .ToList();
        var averageRating = allRatings.Count > 0 ? allRatings.Average(r => (double)r) : 0;

        var totalAppointments = await _context.Appointments
            .Where(a => a.RequestedDate.Year == year && a.RequestedDate.Month == month)
            .CountAsync();

        var cancelledAppointments = await _context.Appointments
            .Where(a => a.RequestedDate.Year == year && a.RequestedDate.Month == month &&
                        a.Status == AppointmentStatus.Cancelled)
            .CountAsync();

        _logger.LogInformation($"Monthly report generated for staff {staffId}: {month}/{year}");

        return new StaffMonthlyReportDto
        {
            Year = year,
            Month = month,
            TotalRevenue = totalRevenue,
            CompletedServices = completedServices,
            AverageCostPerService = averageCostPerService,
            AverageCustomerRating = averageRating,
            TotalAppointments = totalAppointments,
            CancelledAppointments = cancelledAppointments
        };
    }

    public async Task<StaffDashboardDto> GetDashboardAsync(int staffId)
    {
        var profile = await _profileService.GetProfileAsync(staffId);
        var scheduleSummary = await _scheduleService.GetScheduleSummaryAsync(staffId);
        var performanceReport = await GetPerformanceReportAsync(staffId);

        _logger.LogInformation($"Dashboard generated for staff {staffId}");

        return new StaffDashboardDto
        {
            Profile = profile,
            ScheduleSummary = scheduleSummary,
            PerformanceMetrics = performanceReport
        };
    }
}
