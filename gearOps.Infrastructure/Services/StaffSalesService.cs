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

public class StaffSalesService : IStaffSalesService
{
    private readonly AppDbContext _db;
    private readonly ILogger<StaffSalesService> _logger;
    private readonly IEmailService _emailService;

    public StaffSalesService(AppDbContext db, ILogger<StaffSalesService> logger, IEmailService emailService)
    {
        _db = db;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<SalesInvoiceResponseDto> CreateSalesInvoiceAsync(int staffId, CreateSalesInvoiceDto dto)
    {
        var customer = await _db.Users.FirstOrDefaultAsync(u => u.UserId == dto.CustomerId && !u.IsDeleted)
            ?? throw new NotFoundException($"Customer with ID {dto.CustomerId} not found.");

        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == dto.VehicleId && !v.IsDeleted)
            ?? throw new NotFoundException($"Vehicle with ID {dto.VehicleId} not found.");

        if (dto.AppointmentId.HasValue)
        {
            var appointmentExists = await _db.Appointments.AnyAsync(a => a.AppointmentId == dto.AppointmentId.Value);
            if (!appointmentExists)
                throw new NotFoundException($"Appointment with ID {dto.AppointmentId.Value} not found.");
                
            var existingInvoice = await _db.SalesInvoices.AnyAsync(i => i.AppointmentId == dto.AppointmentId.Value);
            if (existingInvoice)
                throw new BadRequestException($"An invoice has already been created for appointment {dto.AppointmentId.Value}.");
        }

        decimal subTotal = 0;
        var invoiceItems = new List<SalesInvoiceItem>();

        foreach (var item in dto.Items)
        {
            var part = await _db.Parts.FindAsync(item.PartId)
                ?? throw new NotFoundException($"Part with ID {item.PartId} not found.");
            if (part.IsDeleted)
                throw new NotFoundException($"Part with ID {item.PartId} not found.");

            if (item.Quantity <= 0)
                throw new BadRequestException("Quantity must be greater than 0.");

            if (item.PricePerUnit <= 0)
                throw new BadRequestException("Price per unit must be greater than 0.");

            if (part.StockQuantity < item.Quantity)
                throw new BadRequestException($"Insufficient stock for '{part.PartName}'. Available: {part.StockQuantity}, Requested: {item.Quantity}");

            var lineTotal = item.Quantity * item.PricePerUnit;
            subTotal += lineTotal;

            invoiceItems.Add(new SalesInvoiceItem
            {
                PartId = item.PartId,
                Quantity = item.Quantity,
                PricePerUnit = item.PricePerUnit
            });

            // Decrement stock at invoice creation time
            part.StockQuantity -= item.Quantity;
            await NotifyAdminsIfLowStockAsync(part);
        }


        // Discount rule: invoices above 5,000 must receive at least the 10% loyalty discount.
        // The client may send a larger total discount when manual discount is added.
        decimal loyaltyDiscount = subTotal > 5000 ? subTotal * 0.10m : 0;
        decimal manualDiscount = dto.DiscountAmount.HasValue ? dto.DiscountAmount.Value : 0;
        decimal discountAmount = Math.Max(loyaltyDiscount, manualDiscount);
        if (discountAmount < 0)
            throw new BadRequestException("Discount amount cannot be negative.");

        if (discountAmount > subTotal)
            throw new BadRequestException("Discount amount cannot exceed invoice subtotal.");

        decimal finalAmount = subTotal + dto.ServiceCharge - discountAmount;

        var invoice = new SalesInvoice
        {
            CustomerId = dto.CustomerId,
            StaffId = staffId,
            VehicleId = dto.VehicleId,
            AppointmentId = dto.AppointmentId,
            InvoiceType = dto.InvoiceType,
            InvoiceDate = DateTime.UtcNow,
            SubTotal = subTotal,
            ServiceCharge = dto.ServiceCharge,
            DiscountAmount = discountAmount,
            FinalAmount = finalAmount,
            IsPaid = dto.IsPaid,
            DueDate = dto.IsPaid ? null : dto.DueDate ?? DateTime.UtcNow.AddDays(30)
        };

        _db.SalesInvoices.Add(invoice);

        if (!dto.IsPaid)
        {
            if (customer.CreditsRemaining >= finalAmount)
            {
                // Customer has enough store credit to cover the entire invoice
                customer.CreditsRemaining -= finalAmount;
                invoice.IsPaid = true; // Auto-paid via store credit!
                invoice.DueDate = null;
            }
            else
            {
                // Credits can go negative — means customer owes money
                customer.CreditsRemaining -= finalAmount;
            }
        }

        await _db.SaveChangesAsync();

        // Attach line items now that we have the invoice ID
        foreach (var item in invoiceItems)
            item.SalesInvoiceId = invoice.SalesInvoiceId;

        _db.SalesInvoiceItems.AddRange(invoiceItems);

        // Record loyalty discount after the invoice has a database ID.
        if (loyaltyDiscount > 0)
        {
            _db.LoyaltyDiscounts.Add(new LoyaltyDiscount
            {
                SalesInvoiceId = invoice.SalesInvoiceId,
                CustomerId = dto.CustomerId,
                PurchaseAmount = subTotal,
                DiscountApplied = loyaltyDiscount
            });
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Sales invoice {InvoiceId} created by staff {StaffId} for customer {CustomerId} (type={Type})",
            invoice.SalesInvoiceId, staffId, dto.CustomerId, dto.InvoiceType ?? "n/a");

        return await GetSalesInvoiceByIdAsync(invoice.SalesInvoiceId)
            ?? throw new Exception("Failed to retrieve created invoice.");
    }

    public async Task<SalesInvoiceResponseDto?> GetSalesInvoiceByIdAsync(int invoiceId)
    {
        var invoice = await _db.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Vehicle)
            .Include(i => i.SalesInvoiceItems)
                .ThenInclude(ii => ii.Part)
            .FirstOrDefaultAsync(i => i.SalesInvoiceId == invoiceId);

        return invoice == null ? null : MapToDto(invoice);
    }

    public async Task<List<SalesInvoiceResponseDto>> GetAllSalesInvoicesAsync()
    {
        var invoices = await _db.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Vehicle)
            .Include(i => i.SalesInvoiceItems)
                .ThenInclude(ii => ii.Part)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        return invoices.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<SalesInvoiceResponseDto>> GetAllSalesInvoicesAsync(PaginationParams paging, string? search = null)
    {
        var query = _db.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.Vehicle)
            .Include(i => i.SalesInvoiceItems)
                .ThenInclude(ii => ii.Part)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(i =>
                (i.Customer != null && i.Customer.FullName.ToLower().Contains(searchLower)) ||
                (i.Customer != null && i.Customer.Email.ToLower().Contains(searchLower)) ||
                (i.Vehicle != null && i.Vehicle.VehicleNumber.ToLower().Contains(searchLower)) ||
                i.SalesInvoiceId.ToString() == searchLower ||
                (i.InvoiceType != null && i.InvoiceType.ToLower().Contains(searchLower))
            );
        }

        var ordered = query.OrderByDescending(i => i.InvoiceDate)
            .Select(i => MapToDto(i));

        return await ordered.ToPagedResultAsync(paging);
    }

    public async Task<bool> MarkInvoicePaidAsync(int invoiceId)
    {
        var invoice = await _db.SalesInvoices
            .Include(i => i.Customer)
            .FirstOrDefaultAsync(i => i.SalesInvoiceId == invoiceId)
            ?? throw new NotFoundException($"Invoice with ID {invoiceId} not found.");

        if (invoice.IsPaid)
            throw new BadRequestException("Invoice is already marked as paid.");

        invoice.IsPaid = true;

        // Customer pays the invoice, so their balance increases back
        invoice.Customer.CreditsRemaining += invoice.FinalAmount;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SendInvoiceEmailAsync(int invoiceId)
    {
        var invoice = await _db.SalesInvoices
            .Include(i => i.Customer)
            .Include(i => i.SalesInvoiceItems)
                .ThenInclude(ii => ii.Part)
            .FirstOrDefaultAsync(i => i.SalesInvoiceId == invoiceId)
            ?? throw new NotFoundException($"Invoice with ID {invoiceId} not found.");

                var subject = $"GearOps Invoice #{invoice.SalesInvoiceId}";
                var statusLabel = invoice.IsPaid ? "Paid" : "Unpaid";
                var statusColor = invoice.IsPaid ? "#16a34a" : "#dc2626";
                var body = $@"
<div style='font-family: Arial, sans-serif; max-width: 640px; margin: 0 auto; padding: 24px; color: #111827;'>
    <div style='border: 1px solid #e5e7eb; border-radius: 12px; overflow: hidden;'>
        <div style='background: #0f172a; color: #ffffff; padding: 16px 20px;'>
            <h2 style='margin: 0; font-size: 20px;'>GearOps Invoice</h2>
            <p style='margin: 6px 0 0; font-size: 13px; opacity: 0.8;'>Invoice #{invoice.SalesInvoiceId}</p>
        </div>
        <div style='padding: 20px;'>
            <p style='margin: 0 0 8px;'>Dear {invoice.Customer.FullName},</p>
            <p style='margin: 0 0 16px;'>Thank you for choosing GearOps. Here is your invoice summary:</p>

            <div style='display: flex; gap: 16px; flex-wrap: wrap; margin-bottom: 16px;'>
                <div style='flex: 1; min-width: 180px; padding: 12px; background: #f8fafc; border: 1px solid #e5e7eb; border-radius: 10px;'>
                    <div style='font-size: 12px; color: #6b7280;'>Total Amount</div>
                    <div style='font-size: 20px; font-weight: 700; color: #0f172a;'>${invoice.FinalAmount:F2}</div>
                </div>
                <div style='flex: 1; min-width: 180px; padding: 12px; background: #f8fafc; border: 1px solid #e5e7eb; border-radius: 10px;'>
                    <div style='font-size: 12px; color: #6b7280;'>Payment Status</div>
                    <div style='font-size: 16px; font-weight: 700; color: {statusColor};'>{statusLabel}</div>
                </div>
            </div>

            <div style='border-top: 1px solid #e5e7eb; padding-top: 12px; font-size: 13px; color: #4b5563;'>
                <p style='margin: 0;'>If you have any questions about this invoice, please contact our support team.</p>
            </div>
        </div>
    </div>
    <p style='margin: 12px 0 0; font-size: 11px; color: #9ca3af;'>This is an automated email from GearOps. Please do not reply.</p>
</div>";

        await _emailService.SendEmailAsync(invoice.Customer.Email, subject, body);
        _logger.LogInformation("Invoice email sent for invoice {InvoiceId} to {Email}", invoiceId, invoice.Customer.Email);

        return true;
    }

    private async Task NotifyAdminsIfLowStockAsync(Part part, int threshold = 10)
    {
        if (part.StockQuantity >= threshold)
            return;

        var subject = $"Low stock: {part.PartName} (#{part.PartId})";
        var alreadyOpen = await _db.Notifications.AnyAsync(n =>
            n.LogType == "LowStock" &&
            n.Subject == subject &&
            !n.IsRead);

        if (alreadyOpen)
            return;

        var adminIds = await _db.Users
            .Where(u => u.Role == Role.Admin)
            .Select(u => u.UserId)
            .ToListAsync();

        foreach (var adminId in adminIds)
        {
            _db.Notifications.Add(new Notification
            {
                UserId = adminId,
                LogType = "LowStock",
                Subject = subject,
                Message = $"{part.PartName} has {part.StockQuantity} {part.Unit} remaining after invoice stock deduction. Reorder threshold is {threshold}.",
                MailedStatus = NotificationStatus.Pending
            });
        }
    }

    private static SalesInvoiceResponseDto MapToDto(SalesInvoice invoice) =>
        new()
        {
            SalesInvoiceId = invoice.SalesInvoiceId,
            CustomerId = invoice.CustomerId,
            CustomerName = invoice.Customer?.FullName ?? string.Empty,
            CustomerEmail = invoice.Customer?.Email ?? string.Empty,
            VehicleId = invoice.VehicleId,
            VehicleNumber = invoice.Vehicle?.VehicleNumber ?? string.Empty,
            AppointmentId = invoice.AppointmentId,
            InvoiceType = invoice.InvoiceType,
            InvoiceDate = invoice.InvoiceDate,
            SubTotal = invoice.SubTotal,
            ServiceCharge = invoice.ServiceCharge,
            DiscountAmount = invoice.DiscountAmount,
            FinalAmount = invoice.FinalAmount,
            IsPaid = invoice.IsPaid,
            DueDate = invoice.DueDate,
            Items = invoice.SalesInvoiceItems?.Select(ii => new SalesInvoiceItemResponseDto
            {
                PartId = ii.PartId,
                PartName = ii.Part?.PartName ?? string.Empty,
                Quantity = ii.Quantity,
                PricePerUnit = ii.PricePerUnit,
                TotalPrice = ii.Quantity * ii.PricePerUnit
            }).ToList() ?? new()
        };
}
