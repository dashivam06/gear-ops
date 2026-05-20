using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using gearOps.Application.DTOs;
using gearOps.Application.Exceptions;
using gearOps.Application.Interfaces;
using gearOps.Domain.Entities;
using gearOps.Domain.Enums;
using gearOps.Infrastructure.Data;

namespace gearOps.Infrastructure.Services;

public class PurchaseInvoiceService : IPurchaseInvoiceService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PurchaseInvoiceService> _logger;
    private readonly IEmailService _emailService;

    public PurchaseInvoiceService(AppDbContext context, ILogger<PurchaseInvoiceService> logger, IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<PurchaseOrderResponseDto> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto)
    {
        try
        {
            var vendor = await GetActiveVendorAsync(dto.VendorId);
            await ValidateInvoiceNumberAsync(dto.InvoiceNumber, null);

            var purchaseOrder = new PurchaseOrder
            {
                VendorId = dto.VendorId,
                OrderDate = DateTime.UtcNow,
                InvoiceNumber = NormalizeInvoiceNumber(dto.InvoiceNumber) ?? $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Status = PurchaseOrderStatus.Draft,
                DeliveredAt = null,
                PurchaseOrderItems = new List<PurchaseOrderItem>()
            };

            await ReplaceItemsAsync(purchaseOrder, dto.Items, dto.VendorId);
            purchaseOrder.TotalAmount = CalculateTotal(purchaseOrder.PurchaseOrderItems);

            _context.PurchaseOrders.Add(purchaseOrder);
            AddLog(purchaseOrder, "Created", null, PurchaseOrderStatus.Draft, "Purchase order draft created.", false);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Purchase order draft created: {PurchaseOrderId} for vendor {VendorId}", purchaseOrder.PurchaseOrderId, vendor.VendorId);

            return await GetRequiredPurchaseOrderDtoAsync(purchaseOrder.PurchaseOrderId);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error creating purchase order");
            throw;
        }
    }

    public async Task<PurchaseOrderResponseDto> UpdatePurchaseOrderAsync(int purchaseOrderId, UpdatePurchaseOrderDto dto)
    {
        try
        {
            var order = await GetOrderForUpdateAsync(purchaseOrderId);
            EnsureEditable(order);
            await ValidateInvoiceNumberAsync(dto.InvoiceNumber, purchaseOrderId);

            var itemsChanged = !ItemsMatch(order.PurchaseOrderItems, dto.Items);
            order.InvoiceNumber = NormalizeInvoiceNumber(dto.InvoiceNumber);
            await ReplaceItemsAsync(order, dto.Items, order.VendorId);
            order.TotalAmount = CalculateTotal(order.PurchaseOrderItems);

            var emailSent = false;
            if (itemsChanged && order.Status is PurchaseOrderStatus.SentToVendor or PurchaseOrderStatus.ConfirmedByVendor)
            {
                emailSent = await SendVendorEmailAsync(
                    order,
                    "Purchase order updated",
                    "The purchase order items were updated. Please review the latest requested items.");
            }

            AddLog(order, "Updated", order.Status, order.Status, dto.Notes ?? "Purchase order updated.", emailSent);
            await _context.SaveChangesAsync();

            return await GetRequiredPurchaseOrderDtoAsync(purchaseOrderId);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error updating purchase order {PurchaseOrderId}", purchaseOrderId);
            throw;
        }
    }

    public async Task<PurchaseOrderResponseDto> SendPurchaseOrderToVendorAsync(int purchaseOrderId, SendPurchaseOrderToVendorDto dto)
    {
        var order = await GetOrderForUpdateAsync(purchaseOrderId);
        EnsureEditable(order);

        var emailSent = await SendVendorEmailAsync(
            order,
            "New purchase order request",
            dto.Message ?? "Please review this purchase order request and send the invoice number after confirmation.");

        var previousStatus = order.Status;
        if (order.Status == PurchaseOrderStatus.Draft)
        {
            order.Status = PurchaseOrderStatus.SentToVendor;
            order.SentToVendorAt = DateTime.UtcNow;
        }

        AddLog(order, "SentToVendor", previousStatus, order.Status, dto.Message ?? "Purchase order sent to vendor.", emailSent);
        await _context.SaveChangesAsync();

        return await GetRequiredPurchaseOrderDtoAsync(purchaseOrderId);
    }

    public async Task<PurchaseOrderResponseDto> ConfirmPurchaseOrderAsync(int purchaseOrderId, ConfirmPurchaseOrderDto dto)
    {
        var order = await GetOrderForUpdateAsync(purchaseOrderId);
        EnsureEditable(order);
        await ValidateInvoiceNumberAsync(dto.InvoiceNumber, purchaseOrderId);

        var previousStatus = order.Status;
        order.InvoiceNumber = NormalizeInvoiceNumber(dto.InvoiceNumber);
        order.Status = PurchaseOrderStatus.ConfirmedByVendor;
        order.ConfirmedAt = DateTime.UtcNow;

        AddLog(order, "ConfirmedByVendor", previousStatus, order.Status, dto.Notes ?? "Vendor invoice received and purchase order confirmed.", false);
        await _context.SaveChangesAsync();

        return await GetRequiredPurchaseOrderDtoAsync(purchaseOrderId);
    }

    public async Task<PurchaseOrderResponseDto> MarkPurchaseOrderDeliveredAsync(int purchaseOrderId, DeliverPurchaseOrderDto dto)
    {
        var order = await GetOrderForUpdateAsync(purchaseOrderId);
        if (order.Status == PurchaseOrderStatus.Delivered)
            throw new BadRequestException("Purchase order is already delivered.");

        if (string.IsNullOrWhiteSpace(order.InvoiceNumber))
            throw new BadRequestException("Invoice number is required before marking the purchase order as delivered.");

        var previousStatus = order.Status;
        foreach (var item in order.PurchaseOrderItems)
        {
            item.Part.StockQuantity += item.Quantity;
        }

        order.Status = PurchaseOrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;
        AddLog(order, "Delivered", previousStatus, order.Status, dto.Notes ?? "Purchase order delivered. Inventory stock updated.", false);
        await _context.SaveChangesAsync();

        return await GetRequiredPurchaseOrderDtoAsync(purchaseOrderId);
    }

    public async Task<PurchaseOrderResponseDto?> GetPurchaseOrderByIdAsync(int purchaseOrderId)
    {
        var order = await BuildReadQuery()
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId);

        return order == null ? null : MapToDto(order);
    }

    public async Task<List<PurchaseOrderResponseDto>> GetPurchaseOrdersByVendorAsync(int vendorId)
    {
        _ = await _context.Vendors.FindAsync(vendorId)
            ?? throw new NotFoundException($"Vendor with ID {vendorId} not found.");

        var orders = await BuildReadQuery()
            .Where(p => p.VendorId == vendorId)
            .ToListAsync();

        return orders.Select(MapToDto).ToList();
    }

    public async Task<List<PurchaseOrderResponseDto>> GetAllPurchaseOrdersAsync()
    {
        var orders = await BuildReadQuery().ToListAsync();
        return orders.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<PurchaseOrderResponseDto>> GetAllPurchaseOrdersAsync(PaginationParams paging)
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.PurchaseOrderItems)
            .ThenInclude(i => i.Part)
            .Include(p => p.Logs)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(paging.Search))
        {
            var search = paging.Search.ToLower();
            query = query.Where(p => 
                p.PurchaseOrderId.ToString().Contains(search) ||
                (p.InvoiceNumber != null && p.InvoiceNumber.ToLower().Contains(search)) ||
                (p.Vendor.VendorName != null && p.Vendor.VendorName.ToLower().Contains(search))
            );
        }

        query = query.OrderByDescending(p => p.OrderDate);

        var totalItems = await query.CountAsync();
        var orders = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync();

        return new PagedResult<PurchaseOrderResponseDto>
        {
            Items = orders.Select(MapToDto).ToList(),
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)paging.PageSize)
        };
    }

    private IQueryable<PurchaseOrder> BuildReadQuery() =>
        _context.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.PurchaseOrderItems)
            .ThenInclude(i => i.Part)
            .Include(p => p.Logs)
            .OrderByDescending(p => p.OrderDate);

    private async Task<PurchaseOrder> GetOrderForUpdateAsync(int purchaseOrderId) =>
        await _context.PurchaseOrders
            .Include(p => p.Vendor)
            .Include(p => p.PurchaseOrderItems)
            .ThenInclude(i => i.Part)
            .Include(p => p.Logs)
            .FirstOrDefaultAsync(p => p.PurchaseOrderId == purchaseOrderId)
        ?? throw new NotFoundException($"Purchase order with ID {purchaseOrderId} not found.");

    private async Task<Vendor> GetActiveVendorAsync(int vendorId) =>
        await _context.Vendors.FirstOrDefaultAsync(v => v.VendorId == vendorId && !v.IsDeleted)
        ?? throw new NotFoundException($"Vendor with ID {vendorId} not found.");

    private async Task ValidateInvoiceNumberAsync(string? invoiceNumber, int? currentPurchaseOrderId)
    {
        var normalized = NormalizeInvoiceNumber(invoiceNumber);
        if (normalized == null)
            return;

        var exists = await _context.PurchaseOrders.AnyAsync(p =>
            p.InvoiceNumber == normalized &&
            (!currentPurchaseOrderId.HasValue || p.PurchaseOrderId != currentPurchaseOrderId.Value));

        if (exists)
            throw new ConflictException($"Invoice number '{normalized}' already exists.");
    }

    private async Task ReplaceItemsAsync(PurchaseOrder order, List<PurchaseOrderItemDto> items, int vendorId)
    {
        if (items == null || items.Count == 0)
            throw new BadRequestException("Purchase order must contain at least one item.");

        var newItems = new List<PurchaseOrderItem>();
        foreach (var item in items)
        {
            var part = await _context.Parts.FindAsync(item.PartId)
                ?? throw new NotFoundException($"Part with ID {item.PartId} not found.");
            if (part.IsDeleted)
                throw new NotFoundException($"Part with ID {item.PartId} not found.");

            if (part.VendorId != vendorId)
                throw new BadRequestException($"Part with ID {item.PartId} does not belong to vendor {vendorId}.");

            if (item.Quantity <= 0)
                throw new BadRequestException("Quantity must be greater than 0.");

            if (item.UnitPrice <= 0)
                throw new BadRequestException("Unit price must be greater than 0.");

            newItems.Add(new PurchaseOrderItem
            {
                PurchaseOrderId = order.PurchaseOrderId,
                PartId = item.PartId,
                Part = part,
                Quantity = item.Quantity,
                PricePerUnit = item.UnitPrice
            });
        }

        order.PurchaseOrderItems.Clear();
        foreach (var item in newItems)
        {
            order.PurchaseOrderItems.Add(item);
        }
    }

    private async Task<bool> SendVendorEmailAsync(PurchaseOrder order, string subjectStatus, string message)
    {
        if (string.IsNullOrWhiteSpace(order.Vendor.Email))
        {
            _logger.LogWarning("Vendor email is missing for vendor {VendorId}. Email was not sent.", order.VendorId);
            return false;
        }

        try
        {
            await _emailService.SendPurchaseOrderEmailAsync(
                order.Vendor.Email,
                order.Vendor.VendorName,
                order.PurchaseOrderId,
                subjectStatus,
                message,
                BuildItemsSummary(order));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send purchase order email for PO {PurchaseOrderId} to vendor {VendorId} ({Email})", 
                order.PurchaseOrderId, order.VendorId, order.Vendor.Email);
            return false;
        }
    }

    private static void EnsureEditable(PurchaseOrder order)
    {
        if (order.Status == PurchaseOrderStatus.Delivered)
            throw new BadRequestException("Delivered purchase orders cannot be edited.");

        if (order.Status == PurchaseOrderStatus.Cancelled)
            throw new BadRequestException("Cancelled purchase orders cannot be edited.");
    }

    private static bool ItemsMatch(IEnumerable<PurchaseOrderItem> existingItems, IEnumerable<PurchaseOrderItemDto> incomingItems)
    {
        var existing = existingItems
            .Select(i => (i.PartId, i.Quantity, i.PricePerUnit))
            .OrderBy(i => i.PartId)
            .ThenBy(i => i.Quantity)
            .ThenBy(i => i.PricePerUnit)
            .ToList();

        var incoming = incomingItems
            .Select(i => (i.PartId, i.Quantity, PricePerUnit: i.UnitPrice))
            .OrderBy(i => i.PartId)
            .ThenBy(i => i.Quantity)
            .ThenBy(i => i.PricePerUnit)
            .ToList();

        return existing.SequenceEqual(incoming);
    }

    private static decimal CalculateTotal(IEnumerable<PurchaseOrderItem> items) =>
        items.Sum(i => i.Quantity * i.PricePerUnit);

    private static string? NormalizeInvoiceNumber(string? invoiceNumber) =>
        string.IsNullOrWhiteSpace(invoiceNumber) ? null : invoiceNumber.Trim();

    private static void AddLog(
        PurchaseOrder order,
        string action,
        PurchaseOrderStatus? fromStatus,
        PurchaseOrderStatus? toStatus,
        string? notes,
        bool emailSent)
    {
        order.Logs.Add(new PurchaseOrderLog
        {
            Action = action,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Notes = notes,
            EmailSentToVendor = emailSent,
            CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<PurchaseOrderResponseDto> GetRequiredPurchaseOrderDtoAsync(int purchaseOrderId) =>
        await GetPurchaseOrderByIdAsync(purchaseOrderId)
        ?? throw new Exception("Failed to retrieve purchase order.");

    private static string BuildItemsSummary(PurchaseOrder order)
    {
        var builder = new StringBuilder();
        foreach (var item in order.PurchaseOrderItems)
        {
            builder.AppendLine($"{item.Part.PartName}: {item.Quantity} x {item.PricePerUnit:C} = {(item.Quantity * item.PricePerUnit):C}");
        }

        builder.AppendLine($"Total: {order.TotalAmount:C}");
        return builder.ToString();
    }

    private static PurchaseOrderResponseDto MapToDto(PurchaseOrder order) => new()
    {
        PurchaseOrderId = order.PurchaseOrderId,
        VendorId = order.VendorId,
        VendorName = order.Vendor.VendorName,
        OrderDate = order.OrderDate,
        InvoiceNumber = order.InvoiceNumber,
        TotalAmount = order.TotalAmount,
        Status = order.Status,
        StatusText = order.Status.ToString(),
        IsEditable = order.Status is not PurchaseOrderStatus.Delivered and not PurchaseOrderStatus.Cancelled,
        SentToVendorAt = order.SentToVendorAt,
        ConfirmedAt = order.ConfirmedAt,
        DeliveredAt = order.DeliveredAt,
        Items = order.PurchaseOrderItems.Select(i => new PurchaseOrderItemResponseDto
        {
            PartId = i.PartId,
            PartName = i.Part.PartName,
            Quantity = i.Quantity,
            UnitPrice = i.PricePerUnit,
            TotalPrice = i.Quantity * i.PricePerUnit
        }).ToList(),
        Logs = order.Logs
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new PurchaseOrderLogResponseDto
            {
                PurchaseOrderLogId = l.PurchaseOrderLogId,
                Action = l.Action,
                FromStatus = l.FromStatus?.ToString(),
                ToStatus = l.ToStatus?.ToString(),
                Notes = l.Notes,
                EmailSentToVendor = l.EmailSentToVendor,
                CreatedAt = l.CreatedAt
            })
            .ToList()
    };
}
