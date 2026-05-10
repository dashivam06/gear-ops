using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using gearOps.Application.DTOs;

namespace gearOps.Application.Interfaces;

/// <summary>
/// Service for managing staff profile information
/// </summary>
public interface IStaffProfileService
{
    Task<StaffProfileResponseDto> GetProfileAsync(int staffId);
    Task<StaffProfileResponseDto> UpdateProfileAsync(int staffId, UpdateStaffProfileDto dto);
}

/// <summary>
/// Service for managing staff appointments and schedules
/// </summary>
public interface IStaffScheduleService
{
    Task<List<StaffAppointmentDto>> GetTodayAppointmentsAsync(int staffId);
    Task<List<StaffAppointmentDto>> GetUpcomingAppointmentsAsync(int staffId);
    Task<List<StaffAppointmentDto>> GetAllAppointmentsAsync(int staffId);
    Task<StaffAppointmentDto?> GetAppointmentByIdAsync(int appointmentId);
    Task<StaffScheduleSummaryDto> GetScheduleSummaryAsync(int staffId);
}

/// <summary>
/// Service for recording and managing service records by staff
/// </summary>
public interface IStaffServiceRecordService
{
    Task<StaffServiceRecordDto> CreateServiceRecordAsync(int staffId, CompleteAppointmentDto dto);
    Task<StaffServiceRecordDto> UpdateServiceRecordAsync(int staffId, UpdateServiceRecordDto dto);
    Task<StaffServiceRecordDto?> GetServiceRecordByIdAsync(int serviceRecordId);
    Task<List<StaffServiceRecordDto>> GetStaffServiceRecordsAsync(int staffId);
    Task<List<StaffServiceRecordDto>> GetMonthlyServiceRecordsAsync(int staffId, int year, int month);
}

/// <summary>
/// Service for generating staff performance and monthly reports
/// </summary>
public interface IStaffReportService
{
    Task<StaffPerformanceReportDto> GetPerformanceReportAsync(int staffId);
    Task<StaffMonthlyReportDto> GetMonthlyReportAsync(int staffId, int year, int month);
    Task<StaffDashboardDto> GetDashboardAsync(int staffId);
}

/// <summary>
/// Service for staff-facing customer management operations
/// </summary>
public interface IStaffCustomerService
{
    Task<StaffCustomerDto> RegisterCustomerAsync(StaffRegisterCustomerDto dto);
    Task<StaffCustomerDto?> GetCustomerByIdAsync(int customerId);
    Task<List<StaffCustomerDto>> SearchCustomersAsync(string query);
    Task<List<StaffCustomerDto>> GetAllCustomersAsync();
    Task<PagedResult<StaffCustomerDto>> GetAllCustomersAsync(PaginationParams paging);
    Task<StaffCustomerReportDto> GetCustomerReportsAsync();
}

/// <summary>
/// Service for staff-facing sales invoice operations
/// </summary>
public interface IStaffSalesService
{
    Task<SalesInvoiceResponseDto> CreateSalesInvoiceAsync(int staffId, CreateSalesInvoiceDto dto);
    Task<SalesInvoiceResponseDto?> GetSalesInvoiceByIdAsync(int invoiceId);
    Task<List<SalesInvoiceResponseDto>> GetAllSalesInvoicesAsync();
    Task<PagedResult<SalesInvoiceResponseDto>> GetAllSalesInvoicesAsync(PaginationParams paging);
    Task<bool> MarkInvoicePaidAsync(int invoiceId);
    Task<bool> SendInvoiceEmailAsync(int invoiceId);
}
