using System;
using System.Text.Json.Serialization;
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
    public string? FullName { get; set; }
    public string? Phone { get; set; }
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
/// <summary>Body for POST /api/v1/customers/appointments — fields frozen in contract.</summary>
public class CreateAppointmentDto
{
    /// <summary>ID of the customer's vehicle to service.</summary>
    public int VehicleId { get; set; }
    /// <summary>Requested service date/time (ISO-8601). JSON alias: requestedDate.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("requestedDate")]
    public DateTime RequestedDate { get; set; }
    /// <summary>Optional customer notes. JSON alias: remarks.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}

/// <summary>Body for PUT /api/v1/customers/appointments/{id} — customer reschedule.</summary>
public class UpdateAppointmentDto
{
    /// <summary>Set by the route parameter; not sent by client.</summary>
    public int AppointmentId { get; set; }
    /// <summary>New requested date/time. JSON alias: requestedDate.</summary>
    [System.Text.Json.Serialization.JsonPropertyName("requestedDate")]
    public DateTime RequestedDate { get; set; }
}

public class AppointmentResponseDto
{
    public int AppointmentId { get; set; }
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    /// <summary>Canonical name; client also reads appointmentDate.</summary>
    public DateTime RequestedDate { get; set; }
    /// <summary>Alias for backward compat — same value as RequestedDate.</summary>
    public DateTime AppointmentDate => RequestedDate;
    /// <summary>Customer notes / description.</summary>
    public string? Remarks { get; set; }
    /// <summary>Alias for Description used on older client screens.</summary>
    public string? Description => Remarks;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}

// ===== REVIEW DTOs =====
public class CreateReviewDto
{
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

public class ReviewResponseDto
{
    public int ReviewId { get; set; }
    public int AppointmentId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ===== PART REQUEST DTOs =====
public class CreatePartRequestDto
{
    public string PartName { get; set; } = null!;
    public string? Description { get; set; }
    public int? VehicleId { get; set; }
}

public class ReviewPartRequestDto
{
    public string? DecisionNote { get; set; }
    public int? PartId { get; set; }
}

public class OrderPartRequestDto
{
    public int? PartId { get; set; }
    public int VendorId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
    
    // Optional fields for when creating a new part from the request
    public string? NewPartCategory { get; set; }
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
    public int? ReviewedByStaffId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? DecisionNote { get; set; }
    public int? SuggestedPartId { get; set; }
}

// ===== SALES INVOICE & PURCHASE HISTORY DTOs =====
public class DirectBuyPartsDto
{
    public int? VehicleId { get; set; }
    public List<DirectBuyPartItemDto> Items { get; set; } = new();
}

public class DirectBuyPartItemDto
{
    public int PartId { get; set; }
    public int Quantity { get; set; }
}

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
    public int AppointmentId { get; set; }
    public int VehicleId { get; set; }
    public string VehicleNumber { get; set; } = null!;
    public DateTime ServiceDate { get; set; }
    public string Description { get; set; } = null!;
    public string? IssueReported { get; set; }
    public string? StaffAnswer { get; set; }
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
