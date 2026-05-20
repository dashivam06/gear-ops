using System.Collections.Generic;
using System.Threading.Tasks;
using gearOps.Application.DTOs;

namespace gearOps.Application.Interfaces;

public interface ICustomerProfileService
{
    Task<CustomerProfileResponseDto> GetProfileAsync(int userId);
    Task<CustomerProfileResponseDto> UpdateProfileAsync(int userId, UpdateCustomerProfileDto dto);
    Task<VehicleResponseDto> AddVehicleAsync(int userId, CreateVehicleDto dto);
    Task<VehicleResponseDto> UpdateVehicleAsync(int userId, UpdateVehicleDto dto);
    Task<VehicleResponseDto?> GetVehicleByIdAsync(int vehicleId);
    Task<List<VehicleResponseDto>> GetCustomerVehiclesAsync(int userId);
    Task<bool> DeleteVehicleAsync(int userId, int vehicleId);
}

public interface IAppointmentService
{
    Task<AppointmentResponseDto> CreateAppointmentAsync(int userId, CreateAppointmentDto dto);
    Task<AppointmentResponseDto> UpdateAppointmentAsync(int userId, UpdateAppointmentDto dto);
    Task<AppointmentResponseDto?> GetAppointmentByIdAsync(int appointmentId);
    Task<List<AppointmentResponseDto>> GetCustomerAppointmentsAsync(int userId);
    Task<List<AppointmentResponseDto>> GetUpcomingAppointmentsAsync(int userId);
    Task<bool> CancelAppointmentAsync(int appointmentId, int userId);
}

public interface IReviewService
{
    Task<ReviewResponseDto> CreateReviewAsync(int userId, CreateReviewDto dto);
    Task<ReviewResponseDto?> GetReviewByIdAsync(int reviewId);
    Task<List<ReviewResponseDto>> GetCustomerReviewsAsync(int userId);
    Task<bool> DeleteReviewAsync(int reviewId);
}

public interface IPartRequestService
{
    Task<PartRequestResponseDto> CreatePartRequestAsync(int userId, CreatePartRequestDto dto);
    Task<PartRequestResponseDto?> GetPartRequestByIdAsync(int partRequestId);
    Task<List<PartRequestResponseDto>> GetCustomerPartRequestsAsync(int userId);
    Task<List<PartRequestResponseDto>> GetPendingPartRequestsAsync(int userId);
    Task<List<PartRequestResponseDto>> GetAllPartRequestsAsync();
    Task<PagedResult<PartRequestResponseDto>> GetAllPartRequestsPagedAsync(PaginationParams paging, string? status = null);
    Task<List<PartRequestResponseDto>> GetPendingPartRequestsForReviewAsync();
    Task<PartRequestResponseDto?> ApprovePartRequestAsync(int staffUserId, int partRequestId, ReviewPartRequestDto dto);
    Task<PartRequestResponseDto?> RejectPartRequestAsync(int staffUserId, int partRequestId, ReviewPartRequestDto dto);
    Task<PartRequestResponseDto?> OrderPartRequestAsync(int adminUserId, int partRequestId, OrderPartRequestDto dto);
}

public interface IPurchaseHistoryService
{
    Task<SalesInvoiceDetailDto?> GetInvoiceDetailsAsync(int invoiceId);
    Task<List<SalesInvoiceDetailDto>> GetCustomerPurchaseHistoryAsync(int userId);
    Task<List<ServiceRecordResponseDto>> GetCustomerServiceHistoryAsync(int userId);
    Task<CustomerHistorySummaryDto> GetCustomerHistorySummaryAsync(int userId);
    Task<SalesInvoiceDetailDto> BuyPartsDirectlyAsync(int userId, DirectBuyPartsDto dto);
}

public interface ILoyaltyProgramService
{
    Task<LoyaltyStatusDto> GetLoyaltyStatusAsync(int userId);
    decimal CalculateDiscount(decimal purchaseAmount);
    Task ApplyLoyaltyDiscountAsync(int userId, int invoiceId);
}

public interface IInvoicePdfService
{
    Task<InvoicePdfResponseDto> GenerateInvoicePdfAsync(int invoiceId, int userId);
}

public interface ICreditService
{
    Task<CreditBalanceDto> GetCreditBalanceAsync(int userId);
    Task<List<OverdueCreditDto>> GetOverdueCreditsAsync(int userId, int daysOverdue = 30);
}
