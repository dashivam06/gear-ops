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

public class StaffScheduleService : IStaffScheduleService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StaffScheduleService> _logger;
    private readonly IEmailService _emailService;

    public StaffScheduleService(AppDbContext context, ILogger<StaffScheduleService> logger, IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<List<StaffAppointmentDto>> GetTodayAppointmentsAsync(int staffId)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var appointments = await _context.Appointments
            .Where(a => a.RequestedDate >= today && a.RequestedDate < tomorrow && a.Status == AppointmentStatus.Pending)
            .Include(a => a.Vehicle)
            .Include(a => a.Customer)
            .ToListAsync();

        return appointments.Select(a => MapToDto(a)).ToList();
    }

    public async Task<List<StaffAppointmentDto>> GetUpcomingAppointmentsAsync(int staffId)
    {
        var tomorrow = DateTime.UtcNow.AddDays(1);

        var appointments = await _context.Appointments
            .Where(a => a.RequestedDate >= tomorrow && a.Status == AppointmentStatus.Pending)
            .Include(a => a.Vehicle)
            .Include(a => a.Customer)
            .OrderBy(a => a.RequestedDate)
            .ToListAsync();

        return appointments.Select(a => MapToDto(a)).ToList();
    }

    public async Task<List<StaffAppointmentDto>> GetAllAppointmentsAsync(int staffId)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Vehicle)
            .Include(a => a.Customer)
            .OrderByDescending(a => a.RequestedDate)
            .ToListAsync();

        return appointments.Select(a => MapToDto(a)).ToList();
    }

    public async Task<PagedResult<StaffAppointmentDto>> GetAllAppointmentsPagedAsync(int staffId, PaginationParams paging)
    {
        var query = _context.Appointments
            .Include(a => a.Vehicle)
            .Include(a => a.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(paging.Search))
        {
            var search = paging.Search.ToLower();
            query = query.Where(a =>
                (a.Customer.FullName != null && a.Customer.FullName.ToLower().Contains(search)) ||
                (a.Vehicle.VehicleNumber != null && a.Vehicle.VehicleNumber.ToLower().Contains(search))
            );
        }

        query = query.OrderByDescending(a => a.RequestedDate);
        var paged = await query.ToPagedResultAsync(paging);
        
        return new PagedResult<StaffAppointmentDto>
        {
            Items = paged.Items.Select(MapToDto).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
            TotalPages = paged.TotalPages
        };
    }

    public async Task<StaffAppointmentDto?> GetAppointmentByIdAsync(int appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Vehicle)
            .Include(a => a.Customer)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

        return appointment == null ? null : MapToDto(appointment);
    }

    public async Task<StaffScheduleSummaryDto> GetScheduleSummaryAsync(int staffId)
    {
        // staffId is the User ID from JWT token
        var staffUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == staffId && u.Role == Role.Staff)
            ?? throw new NotFoundException($"Staff user with ID {staffId} not found.");

        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayAppointments = await _context.Appointments
            .Where(a => a.RequestedDate >= today && a.RequestedDate < tomorrow && a.Status == AppointmentStatus.Pending)
            .Include(a => a.Vehicle)
            .Include(a => a.Customer)
            .ToListAsync();

        var upcomingAppointments = await _context.Appointments
            .Where(a => a.RequestedDate >= tomorrow && a.Status == AppointmentStatus.Pending)
            .Include(a => a.Vehicle)
            .Include(a => a.Customer)
            .OrderBy(a => a.RequestedDate)
            .Take(10)
            .ToListAsync();

        var completedThisMonth = await _context.ServiceRecords
            .Where(sr => sr.StaffId == staffId &&
                         sr.ServiceDate.Year == DateTime.UtcNow.Year &&
                         sr.ServiceDate.Month == DateTime.UtcNow.Month)
            .CountAsync();

        _logger.LogInformation($"Schedule summary retrieved for staff {staffId}");

        return new StaffScheduleSummaryDto
        {
            StaffId = staffId,
            StaffName = staffUser.FullName,
            TodayAppointments = todayAppointments.Count,
            UpcomingAppointments = upcomingAppointments.Count,
            CompletedAppointmentsThisMonth = completedThisMonth,
            TodaySchedule = todayAppointments.Select(MapToDto).ToList(),
            UpcomingSchedule = upcomingAppointments.Select(MapToDto).ToList()
        };
    }

    public async Task<AvailableTimeSlotsResponseDto> GetAvailableTimeSlotsAsync(DateTime date)
    {
        var utcDate = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

        // Define business hours: 10 AM - 5 PM, 1-hour intervals, break at 1-2 PM
        var timeSlots = new List<TimeSlotDto>
        {
            new() { SlotNumber = 1, DisplayTime = "10:00 AM - 11:00 AM", StartTime = utcDate.AddHours(10), EndTime = utcDate.AddHours(11) },
            new() { SlotNumber = 2, DisplayTime = "11:00 AM - 12:00 PM", StartTime = utcDate.AddHours(11), EndTime = utcDate.AddHours(12) },
            new() { SlotNumber = 3, DisplayTime = "12:00 PM - 1:00 PM", StartTime = utcDate.AddHours(12), EndTime = utcDate.AddHours(13) },
            new() { SlotNumber = 4, DisplayTime = "1:00 PM - 2:00 PM", StartTime = utcDate.AddHours(13), EndTime = utcDate.AddHours(14), IsBreak = true },
            new() { SlotNumber = 5, DisplayTime = "2:00 PM - 3:00 PM", StartTime = utcDate.AddHours(14), EndTime = utcDate.AddHours(15) },
            new() { SlotNumber = 6, DisplayTime = "3:00 PM - 4:00 PM", StartTime = utcDate.AddHours(15), EndTime = utcDate.AddHours(16) },
            new() { SlotNumber = 7, DisplayTime = "4:00 PM - 5:00 PM", StartTime = utcDate.AddHours(16), EndTime = utcDate.AddHours(17) }
        };

        var dayStart = utcDate;
        var dayEnd = utcDate.AddDays(1);

        // Get booked appointments for this date
        var bookedAppointments = await _context.Appointments
            .Where(a => a.RequestedDate >= dayStart && a.RequestedDate < dayEnd && a.Status != AppointmentStatus.Cancelled)
            .ToListAsync();

        // Mark booked slots
        foreach (var slot in timeSlots)
        {
            if (slot.IsBreak)
                continue;

            slot.IsBooked = bookedAppointments.Any(a =>
                a.RequestedDate >= slot.StartTime && a.RequestedDate < slot.EndTime);
        }

        var availableSlots = timeSlots.Count(s => !s.IsBooked && !s.IsBreak);
        var bookedSlots = timeSlots.Count(s => s.IsBooked);

        return new AvailableTimeSlotsResponseDto
        {
            Date = utcDate,
            TimeSlots = timeSlots,
            TotalAvailableSlots = availableSlots,
            TotalBookedSlots = bookedSlots
        };
    }

    public async Task<AppointmentDecisionResponseDto> ApproveAppointmentAsync(int staffId, int appointmentId, string? notes)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Vehicle)
            .Include(a => a.ApprovedByStaff)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId)
            ?? throw new NotFoundException($"Appointment with ID {appointmentId} not found.");

        if (appointment.Status != AppointmentStatus.Pending)
            throw new BadRequestException($"Only pending appointments can be approved. Current status: {appointment.Status}");

        var staff = await _context.Users.FindAsync(staffId)
            ?? throw new NotFoundException($"Staff with ID {staffId} not found.");

        appointment.Status = AppointmentStatus.Confirmed;
        appointment.ApprovedByStaffId = staffId;
        appointment.ApprovedAt = DateTime.UtcNow;
        appointment.ApprovalNotes = notes;

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        // Send email notification to customer
        await _emailService.SendAppointmentApprovedEmailAsync(
            appointment.Customer.Email,
            appointment.Customer.FullName,
            appointment.Vehicle.VehicleNumber,
            appointment.RequestedDate,
            staff.FullName,
            notes);

        _logger.LogInformation($"Appointment {appointmentId} approved by staff {staffId}");

        return new AppointmentDecisionResponseDto
        {
            AppointmentId = appointmentId,
            Status = "Confirmed",
            Decision = "Approved",
            Notes = notes,
            ApprovedByStaffName = staff.FullName,
            ApprovedAt = DateTime.UtcNow,
            CustomerEmail = appointment.Customer.Email,
            Message = $"Appointment successfully confirmed for {appointment.RequestedDate:dddd, MMMM d, yyyy at h:mm tt}"
        };
    }

    public async Task<AppointmentDecisionResponseDto> RejectAppointmentAsync(int staffId, int appointmentId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new BadRequestException("Rejection reason is required.");

        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Vehicle)
            .Include(a => a.ApprovedByStaff)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId)
            ?? throw new NotFoundException($"Appointment with ID {appointmentId} not found.");

        if (appointment.Status != AppointmentStatus.Pending)
            throw new BadRequestException($"Only pending appointments can be rejected. Current status: {appointment.Status}");

        var staff = await _context.Users.FindAsync(staffId)
            ?? throw new NotFoundException($"Staff with ID {staffId} not found.");

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.ApprovedByStaffId = staffId;
        appointment.ApprovedAt = DateTime.UtcNow;
        appointment.ApprovalNotes = reason;

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        // Send email notification to customer
        await _emailService.SendAppointmentRejectedEmailAsync(
            appointment.Customer.Email,
            appointment.Customer.FullName,
            appointment.Vehicle.VehicleNumber,
            appointment.RequestedDate,
            staff.FullName,
            reason);

        _logger.LogInformation($"Appointment {appointmentId} rejected by staff {staffId}. Reason: {reason}");

        return new AppointmentDecisionResponseDto
        {
            AppointmentId = appointmentId,
            Status = "Cancelled",
            Decision = "Rejected",
            Notes = reason,
            ApprovedByStaffName = staff.FullName,
            ApprovedAt = DateTime.UtcNow,
            CustomerEmail = appointment.Customer.Email,
            Message = $"Appointment for {appointment.RequestedDate:dddd, MMMM d, yyyy at h:mm tt} has been rejected. Please contact us to reschedule."
        };
    }

    private StaffAppointmentDto MapToDto(Appointment appointment) => new()
    {
        AppointmentId = appointment.AppointmentId,
        CustomerId = appointment.CustomerId,
        VehicleId = appointment.VehicleId,
        VehicleNumber = appointment.Vehicle.VehicleNumber,
        CustomerName = appointment.Customer.FullName,
        CustomerPhone = appointment.Customer.Phone,
        CustomerEmail = appointment.Customer.Email,
        AppointmentDate = appointment.RequestedDate,
        Description = appointment.Remarks ?? "",
        Status = appointment.Status.ToString(),
        ApprovalNotes = appointment.ApprovalNotes,
        ApprovedByStaffName = appointment.ApprovedByStaff?.FullName,
        ApprovedAt = appointment.ApprovedAt,
        CreatedAt = appointment.CreatedAt
    };

    public async Task<AppointmentDecisionResponseDto> MarkAppointmentAsNoShowAsync(int staffId, int appointmentId, string? reason)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId)
            ?? throw new NotFoundException($"Appointment with ID {appointmentId} not found.");

        if (appointment.Status != AppointmentStatus.Confirmed)
            throw new BadRequestException($"Only confirmed appointments can be marked as no-show. Current status: {appointment.Status}");

        var staff = await _context.Users.FindAsync(staffId)
            ?? throw new NotFoundException($"Staff with ID {staffId} not found.");

        appointment.Status = AppointmentStatus.NoShow;
        appointment.ApprovedByStaffId = staffId;
        appointment.ApprovedAt = DateTime.UtcNow;
        appointment.ApprovalNotes = reason ?? "Customer did not show up for appointment.";

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        // Send email notification to customer
        await _emailService.SendAppointmentNoShowEmailAsync(
            appointment.Customer.Email,
            appointment.Customer.FullName,
            appointment.Vehicle.VehicleNumber,
            appointment.RequestedDate,
            staff.FullName,
            reason);

        _logger.LogInformation($"Appointment {appointmentId} marked as no-show by staff {staffId}.");

        return new AppointmentDecisionResponseDto
        {
            AppointmentId = appointmentId,
            Status = "NoShow",
            Decision = "NoShow",
            Notes = reason ?? "No reason provided",
            ApprovedByStaffName = staff.FullName,
            ApprovedAt = DateTime.UtcNow,
            CustomerEmail = appointment.Customer.Email,
            Message = $"Appointment for {appointment.RequestedDate:dddd, MMMM d, yyyy at h:mm tt} marked as no-show."
        };
    }

    public async Task<AppointmentDecisionResponseDto> RescheduleAppointmentAsync(int staffId, int appointmentId, DateTime newDate)
    {
        if (newDate <= DateTime.UtcNow)
            throw new BadRequestException("New appointment date must be in the future.");

        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId)
            ?? throw new NotFoundException($"Appointment with ID {appointmentId} not found.");

        var staff = await _context.Users.FindAsync(staffId)
            ?? throw new NotFoundException($"Staff with ID {staffId} not found.");

        var oldDate = appointment.RequestedDate;
        appointment.RequestedDate = newDate;
        appointment.Status = AppointmentStatus.Pending; // Reset to pending for re-approval

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        // Send email notification to customer
        await _emailService.SendAppointmentRescheduleEmailAsync(
            appointment.Customer.Email,
            appointment.Customer.FullName,
            appointment.Vehicle.VehicleNumber,
            oldDate,
            newDate,
            staff.FullName);

        _logger.LogInformation($"Appointment {appointmentId} rescheduled from {oldDate:yyyy-MM-dd} to {newDate:yyyy-MM-dd} by staff {staffId}.");

        return new AppointmentDecisionResponseDto
        {
            AppointmentId = appointmentId,
            Status = "Pending",
            Decision = "Rescheduled",
            Notes = $"Rescheduled from {oldDate:dddd, MMMM d, yyyy} to {newDate:dddd, MMMM d, yyyy}",
            ApprovedByStaffName = staff.FullName,
            ApprovedAt = DateTime.UtcNow,
            CustomerEmail = appointment.Customer.Email,
            Message = $"Appointment rescheduled from {oldDate:dddd, MMMM d, yyyy at h:mm tt} to {newDate:dddd, MMMM d, yyyy at h:mm tt}. Awaiting approval."
        };
    }

    public async Task<AppointmentDecisionResponseDto> CompleteAppointmentAsync(int staffId, int appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId)
            ?? throw new NotFoundException($"Appointment with ID {appointmentId} not found.");

        if (appointment.Status != AppointmentStatus.Confirmed)
            throw new BadRequestException($"Only confirmed appointments can be marked as completed. Current status: {appointment.Status}");

        var staff = await _context.Users.FindAsync(staffId)
            ?? throw new NotFoundException($"Staff with ID {staffId} not found.");

        appointment.Status = AppointmentStatus.Completed;
        appointment.ApprovedByStaffId = staffId;
        appointment.ApprovedAt = DateTime.UtcNow;

        _context.Appointments.Update(appointment);
        await _context.SaveChangesAsync();

        // Send email notification to customer
        await _emailService.SendAppointmentCompletionEmailAsync(
            appointment.Customer.Email,
            appointment.Customer.FullName,
            appointment.Vehicle.VehicleNumber,
            appointment.RequestedDate,
            staff.FullName);

        _logger.LogInformation($"Appointment {appointmentId} marked as completed by staff {staffId}.");

        return new AppointmentDecisionResponseDto
        {
            AppointmentId = appointmentId,
            Status = "Completed",
            Decision = "Completed",
            Notes = "Service completed successfully",
            ApprovedByStaffName = staff.FullName,
            ApprovedAt = DateTime.UtcNow,
            CustomerEmail = appointment.Customer.Email,
            Message = $"Appointment for {appointment.RequestedDate:dddd, MMMM d, yyyy at h:mm tt} has been completed. Thank you for choosing our service!"
        };
    }
}
