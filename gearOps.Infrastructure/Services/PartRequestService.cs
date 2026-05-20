using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;
using gearOps.Domain.Enums;
using gearOps.Infrastructure.Data;
using gearOps.Infrastructure.Extensions;

namespace gearOps.Infrastructure.Services;

public class PartRequestService : IPartRequestService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PartRequestService> _logger;
    private readonly IEmailService _emailService;

    public PartRequestService(AppDbContext context, ILogger<PartRequestService> logger, IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<PartRequestResponseDto> CreatePartRequestAsync(int userId, CreatePartRequestDto dto)
    {
        try
        {
            // Verify user exists
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted)
                ?? throw new NotFoundException($"Customer with ID {userId} not found.");

            var partRequest = new PartRequest
            {
                CustomerId = userId,
                VehicleId = dto.VehicleId,
                PartName = dto.PartName,
                Description = dto.Description,
                RequestedDate = DateTime.UtcNow,
                Status = PartRequestStatus.Pending
            };

            // If a vehicle was provided, ensure it exists and belongs to this customer
            if (dto.VehicleId.HasValue)
            {
                var vehicle = await _context.Vehicles.FindAsync(dto.VehicleId.Value);
                if (vehicle == null || vehicle.CustomerId != userId || vehicle.IsDeleted)
                    throw new NotFoundException($"Vehicle with ID {dto.VehicleId.Value} not found for this customer.");
            }

            _context.PartRequests.Add(partRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Part request created: {partRequest.PartRequestId} by customer {userId}");

            return MapToDto(partRequest);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error creating part request");
            throw;
        }
    }

    public async Task<PartRequestResponseDto?> GetPartRequestByIdAsync(int partRequestId)
    {
        var partRequest = await _context.PartRequests
            .Include(pr => pr.Customer)
            .Include(pr => pr.Vehicle)
            .FirstOrDefaultAsync(pr => pr.PartRequestId == partRequestId);

        return partRequest == null ? null : MapToDto(partRequest);
    }

    public async Task<List<PartRequestResponseDto>> GetCustomerPartRequestsAsync(int userId)
    {
        var partRequests = await _context.PartRequests
            .Where(pr => pr.CustomerId == userId)
            .OrderByDescending(pr => pr.RequestedDate)
            .ToListAsync();

        return partRequests.Select(MapToDto).ToList();
    }

    public async Task<List<PartRequestResponseDto>> GetPendingPartRequestsAsync(int userId)
    {
        var partRequests = await _context.PartRequests
            .Where(pr => pr.CustomerId == userId && pr.Status == PartRequestStatus.Pending)
            .OrderByDescending(pr => pr.RequestedDate)
            .ToListAsync();

        return partRequests.Select(MapToDto).ToList();
    }

    public async Task<List<PartRequestResponseDto>> GetAllPartRequestsAsync()
    {
        var partRequests = await _context.PartRequests
            .Include(pr => pr.Customer)
            .Include(pr => pr.Vehicle)
            .OrderByDescending(pr => pr.RequestedDate)
            .ToListAsync();

        return partRequests.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<PartRequestResponseDto>> GetAllPartRequestsPagedAsync(PaginationParams paging, string? status = null)
    {
        var query = _context.PartRequests
            .Include(pr => pr.Customer)
            .Include(pr => pr.Vehicle)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PartRequestStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(pr => pr.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(paging.Search))
        {
            var search = paging.Search.ToLower();
            query = query.Where(pr =>
                (pr.PartName != null && pr.PartName.ToLower().Contains(search)) ||
                (pr.Customer.FullName != null && pr.Customer.FullName.ToLower().Contains(search)) ||
                (pr.Vehicle.VehicleNumber != null && pr.Vehicle.VehicleNumber.ToLower().Contains(search))
            );
        }

        query = query.OrderByDescending(pr => pr.RequestedDate);
        var paged = await query.ToPagedResultAsync(paging);

        return new PagedResult<PartRequestResponseDto>
        {
            Items = paged.Items.Select(MapToDto).ToList(),
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalItems = paged.TotalItems,
            TotalPages = paged.TotalPages
        };
    }

    public async Task<List<PartRequestResponseDto>> GetPendingPartRequestsForReviewAsync()
    {
        var partRequests = await _context.PartRequests
            .Where(pr => pr.Status == PartRequestStatus.Pending)
            .Include(pr => pr.Customer)
            .Include(pr => pr.Vehicle)
            .OrderByDescending(pr => pr.RequestedDate)
            .ToListAsync();

        return partRequests.Select(MapToDto).ToList();
    }

    public async Task<PartRequestResponseDto?> ApprovePartRequestAsync(int staffUserId, int partRequestId, ReviewPartRequestDto dto)
    {
        var partRequest = await _context.PartRequests
            .Include(pr => pr.Customer)
            .Include(pr => pr.Vehicle)
            .FirstOrDefaultAsync(pr => pr.PartRequestId == partRequestId);
        if (partRequest == null)
            return null;

        partRequest.Status = PartRequestStatus.Available;
        partRequest.ReviewedByStaffId = staffUserId;
        partRequest.ReviewedAt = DateTime.UtcNow;
        partRequest.DecisionNote = dto.DecisionNote?.Trim();

        await _context.SaveChangesAsync();

        _logger.LogInformation("Part request {PartRequestId} approved and marked Available", partRequestId);

        await _emailService.SendPartRequestDecisionEmailAsync(
            partRequest.Customer.Email,
            partRequest.Customer.FullName,
            partRequest.PartName,
            "Approved",
            partRequest.DecisionNote);

        return MapToDto(partRequest);
    }

    public async Task<PartRequestResponseDto?> RejectPartRequestAsync(int staffUserId, int partRequestId, ReviewPartRequestDto dto)
    {
        var partRequest = await _context.PartRequests
            .Include(pr => pr.Customer)
            .Include(pr => pr.Vehicle)
            .FirstOrDefaultAsync(pr => pr.PartRequestId == partRequestId);
        if (partRequest == null)
            return null;

        partRequest.Status = PartRequestStatus.Rejected;
        partRequest.ReviewedByStaffId = staffUserId;
        partRequest.ReviewedAt = DateTime.UtcNow;
        partRequest.DecisionNote = dto.DecisionNote?.Trim();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Part request {PartRequestId} rejected", partRequestId);

        await _emailService.SendPartRequestDecisionEmailAsync(
            partRequest.Customer.Email,
            partRequest.Customer.FullName,
            partRequest.PartName,
            "Rejected",
            partRequest.DecisionNote);

        return MapToDto(partRequest);
    }

    public async Task<PartRequestResponseDto?> OrderPartRequestAsync(int adminUserId, int partRequestId, OrderPartRequestDto dto)
    {
        var partRequest = await _context.PartRequests
            .Include(pr => pr.Customer)
            .Include(pr => pr.Vehicle)
            .FirstOrDefaultAsync(pr => pr.PartRequestId == partRequestId);

        if (partRequest == null)
            return null;

        if (partRequest.Status != PartRequestStatus.PendingAdminReview && partRequest.Status != PartRequestStatus.Pending)
            throw new BadRequestException("Part request must be in pending or review status to order.");

        Part part;
        if (dto.PartId.HasValue)
        {
            part = await _context.Parts.FirstOrDefaultAsync(p => p.PartId == dto.PartId.Value && !p.IsDeleted)
                ?? throw new NotFoundException($"Part with ID {dto.PartId.Value} not found.");
        }
        else
        {
            var vendor = await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == dto.VendorId && !v.IsDeleted)
                ?? throw new NotFoundException($"Vendor with ID {dto.VendorId} not found.");

            part = new Part
            {
                VendorId = dto.VendorId,
                PartName = partRequest.PartName,
                Description = dto.Notes ?? partRequest.Description ?? "Created from part request",
                Category = dto.NewPartCategory ?? "General",
                StockQuantity = 0,
                Unit = "pcs",
                CostPricePerUnit = dto.UnitPrice,
                SellingPricePerUnit = dto.UnitPrice * 1.2m,
                CreatedAt = DateTime.UtcNow
            };
            _context.Parts.Add(part);
            await _context.SaveChangesAsync();
        }

        // Create a PurchaseOrder
        var purchaseOrder = new PurchaseOrder
        {
            VendorId = dto.VendorId,
            OrderDate = DateTime.UtcNow,
            InvoiceNumber = $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Status = PurchaseOrderStatus.SentToVendor,
            SentToVendorAt = DateTime.UtcNow,
            PurchaseOrderItems = new List<PurchaseOrderItem>()
        };

        var orderItem = new PurchaseOrderItem
        {
            PartId = part.PartId,
            Quantity = dto.Quantity,
            PricePerUnit = dto.UnitPrice
        };
        purchaseOrder.PurchaseOrderItems.Add(orderItem);
        purchaseOrder.TotalAmount = dto.Quantity * dto.UnitPrice;

        // Add a log to the PurchaseOrder
        purchaseOrder.Logs.Add(new PurchaseOrderLog
        {
            Action = "SentToVendor",
            FromStatus = PurchaseOrderStatus.Draft,
            ToStatus = PurchaseOrderStatus.SentToVendor,
            Notes = dto.Notes ?? $"Purchase order created automatically from part request #{partRequestId}.",
            EmailSentToVendor = false,
            CreatedAt = DateTime.UtcNow
        });

        _context.PurchaseOrders.Add(purchaseOrder);

        // Update the PartRequest
        partRequest.Status = PartRequestStatus.Ordered;
        partRequest.ReviewedByStaffId = adminUserId;
        partRequest.ReviewedAt = DateTime.UtcNow;
        partRequest.DecisionNote = $"Ordered from vendor. Purchase Order #{purchaseOrder.PurchaseOrderId} created.";
        partRequest.SuggestedPartId = part.PartId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Part request {PartRequestId} ordered successfully. Purchase Order {PurchaseOrderId} created.", partRequestId, purchaseOrder.PurchaseOrderId);

        return MapToDto(partRequest);
    }

    private PartRequestResponseDto MapToDto(PartRequest partRequest) => new()
    {
        PartRequestId = partRequest.PartRequestId,
        PartName = partRequest.PartName,
        Description = partRequest.Description,
        VehicleId = partRequest.VehicleId,
        VehicleNumber = partRequest.Vehicle?.VehicleNumber,
        Status = partRequest.Status.ToString(),
        CreatedAt = partRequest.RequestedDate,
        ReviewedByStaffId = partRequest.ReviewedByStaffId,
        ReviewedAt = partRequest.ReviewedAt,
        DecisionNote = partRequest.DecisionNote,
        SuggestedPartId = partRequest.SuggestedPartId
    };
}
