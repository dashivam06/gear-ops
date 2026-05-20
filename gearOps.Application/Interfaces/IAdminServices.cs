using System.Collections.Generic;
using System.Threading.Tasks;
using gearOps.Application.DTOs;

namespace gearOps.Application.Interfaces;

public interface IStaffService
{
    Task<CreateStaffResponseDto> CreateStaffAsync(CreateStaffDto dto);
    Task<StaffResponseDto> UpdateStaffAsync(UpdateStaffDto dto);
    Task<StaffResponseDto?> GetStaffByIdAsync(int staffId);
    Task<List<StaffResponseDto>> GetAllStaffAsync();
    Task<PagedResult<StaffResponseDto>> GetAllStaffAsync(PaginationParams paging);
    Task<bool> DeleteStaffAsync(int staffId);
    Task<bool> ToggleStaffStatusAsync(int staffId, bool isActive);
}

public interface IPartService
{
    Task<PartResponseDto> CreatePartAsync(CreatePartDto dto);
    Task<PartResponseDto> UpdatePartAsync(UpdatePartDto dto);
    Task<PartResponseDto?> GetPartByIdAsync(int partId);
    Task<List<PartResponseDto>> GetAllPartsAsync();
    Task<PagedResult<PartResponseDto>> GetAllPartsAsync(PaginationParams paging);
    Task<List<PartResponseDto>> GetPartsByCategoryAsync(string category);
    Task<PagedResult<PartResponseDto>> GetPartsByCategoryAsync(string category, PaginationParams paging);
    Task<List<PartResponseDto>> SearchPartsAsync(string query);
    Task<PagedResult<PartResponseDto>> SearchPartsAsync(string query, PaginationParams paging);
    Task<List<PartResponseDto>> GetPartsByVendorAsync(int vendorId);
    Task<bool> DeletePartAsync(int partId);
}

public interface IVendorService
{
    Task<VendorResponseDto> CreateVendorAsync(CreateVendorDto dto);
    Task<VendorResponseDto> UpdateVendorAsync(UpdateVendorDto dto);
    Task<VendorResponseDto?> GetVendorByIdAsync(int vendorId);
    Task<List<VendorResponseDto>> GetAllVendorsAsync();
    Task<PagedResult<VendorResponseDto>> GetAllVendorsAsync(PaginationParams paging);
    Task<bool> DeleteVendorAsync(int vendorId);
}

public interface IPurchaseInvoiceService
{
    Task<PurchaseOrderResponseDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto);
    Task<PurchaseOrderResponseDto> UpdatePurchaseOrderAsync(int purchaseOrderId, UpdatePurchaseOrderDto dto);
    Task<PurchaseOrderResponseDto> SendPurchaseOrderToVendorAsync(int purchaseOrderId, SendPurchaseOrderToVendorDto dto);
    Task<PurchaseOrderResponseDto> ConfirmPurchaseOrderAsync(int purchaseOrderId, ConfirmPurchaseOrderDto dto);
    Task<PurchaseOrderResponseDto> MarkPurchaseOrderDeliveredAsync(int purchaseOrderId, DeliverPurchaseOrderDto dto);
    Task<PurchaseOrderResponseDto?> GetPurchaseOrderByIdAsync(int purchaseOrderId);
    Task<List<PurchaseOrderResponseDto>> GetPurchaseOrdersByVendorAsync(int vendorId);
    Task<List<PurchaseOrderResponseDto>> GetAllPurchaseOrdersAsync();
    Task<PagedResult<PurchaseOrderResponseDto>> GetAllPurchaseOrdersAsync(PaginationParams paging);
}

public interface IReportService
{
    Task<FinancialReportDto> GetFinancialReportAsync(ReportPeriod period, int? year = null, int? month = null);
    Task<FinancialReportDto> GetFinancialReportAsync(DateTime startDate, DateTime endDate);
    Task<FinancialReportPdfResponseDto> UploadFinancialReportPdfAsync(DateTime startDate, DateTime endDate);
    Task<List<InventoryReportDto>> GetInventoryReportAsync();
    Task<List<LowStockPartDto>> GetLowStockPartsAsync(int threshold = 10);
}
