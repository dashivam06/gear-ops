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
using gearOps.Infrastructure.Data;

namespace gearOps.Infrastructure.Services;

public class PurchaseHistoryService : IPurchaseHistoryService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PurchaseHistoryService> _logger;

    public PurchaseHistoryService(AppDbContext context, ILogger<PurchaseHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SalesInvoiceDetailDto?> GetInvoiceDetailsAsync(int invoiceId)
    {
        var invoice = await _context.SalesInvoices
            .Include(s => s.Vehicle)
            .Include(s => s.SalesInvoiceItems)
            .ThenInclude(item => item.Part)
            .FirstOrDefaultAsync(s => s.SalesInvoiceId == invoiceId);

        return invoice == null ? null : MapInvoiceToDto(invoice);
    }

    public async Task<List<SalesInvoiceDetailDto>> GetCustomerPurchaseHistoryAsync(int userId)
    {
        var invoices = await _context.SalesInvoices
            .Where(s => s.CustomerId == userId)
            .Include(s => s.Vehicle)
            .Include(s => s.SalesInvoiceItems)
            .ThenInclude(item => item.Part)
            .OrderByDescending(s => s.InvoiceDate)
            .ToListAsync();

        return invoices.Select(MapInvoiceToDto).ToList();
    }

    public async Task<List<ServiceRecordResponseDto>> GetCustomerServiceHistoryAsync(int userId)
    {
        var serviceRecords = await _context.ServiceRecords
            .Where(sr => sr.Vehicle.CustomerId == userId)
            .Include(sr => sr.Vehicle)
            .Include(sr => sr.Appointment)
            .OrderByDescending(sr => sr.ServiceDate)
            .ToListAsync();

        return serviceRecords.Select(sr => MapServiceRecordToDto(sr)).ToList();
    }

    public async Task<CustomerHistorySummaryDto> GetCustomerHistorySummaryAsync(int userId)
    {
        var purchaseHistory = await GetCustomerPurchaseHistoryAsync(userId);
        var serviceHistory = await GetCustomerServiceHistoryAsync(userId);

        var totalSpent = purchaseHistory.Sum(p => p.TotalAmount);
        var totalServices = serviceHistory.Count;

        return new CustomerHistorySummaryDto
        {
            TotalPurchases = purchaseHistory.Count,
            TotalSpent = totalSpent,
            TotalServices = totalServices,
            PurchaseHistory = purchaseHistory,
            ServiceHistory = serviceHistory
        };
    }

    private SalesInvoiceDetailDto MapInvoiceToDto(SalesInvoice invoice) => new()
    {
        SalesInvoiceId = invoice.SalesInvoiceId,
        VehicleId = invoice.VehicleId,
        VehicleNumber = invoice.Vehicle.VehicleNumber,
        InvoiceDate = invoice.InvoiceDate,
        TotalAmount = invoice.FinalAmount,
        PaymentStatus = invoice.IsPaid ? "Paid" : "Unpaid",
        Items = invoice.SalesInvoiceItems.Select(item => new SalesInvoiceItemDetailDto
        {
            PartId = item.PartId,
            PartName = item.Part.PartName,
            Quantity = item.Quantity,
            PricePerUnit = item.PricePerUnit,
            TotalPrice = item.Quantity * item.PricePerUnit
        }).ToList()
    };

    private ServiceRecordResponseDto MapServiceRecordToDto(ServiceRecord sr) => new()
    {
        ServiceRecordId = sr.ServiceRecordId,
        AppointmentId = sr.AppointmentId,
        VehicleId = sr.VehicleId,
        VehicleNumber = sr.Vehicle.VehicleNumber,
        ServiceDate = sr.ServiceDate,
        Description = sr.ServiceDescription,
        IssueReported = sr.Appointment.Remarks,
        StaffAnswer = sr.Appointment.ApprovalNotes,
        Status = sr.Status.ToString(),
        Cost = sr.ServiceCost,
        CreatedAt = sr.ServiceDate
    };

    public async Task<SalesInvoiceDetailDto> BuyPartsDirectlyAsync(int userId, DirectBuyPartsDto dto)
    {
        var customer = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted)
            ?? throw new NotFoundException($"Customer with ID {userId} not found.");

        if (dto.VehicleId.HasValue)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == dto.VehicleId && !v.IsDeleted && v.CustomerId == userId)
                ?? throw new NotFoundException($"Vehicle with ID {dto.VehicleId} not found for this customer.");
        }

        // We need a staff ID for the invoice. Let's use the first Admin in the system.
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == Domain.Enums.Role.Admin && !u.IsDeleted);
        var staffId = admin?.UserId ?? userId; // Fallback to user if no admin exists for some reason

        decimal subTotal = 0;
        var invoiceItems = new List<SalesInvoiceItem>();

        foreach (var item in dto.Items)
        {
            var part = await _context.Parts.FindAsync(item.PartId)
                ?? throw new NotFoundException($"Part with ID {item.PartId} not found.");
            
            if (part.IsDeleted)
                throw new NotFoundException($"Part with ID {item.PartId} not found.");

            if (item.Quantity <= 0)
                throw new BadRequestException("Quantity must be greater than 0.");

            if (part.StockQuantity < item.Quantity)
                throw new BadRequestException($"Insufficient stock for '{part.PartName}'. Available: {part.StockQuantity}, Requested: {item.Quantity}");

            var lineTotal = item.Quantity * part.SellingPricePerUnit;
            subTotal += lineTotal;

            invoiceItems.Add(new SalesInvoiceItem
            {
                PartId = item.PartId,
                Quantity = item.Quantity,
                PricePerUnit = part.SellingPricePerUnit
            });

            part.StockQuantity -= item.Quantity;
        }

        var invoice = new SalesInvoice
        {
            CustomerId = userId,
            StaffId = staffId,
            VehicleId = dto.VehicleId ?? 0, // In db VehicleId might be required, check if VehicleId is required. Assuming 0 if not provided or customer has default vehicle
            InvoiceType = "Parts",
            InvoiceDate = DateTime.UtcNow,
            SubTotal = subTotal,
            DiscountAmount = 0,
            FinalAmount = subTotal,
            IsPaid = false,
            DueDate = DateTime.UtcNow.AddDays(30)
        };

        if (dto.VehicleId.HasValue) {
            invoice.VehicleId = dto.VehicleId.Value;
        } else {
             // Assign the first vehicle if not provided and exists
            var firstVehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.CustomerId == userId);
            if (firstVehicle != null) {
                invoice.VehicleId = firstVehicle.VehicleId;
            }
        }

        _context.SalesInvoices.Add(invoice);
        
        if (customer.CreditsRemaining >= subTotal)
        {
            // Customer has enough store credit to cover the entire invoice
            customer.CreditsRemaining -= subTotal;
            invoice.IsPaid = true; // Auto-paid via store credit!
            invoice.DueDate = null;
        }
        else
        {
            // Subtract from customer credit balance (can go negative)
            customer.CreditsRemaining -= subTotal;
        }
        
        await _context.SaveChangesAsync();

        foreach (var item in invoiceItems)
            item.SalesInvoiceId = invoice.SalesInvoiceId;

        _context.SalesInvoiceItems.AddRange(invoiceItems);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Customer {UserId} directly bought parts via SalesInvoice {InvoiceId}", userId, invoice.SalesInvoiceId);

        var savedInvoice = await _context.SalesInvoices
            .Include(s => s.Vehicle)
            .Include(s => s.SalesInvoiceItems)
            .ThenInclude(item => item.Part)
            .FirstOrDefaultAsync(s => s.SalesInvoiceId == invoice.SalesInvoiceId);

        return MapInvoiceToDto(savedInvoice!);
    }
}

