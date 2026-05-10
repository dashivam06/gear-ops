using System;
using System.Collections.Generic;

namespace gearOps.Application.DTOs;

// ===== PROFILE & AUTHENTICATION DTOs =====
public class CustomerProfileResponseDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
    public decimal CreditsRemaining { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<VehicleResponseDto> Vehicles { get; set; } = new();
}

public class UpdateCustomerProfileDto
{
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
}

// ===== VEHICLE DTOs =====
public class CreateVehicleDto
{
    public string VehicleNumber { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateVehicleDto
{
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string? ImageUrl { get; set; }
}

public class VehicleResponseDto
{
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ===== APPOINTMENT DTOs =====
public class CreateAppointmentDto
{
    public int VehicleId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Description { get; set; } = null!;
}

public class UpdateAppointmentDto
{
    public int AppointmentId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Description { get; set; } = null!;
}

public class AppointmentResponseDto
{
    public int AppointmentId { get; set; }
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    public DateTime AppointmentDate { get; set; }
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

// ===== REVIEW DTOs =====
public class CreateReviewDto
{
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = null!;
}

public class ReviewResponseDto
{
    public int ReviewId { get; set; }
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

// ===== PART REQUEST DTOs =====
public class CreatePartRequestDto
{
    public string PartName { get; set; } = null!;
    public string? Description { get; set; }
    public int? VehicleId { get; set; }
}

public class PartRequestResponseDto
{
    public int PartRequestId { get; set; }
    public string PartName { get; set; } = null!;
    public string? Description { get; set; }
    public int? VehicleId { get; set; }
    public string? VehicleNumber { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

// ===== SALES INVOICE & PURCHASE HISTORY DTOs =====
public class SalesInvoiceDetailDto
{
    public int SalesInvoiceId { get; set; }
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    public DateTime InvoiceDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = null!;
    public List<SalesInvoiceItemDetailDto> Items { get; set; } = new();
}

public class SalesInvoiceItemDetailDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal PricePerUnit { get; set; }
    public decimal TotalPrice { get; set; }
}

// ===== SERVICE HISTORY DTOs =====
public class ServiceRecordResponseDto
{
    public int ServiceRecordId { get; set; }
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    public DateTime ServiceDate { get; set; }
    public string Description { get; set; } = null!;
    public string Status { get; set; } = null!;
    public decimal Cost { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ===== LOYALTY PROGRAM DTOs =====
public class LoyaltyStatusDto
{
    public decimal TotalSpent { get; set; }
    public int TransactionCount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public bool EligibleForNextTier { get; set; }
    public decimal RemainingForNextTier { get; set; }
    public string CurrentTier { get; set; } = null!;
    public List<LoyaltyTransactionDto> RecentTransactions { get; set; } = new();
}

public class LoyaltyTransactionDto
{
    public int TransactionId { get; set; }
    public decimal Amount { get; set; }
    public decimal DiscountApplied { get; set; }
    public DateTime TransactionDate { get; set; }
}

// ===== PURCHASE & SERVICE HISTORY SUMMARY =====
public class CustomerHistorySummaryDto
{
    public int TotalPurchases { get; set; }
    public decimal TotalSpent { get; set; }
    public int TotalServices { get; set; }
    public List<SalesInvoiceDetailDto> PurchaseHistory { get; set; } = new();
    public List<ServiceRecordResponseDto> ServiceHistory { get; set; } = new();
}

// ===== PDF GENERATION DTO =====
public class InvoicePdfRequestDto
{
    public int SalesInvoiceId { get; set; }
}

public class InvoicePdfResponseDto
{
    public byte[] PdfBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = "application/pdf";
}

// ===== CREDIT & PAYMENT DTOs =====
public class CreditBalanceDto
{
    public decimal CreditsRemaining { get; set; }
    public List<OverdueCreditDto> OverdueCredits { get; set; } = new();
}

public class OverdueCreditDto
{
    public int SalesInvoiceId { get; set; }
    public decimal Amount { get; set; }
    public int DaysOverdue { get; set; }
    public DateTime DueDate { get; set; }
}

// ===== DASHBOARD/ANALYTICS DTOs =====
public class CustomerDashboardDto
{
    public CustomerProfileResponseDto Profile { get; set; } = null!;
    public CreditBalanceDto CreditBalance { get; set; } = null!;
    public LoyaltyStatusDto LoyaltyStatus { get; set; } = null!;
    public List<AppointmentResponseDto> UpcomingAppointments { get; set; } = new();
    public List<PartRequestResponseDto> PendingPartRequests { get; set; } = new();
    public int TotalVehicles { get; set; }
}
