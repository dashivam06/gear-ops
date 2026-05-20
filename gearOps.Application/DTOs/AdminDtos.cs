using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using gearOps.Domain.Enums;

namespace gearOps.Application.DTOs;

// ===== STAFF DTOs =====
public class CreateStaffDto
{
    [Required]
    public string FullName { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Phone { get; set; } = null!;

    public string? Address { get; set; }

    [Required]
    public string Position { get; set; } = null!;

    public string? ProfileImageUrl { get; set; }
}

public class UpdateStaffDto
{
    public int StaffId { get; set; }

    [Required]
    public string FullName { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    public string Phone { get; set; } = null!;

    public string? Address { get; set; }

    [Required]
    public string Position { get; set; } = null!;

    public string? ProfileImageUrl { get; set; }
    public bool IsActive { get; set; }
}

public class StaffResponseDto
{
    public int StaffId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string Position { get; set; } = null!;
    public string? ProfileImageUrl { get; set; }
    public DateTime JoinDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateStaffResponseDto : StaffResponseDto
{
    public bool OnboardingEmailSent { get; set; }
    public string Message { get; set; } = null!;
}

// ===== VENDOR DTOs =====
public class CreateVendorDto
{
    [Required]
    public string VendorName { get; set; } = null!;

    public string? ContactPerson { get; set; }

    [Required]
    public string Phone { get; set; } = null!;

    [EmailAddress]
    public string? Email { get; set; }

    public string? Address { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateVendorDto
{
    public int VendorId { get; set; }

    [Required]
    public string VendorName { get; set; } = null!;

    public string? ContactPerson { get; set; }

    [Required]
    public string Phone { get; set; } = null!;

    [EmailAddress]
    public string? Email { get; set; }

    public string? Address { get; set; }
    public string? ImageUrl { get; set; }
}

public class VendorResponseDto
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string Status { get; set; } = null!;
}

// ===== PART DTOs =====
public class CreatePartDto
{
    [Range(1, int.MaxValue)]
    public int VendorId { get; set; }

    [Required]
    public string PartName { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    public string Category { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Required]
    public string Unit { get; set; } = null!;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal CostPricePerUnit { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal SellingPricePerUnit { get; set; }

    public string? ImageUrl { get; set; }
}

public class UpdatePartDto
{
    public int PartId { get; set; }

    [Range(1, int.MaxValue)]
    public int VendorId { get; set; }

    [Required]
    public string PartName { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    public string Category { get; set; } = null!;

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Required]
    public string Unit { get; set; } = null!;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal CostPricePerUnit { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal SellingPricePerUnit { get; set; }

    public string? ImageUrl { get; set; }
}

public class PartResponseDto
{
    public int PartId { get; set; }
    public int VendorId { get; set; }
    public string PartName { get; set; } = null!;
    public string? Description { get; set; }
    public string Category { get; set; } = null!;
    public int StockQuantity { get; set; }
    public string Unit { get; set; } = null!;
    public decimal CostPricePerUnit { get; set; }
    public decimal SellingPricePerUnit { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string VendorName { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string Status { get; set; } = null!;
}

// ===== PURCHASE ORDER / INVOICE DTOs =====
public class CreatePurchaseOrderDto
{
    [Range(1, int.MaxValue)]
    public int VendorId { get; set; }

    public string? InvoiceNumber { get; set; }

    [Required]
    [MinLength(1)]
    public List<PurchaseOrderItemDto> Items { get; set; } = new();
}

public class UpdatePurchaseOrderDto
{
    public string? InvoiceNumber { get; set; }

    [Required]
    [MinLength(1)]
    public List<PurchaseOrderItemDto> Items { get; set; } = new();

    public string? Notes { get; set; }
}

public class ConfirmPurchaseOrderDto
{
    [Required]
    public string InvoiceNumber { get; set; } = null!;

    public string? Notes { get; set; }
}

public class SendPurchaseOrderToVendorDto
{
    public string? Message { get; set; }
}

public class DeliverPurchaseOrderDto
{
    public string? Notes { get; set; }
}

public class PurchaseOrderItemDto
{
    [Range(1, int.MaxValue)]
    public int PartId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal UnitPrice { get; set; }
}

public class PurchaseOrderResponseDto
{
    public int PurchaseOrderId { get; set; }
    public int VendorId { get; set; }
    public string VendorName { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public string StatusText { get; set; } = null!;
    public bool IsEditable { get; set; }
    public DateTime? SentToVendorAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public List<PurchaseOrderItemResponseDto> Items { get; set; } = new();
    public List<PurchaseOrderLogResponseDto> Logs { get; set; } = new();
}

public class PurchaseOrderItemResponseDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class PurchaseOrderLogResponseDto
{
    public int PurchaseOrderLogId { get; set; }
    public string Action { get; set; } = null!;
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public string? Notes { get; set; }
    public bool EmailSentToVendor { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ===== FINANCIAL REPORT DTOs =====
public class FinancialReportDto
{
    public DateTime ReportDate { get; set; }
    public ReportPeriod Period { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string PeriodLabel { get; set; } = null!;
    public decimal TotalSalesRevenue { get; set; }
    public decimal TotalPurchaseCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalPartsMovement { get; set; }
    // Added for spec compliance
    public decimal TotalRevenue => TotalSalesRevenue;
    public decimal TotalExpenses => TotalPurchaseCost;
    public decimal NetProfit => GrossProfit;
}

public class FinancialReportPdfResponseDto
{
    public string Url { get; set; } = null!;
    public string SecureUrl { get; set; } = null!;
    public string PublicId { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public FinancialReportDto Report { get; set; } = null!;
}

public class InventoryReportDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
    public decimal CostPricePerUnit { get; set; }
    public decimal InventoryValue { get; set; }
}

public class LowStockPartDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public int StockQuantity { get; set; }
    public string Unit { get; set; } = null!;
}

public enum ReportPeriod
{
    Daily,
    Monthly,
    Yearly
}

// ===== TOGGLE STATUS DTO =====
public class ToggleStatusDto
{
    public bool IsActive { get; set; }
}

// ===== ADMIN PROFILE / NOTIFICATION DTOs =====
public class AdminProfileResponseDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateAdminProfileDto
{
    [Required]
    public string FullName { get; set; } = null!;

    [Required]
    public string Phone { get; set; } = null!;

    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
}

// ChangeAdminPasswordDto removed — use ChangePasswordDto from AuthDtos.cs
// DeleteAdminAccountDto removed — use DeleteAccountDto from AuthDtos.cs

public class AdminNotificationDto
{
    public int NotificationId { get; set; }
    public string LogType { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string MailedStatus { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
