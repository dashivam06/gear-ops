using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using gearOps.Application.DTOs;
using gearOps.Application.Interfaces;
using gearOps.Infrastructure.Data;

namespace gearOps.Infrastructure.Services;

public class CreditService : ICreditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<CreditService> _logger;

    public CreditService(AppDbContext context, ILogger<CreditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CreditBalanceDto> GetCreditBalanceAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new Exception($"User {userId} not found.");

        var overdueCredits = await GetOverdueCreditsAsync(userId, 30);

        return new CreditBalanceDto
        {
            CreditsRemaining = user.CreditsRemaining,
            OverdueCredits = overdueCredits
        };
    }

    public async Task<List<OverdueCreditDto>> GetOverdueCreditsAsync(int userId, int daysOverdue = 30)
    {
        var now = DateTime.UtcNow;
        var overdueThreshold = now.AddDays(-daysOverdue);

        // Assuming invoices with IsPaid = false and created more than daysOverdue ago are overdue
        var overdueInvoices = await _context.SalesInvoices
            .Where(s => s.CustomerId == userId &&
                       !s.IsPaid &&
                       s.InvoiceDate < overdueThreshold)
            .ToListAsync();

        var overdueList = overdueInvoices.Select(invoice => new OverdueCreditDto
        {
            SalesInvoiceId = invoice.SalesInvoiceId,
            Amount = invoice.FinalAmount,
            DaysOverdue = (int)(now - invoice.InvoiceDate).TotalDays,
            DueDate = invoice.DueDate ?? invoice.InvoiceDate.AddDays(30)
        }).ToList();

        _logger.LogInformation($"Retrieved {overdueList.Count} overdue credits for customer {userId}");

        return overdueList;
    }
}
