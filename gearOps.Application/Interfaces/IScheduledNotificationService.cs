using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using gearOps.Application.DTOs;

namespace gearOps.Application.Interfaces;

public interface IScheduledNotificationService
{
    /// <summary>Get all scheduled notifications</summary>
    Task<List<ScheduledNotificationDto>> GetAllNotificationsAsync();
    
    /// <summary>Get a specific scheduled notification</summary>
    Task<ScheduledNotificationDto?> GetNotificationByIdAsync(int notificationId);
    
    /// <summary>Create a new scheduled notification</summary>
    Task<ScheduledNotificationDto> CreateNotificationAsync(CreateScheduledNotificationDto dto);
    
    /// <summary>Update an existing scheduled notification</summary>
    Task<ScheduledNotificationDto> UpdateNotificationAsync(int notificationId, CreateScheduledNotificationDto dto);
    
    /// <summary>Delete a scheduled notification</summary>
    Task DeleteNotificationAsync(int notificationId);
    
    /// <summary>Execute low stock notification job</summary>
    Task ExecuteLowStockNotificationJobAsync();
    
    /// <summary>Execute unpaid credits notification job</summary>
    Task ExecuteUnpaidCreditsNotificationJobAsync();
    
    /// <summary>Execute auto cancel appointments job</summary>
    Task ExecuteAutoCancelAppointmentsJobAsync();
}
