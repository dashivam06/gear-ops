using System;
using System.Collections.Generic;

namespace gearOps.Application.DTOs;

// ===== STAFF PROFILE DTOs =====
public class StaffProfileResponseDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string Position { get; set; } = null!;
    public string Status { get; set; } = "Active";
    public DateTime JoinDate { get; set; }
    public bool EmailSubscribed { get; set; } = true;
}

public class UpdateStaffProfileDto
{
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
}

// ===== APPOINTMENT ASSIGNMENT DTOs =====
public class StaffAppointmentDto
{
    public int AppointmentId { get; set; }
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public DateTime AppointmentDate { get; set; }
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? ApprovalNotes { get; set; }
    public string? ApprovedByStaffName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsInvoiced { get; set; }
}

public class CompleteAppointmentDto
{
    public int AppointmentId { get; set; }
    public string ServiceDescription { get; set; } = null!;
    public decimal ServiceCost { get; set; }
}

/// <summary>
/// Body for POST /api/v1/staff/service-records — standalone service record creation.
/// ServiceCost may be 0 at create time and updated later via PUT.
/// </summary>
public class CreateServiceRecordDto
{
    public int AppointmentId { get; set; }
    public string ServiceDescription { get; set; } = null!;
    /// <summary>Allowed to be 0 at creation; update later when cost is finalised.</summary>
    public decimal ServiceCost { get; set; } = 0m;
}

// ===== SERVICE RECORD DTOs =====
public class StaffServiceRecordDto
{
    public int ServiceRecordId { get; set; }
    public int AppointmentId { get; set; }
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string ServiceDescription { get; set; } = null!;
    public decimal ServiceCost { get; set; }
    public DateTime ServiceDate { get; set; }
    public string Status { get; set; } = null!;
    public int? ReviewRating { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateServiceRecordDto
{
    public int ServiceRecordId { get; set; }
    public string ServiceDescription { get; set; } = null!;
    public decimal ServiceCost { get; set; }
}

// ===== PERFORMANCE REPORT DTOs =====
public class StaffPerformanceReportDto
{
    public int StaffId { get; set; }
    public string StaffName { get; set; } = null!;
    public string Position { get; set; } = null!;
    public int TotalAppointmentsCompleted { get; set; }
    public int TotalServiceRecords { get; set; }
    public decimal TotalRevenueGenerated { get; set; }
    public decimal AverageServiceCost { get; set; }
    public double AverageCustomerRating { get; set; }
    public int PendingAppointments { get; set; }
    public DateTime ReportGeneratedAt { get; set; }
}

public class StaffScheduleSummaryDto
{
    public int StaffId { get; set; }
    public string StaffName { get; set; } = null!;
    public int TodayAppointments { get; set; }
    public int UpcomingAppointments { get; set; }
    public int CompletedAppointmentsThisMonth { get; set; }
    public List<StaffAppointmentDto> TodaySchedule { get; set; } = new();
    public List<StaffAppointmentDto> UpcomingSchedule { get; set; } = new();
}

public class StaffDashboardDto
{
    public StaffProfileResponseDto Profile { get; set; } = null!;
    public StaffScheduleSummaryDto ScheduleSummary { get; set; } = null!;
    public StaffPerformanceReportDto PerformanceMetrics { get; set; } = null!;
}

// ===== MONTHLY REPORT DTOs =====
public class StaffMonthlyReportDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalRevenue { get; set; }
    public int CompletedServices { get; set; }
    public decimal AverageCostPerService { get; set; }
    public double AverageCustomerRating { get; set; }
    public int TotalAppointments { get; set; }
    public int CancelledAppointments { get; set; }
}

// ===== STAFF CUSTOMER MANAGEMENT DTOs =====
public class StaffRegisterCustomerDto
{
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? VehicleNumber { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public string? VehicleImageUrl { get; set; }
}

public class StaffCustomerDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
    public decimal CreditsRemaining { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<StaffCustomerVehicleDto> Vehicles { get; set; } = new();
}

public class StaffCustomerVehicleDto
{
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string? ImageUrl { get; set; }
}

// ===== STAFF VEHICLE DETAIL DTOs =====
public class StaffVehicleDetailDto
{
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string? ImageUrl { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime? LastServiceDate { get; set; }
    public List<StaffVehicleAppointmentDto> Appointments { get; set; } = new();
    public List<StaffVehicleServiceRecordDto> ServiceRecords { get; set; } = new();
    public List<StaffVehicleInvoiceDto> Invoices { get; set; } = new();
}

public class StaffVehicleAppointmentDto
{
    public int AppointmentId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = null!;
}

public class StaffVehicleServiceRecordDto
{
    public int ServiceRecordId { get; set; }
    public DateTime ServiceDate { get; set; }
    public decimal ServiceCost { get; set; }
    public string ServiceDescription { get; set; } = null!;
}

public class StaffVehicleInvoiceDto
{
    public int SalesInvoiceId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal FinalAmount { get; set; }
    public bool IsPaid { get; set; }
}

// ===== STAFF SALES INVOICE DTOs =====
public class CreateSalesInvoiceDto
{
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public List<SalesInvoiceItemInputDto> Items { get; set; } = new();
    public bool IsPaid { get; set; } = true;
    public DateTime? DueDate { get; set; }
    /// <summary>
    /// Optional manual discount (e.g. staff override).
    /// Applied on top of any automatic loyalty discount.
    /// The larger of the two is used; they do not stack.
    /// </summary>
    public decimal? DiscountAmount { get; set; }
    /// <summary>Optional: link invoice to an appointment for reporting.</summary>
    public int? AppointmentId { get; set; }
    /// <summary>"Parts" | "Appointment" | null. Persisted and returned on list/detail.</summary>
    public string? InvoiceType { get; set; }
    /// <summary>Labor / service charge (separate from parts cost). Defaults to 0.</summary>
    public decimal ServiceCharge { get; set; } = 0m;
}

public class SalesInvoiceItemInputDto
{
    public int PartId { get; set; }
    public int Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
}

public class SalesInvoiceResponseDto
{
    public int SalesInvoiceId { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string CustomerEmail { get; set; } = null!;
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    /// <summary>Optional appointment this invoice is linked to.</summary>
    public int? AppointmentId { get; set; }
    /// <summary>"Parts" | "Appointment" | null.</summary>
    public string? InvoiceType { get; set; }
    public DateTime InvoiceDate { get; set; }
    public decimal SubTotal { get; set; }
    /// <summary>Labor / service charge (separate from parts).</summary>
    public decimal ServiceCharge { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? DueDate { get; set; }
    public List<SalesInvoiceItemResponseDto> Items { get; set; } = new();
}

public class SalesInvoiceItemResponseDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal TotalPrice { get; set; }
}

// ===== STAFF CUSTOMER REPORT DTOs =====
public class StaffCustomerReportDto
{
    public List<StaffCustomerReportRowDto> TopSpenders { get; set; } = new();
    public List<StaffCustomerReportRowDto> RegularCustomers { get; set; } = new();
    public List<StaffCustomerReportRowDto> PendingCredits { get; set; } = new();
}

public class StaffCustomerReportRowDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public decimal? TotalSpend { get; set; }
    public int? TotalPurchases { get; set; }
    public int? VisitCount { get; set; }
    public decimal? CreditsRemaining { get; set; }
    public int? DaysOverdue { get; set; }
    public DateTime? LastActivity { get; set; }
}

// ===== APPOINTMENT TIME SLOT DTOs =====
public class TimeSlotDto
{
    public int SlotNumber { get; set; }  // 1-7 (10AM-11AM, 11AM-12PM, etc, excluding 1-2PM)
    public string DisplayTime { get; set; } = null!;  // "10:00 AM - 11:00 AM"
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public bool IsBooked { get; set; }
    public bool IsBreak { get; set; } = false;
}

public class AvailableTimeSlotsResponseDto
{
    public DateTime Date { get; set; }
    public List<TimeSlotDto> TimeSlots { get; set; } = new();
    public int TotalAvailableSlots { get; set; }
    public int TotalBookedSlots { get; set; }
}

public class AppointmentDecisionDto
{
    public int AppointmentId { get; set; }
    public string Decision { get; set; } = null!;  // "Approved" or "Rejected"
    public string? Notes { get; set; }
}

public class AppointmentDecisionResponseDto
{
    public int AppointmentId { get; set; }
    public string Status { get; set; } = null!;
    public string Decision { get; set; } = null!;
    public string? Notes { get; set; }
    public string ApprovedByStaffName { get; set; } = null!;
    public DateTime ApprovedAt { get; set; }
    public string CustomerEmail { get; set; } = null!;
    public string Message { get; set; } = null!;
}

public class RescheduleAppointmentDto
{
    public DateTime NewDate { get; set; }
}
