using System;

namespace gearOps.Application.DTOs;

/// <summary>DTO for creating/updating scheduled notifications</summary>
public class CreateScheduledNotificationDto
{
    /// <summary>Type of notification: "LowStock" or "UnpaidCredits"</summary>
    public string NotificationType { get; set; } = null!;
    
    /// <summary>Is this notification enabled?</summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>Time of day to run (HH:mm format, e.g., "00:00" for midnight). Only for daily jobs.</summary>
    public string? ScheduleTime { get; set; }
    
    /// <summary>Threshold for low stock alerts (default: 10)</summary>
    public int? LowStockThreshold { get; set; }
    
    /// <summary>Days overdue for unpaid credit reminders (default: 30 days)</summary>
    public int? OverdueDays { get; set; }
    
    /// <summary>Admin email to notify (for low stock alerts)</summary>
    public string? AdminEmail { get; set; }
}

/// <summary>DTO for scheduled notification response</summary>
public class ScheduledNotificationDto
{
    public int NotificationId { get; set; }
    public string NotificationType { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public string? ScheduleTime { get; set; }
    public int? LowStockThreshold { get; set; }
    public int? OverdueDays { get; set; }
    public string? AdminEmail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public string? LastRunStatus { get; set; }
}

/// <summary>DTO for low stock alert response</summary>
public class LowStockAlertDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = null!;
    public int StockLevel { get; set; }
    public int Threshold { get; set; }
    public DateTime AlertedAt { get; set; }
}

/// <summary>DTO for unpaid credits alert response</summary>
public class UnpaidCreditsAlertDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public decimal CreditsRemaining { get; set; }
    public int DaysOverdue { get; set; }
    public DateTime? LastReminderSentAt { get; set; }
}
