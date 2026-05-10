using System;
using System.Collections.Generic;

namespace gearOps.Application.DTOs;

// ===== STAFF PROFILE DTOs =====
public class StaffProfileResponseDto
{
    public int StaffId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string Position { get; set; } = null!;
    public DateTime JoinDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
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
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string CustomerPhone { get; set; } = null!;
    public DateTime AppointmentDate { get; set; }
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

public class CompleteAppointmentDto
{
    public int AppointmentId { get; set; }
    public string ServiceDescription { get; set; } = null!;
    public decimal ServiceCost { get; set; }
}

// ===== SERVICE RECORD DTOs =====
public class StaffServiceRecordDto
{
    public int ServiceRecordId { get; set; }
    public int AppointmentId { get; set; }
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
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
}

public class StaffCustomerDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
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
}

// ===== STAFF SALES INVOICE DTOs =====
public class CreateSalesInvoiceDto
{
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public List<SalesInvoiceItemInputDto> Items { get; set; } = new();
    public bool IsPaid { get; set; } = true;
    public DateTime? DueDate { get; set; }
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
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }
    public decimal SubTotal { get; set; }
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
    public List<StaffCustomerDto> TopSpenders { get; set; } = new();
    public List<StaffCustomerDto> RegularCustomers { get; set; } = new();
    public List<StaffCustomerCreditDto> PendingCredits { get; set; } = new();
}

public class StaffCustomerCreditDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public decimal CreditsRemaining { get; set; }
    public int DaysOverdue { get; set; }
}
