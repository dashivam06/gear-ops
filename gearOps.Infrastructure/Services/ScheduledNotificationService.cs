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

namespace gearOps.Infrastructure.Services;

public class ScheduledNotificationService : IScheduledNotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ScheduledNotificationService> _logger;
    private readonly IEmailService _emailService;

    public ScheduledNotificationService(AppDbContext db, ILogger<ScheduledNotificationService> logger, IEmailService emailService)
    {
        _db = db;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<List<ScheduledNotificationDto>> GetAllNotificationsAsync()
    {
        var notifications = await _db.ScheduledNotifications
            .Where(n => !n.IsDeleted)
            .OrderBy(n => n.NotificationType)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<ScheduledNotificationDto?> GetNotificationByIdAsync(int notificationId)
    {
        var notification = await _db.ScheduledNotifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && !n.IsDeleted);

        return notification == null ? null : MapToDto(notification);
    }

    public async Task<ScheduledNotificationDto> CreateNotificationAsync(CreateScheduledNotificationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NotificationType))
            throw new BadRequestException("NotificationType is required.");

        if (!new[] { "LowStock", "UnpaidCredits" }.Contains(dto.NotificationType))
            throw new BadRequestException("NotificationType must be 'LowStock' or 'UnpaidCredits'.");

        var notification = new ScheduledNotification
        {
            NotificationType = dto.NotificationType,
            IsEnabled = dto.IsEnabled,
            ScheduleTime = dto.ScheduleTime ?? "00:00",
            LowStockThreshold = dto.LowStockThreshold ?? 10,
            OverdueDays = dto.OverdueDays ?? 30,
            AdminEmail = dto.AdminEmail,
            CreatedAt = DateTime.UtcNow
        };

        _db.ScheduledNotifications.Add(notification);
        await _db.SaveChangesAsync();

        _logger.LogInformation($"Created scheduled notification: {dto.NotificationType}");
        return MapToDto(notification);
    }

    public async Task<ScheduledNotificationDto> UpdateNotificationAsync(int notificationId, CreateScheduledNotificationDto dto)
    {
        var notification = await _db.ScheduledNotifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && !n.IsDeleted)
            ?? throw new NotFoundException("Scheduled notification not found.");

        notification.IsEnabled = dto.IsEnabled;
        notification.ScheduleTime = dto.ScheduleTime ?? notification.ScheduleTime;
        notification.LowStockThreshold = dto.LowStockThreshold ?? notification.LowStockThreshold;
        notification.OverdueDays = dto.OverdueDays ?? notification.OverdueDays;
        notification.AdminEmail = dto.AdminEmail ?? notification.AdminEmail;
        notification.UpdatedAt = DateTime.UtcNow;

        _db.ScheduledNotifications.Update(notification);
        await _db.SaveChangesAsync();

        _logger.LogInformation($"Updated scheduled notification: {notificationId}");
        return MapToDto(notification);
    }

    public async Task DeleteNotificationAsync(int notificationId)
    {
        var notification = await _db.ScheduledNotifications
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && !n.IsDeleted)
            ?? throw new NotFoundException("Scheduled notification not found.");

        notification.IsDeleted = true;
        _db.ScheduledNotifications.Update(notification);
        await _db.SaveChangesAsync();

        _logger.LogInformation($"Deleted scheduled notification: {notificationId}");
    }

    public async Task ExecuteLowStockNotificationJobAsync()
    {
        try
        {
            var config = await _db.ScheduledNotifications
                .FirstOrDefaultAsync(n => n.NotificationType == "LowStock" && n.IsEnabled && !n.IsDeleted);

            if (config == null)
            {
                _logger.LogWarning("Low stock notification config not found or disabled.");
                return;
            }

            var threshold = config.LowStockThreshold;
            var lowStockParts = await _db.Parts
                .Where(p => p.StockQuantity < threshold && !p.IsDeleted)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();

            if (lowStockParts.Count == 0)
            {
                _logger.LogInformation("No low stock items found.");
                config.LastRunAt = DateTime.UtcNow;
                config.LastRunStatus = "Success - No low stock items";
                _db.ScheduledNotifications.Update(config);
                await _db.SaveChangesAsync();
                return;
            }

            var adminEmail = config.AdminEmail ?? "admin@gearops.com";
            var alertSummary = string.Join("\n", lowStockParts.Select(p =>
                $"- {p.PartName}: {p.StockQuantity} units (threshold: {threshold})"));

            var emailBody = $@"
Dear Admin,

The following parts have fallen below the low stock threshold of {threshold} units:

{alertSummary}

Please reorder these parts as soon as possible.

Best regards,
GearOps System
";

            await _emailService.SendEmailAsync(adminEmail, "Low Stock Alert", emailBody);

            config.LastRunAt = DateTime.UtcNow;
            config.LastRunStatus = $"Success - {lowStockParts.Count} parts below threshold";
            _db.ScheduledNotifications.Update(config);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Low stock notification sent. {lowStockParts.Count} parts below threshold.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing low stock notification job.");
            
            var config = await _db.ScheduledNotifications
                .FirstOrDefaultAsync(n => n.NotificationType == "LowStock");
            if (config != null)
            {
                config.LastRunAt = DateTime.UtcNow;
                config.LastRunStatus = $"Error: {ex.Message}";
                _db.ScheduledNotifications.Update(config);
                await _db.SaveChangesAsync();
            }
        }
    }

    public async Task ExecuteUnpaidCreditsNotificationJobAsync()
    {
        try
        {
            var config = await _db.ScheduledNotifications
                .FirstOrDefaultAsync(n => n.NotificationType == "UnpaidCredits" && n.IsEnabled && !n.IsDeleted);

            if (config == null)
            {
                _logger.LogWarning("Unpaid credits notification config not found or disabled.");
                return;
            }

            var overdueDays = config.OverdueDays;
            var overdueThreshold = DateTime.UtcNow.AddDays(-overdueDays);

            var customersWithOverdueCredits = await _db.Users
                .Include(u => u.CustomerInvoices)
                .Where(u => u.Role == Role.Customer && 
                            u.CreditsRemaining < 0 && 
                            u.CustomerInvoices.Any(i => 
                                !i.IsPaid && 
                                i.DueDate.HasValue && 
                                i.DueDate.Value < overdueThreshold) &&
                            !u.IsDeleted)
                .ToListAsync();

            if (customersWithOverdueCredits.Count == 0)
            {
                _logger.LogInformation("No customers with overdue credits found.");
                config.LastRunAt = DateTime.UtcNow;
                config.LastRunStatus = "Success - No overdue credits";
                _db.ScheduledNotifications.Update(config);
                await _db.SaveChangesAsync();
                return;
            }

            foreach (var customer in customersWithOverdueCredits)
            {
                var emailBody = $@"
Dear {customer.FullName},

This is a friendly reminder that you have an outstanding credit balance of {Math.Abs(customer.CreditsRemaining):C} that is now {overdueDays} or more days overdue.

Please settle this balance at your earliest convenience to avoid any service disruption.

If you have already made a payment, please disregard this notice.

Best regards,
GearOps Team
";

                try
                {
                    await _emailService.SendEmailAsync(customer.Email, "Payment Reminder - Overdue Credits", emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send email to customer {customer.UserId}");
                }
            }

            config.LastRunAt = DateTime.UtcNow;
            config.LastRunStatus = $"Success - {customersWithOverdueCredits.Count} reminders sent";
            _db.ScheduledNotifications.Update(config);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Unpaid credits reminders sent to {customersWithOverdueCredits.Count} customers.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing unpaid credits notification job.");
            
            var config = await _db.ScheduledNotifications
                .FirstOrDefaultAsync(n => n.NotificationType == "UnpaidCredits");
            if (config != null)
            {
                config.LastRunAt = DateTime.UtcNow;
                config.LastRunStatus = $"Error: {ex.Message}";
                _db.ScheduledNotifications.Update(config);
                await _db.SaveChangesAsync();
            }
        }
    }

    public async Task ExecuteAutoCancelAppointmentsJobAsync()
    {
        try
        {
            var now = DateTime.UtcNow;
            var pastDueThreshold = now.AddDays(-1); // Appointments older than yesterday

            var missedAppointments = await _db.Appointments
                .Where(a => (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Pending) 
                         && a.RequestedDate < pastDueThreshold)
                .ToListAsync();

            if (missedAppointments.Count == 0)
            {
                _logger.LogInformation("No missed appointments found to auto-cancel.");
                return;
            }

            foreach (var appointment in missedAppointments)
            {
                appointment.Status = AppointmentStatus.Cancelled;
                appointment.ApprovalNotes = "Auto-cancelled: Customer did not show up.";
            }

            _db.Appointments.UpdateRange(missedAppointments);
            await _db.SaveChangesAsync();

            _logger.LogInformation($"Auto-cancelled {missedAppointments.Count} missed appointments.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing auto cancel appointments job.");
        }
    }

    private static ScheduledNotificationDto MapToDto(ScheduledNotification notification) => new()
    {
        NotificationId = notification.NotificationId,
        NotificationType = notification.NotificationType,
        IsEnabled = notification.IsEnabled,
        ScheduleTime = notification.ScheduleTime,
        LowStockThreshold = notification.LowStockThreshold,
        OverdueDays = notification.OverdueDays,
        AdminEmail = notification.AdminEmail,
        CreatedAt = notification.CreatedAt,
        LastRunAt = notification.LastRunAt,
        LastRunStatus = notification.LastRunStatus
    };
}
