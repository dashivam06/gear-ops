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
        VehicleId = sr.VehicleId,
        VehicleNumber = sr.Vehicle.VehicleNumber,
        ServiceDate = sr.ServiceDate,
        Description = sr.ServiceDescription,
        Status = sr.Status.ToString(),
        Cost = sr.ServiceCost,
        CreatedAt = sr.ServiceDate
    };
}

