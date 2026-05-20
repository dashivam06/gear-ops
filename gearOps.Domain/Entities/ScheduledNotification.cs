using System;

namespace gearOps.Domain.Entities;

/// <summary>Represents a scheduled notification configuration</summary>
public class ScheduledNotification
{
    public int NotificationId { get; set; }
    
    /// <summary>Type: "LowStock" or "UnpaidCredits"</summary>
    public string NotificationType { get; set; } = null!;
    
    /// <summary>Is this notification enabled?</summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>Schedule time in HH:mm format (e.g., "00:00" for midnight)</summary>
    public string? ScheduleTime { get; set; }
    
    /// <summary>Stock level threshold for low stock alerts</summary>
    public int LowStockThreshold { get; set; } = 10;
    
    /// <summary>Days overdue before sending unpaid credit reminders</summary>
    public int OverdueDays { get; set; } = 30;
    
    /// <summary>Admin email for notifications</summary>
    public string? AdminEmail { get; set; }
    
    /// <summary>When this notification was last run</summary>
    public DateTime? LastRunAt { get; set; }
    
    /// <summary>Status of the last run</summary>
    public string? LastRunStatus { get; set; }
    
    /// <summary>When this config was created</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>When this config was last updated</summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>Soft delete flag</summary>
    public bool IsDeleted { get; set; } = false;
}
