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

public class LoyaltyProgramService : ILoyaltyProgramService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LoyaltyProgramService> _logger;

    // Loyalty tiers: 5000 = 10% discount, 10000 = 15% discount, 20000+ = 20% discount
    private const decimal Tier1Threshold = 5000m;
    private const decimal Tier2Threshold = 10000m;
    private const decimal Tier3Threshold = 20000m;

    private const decimal Tier1Discount = 0.10m;
    private const decimal Tier2Discount = 0.15m;
    private const decimal Tier3Discount = 0.20m;

    public LoyaltyProgramService(AppDbContext context, ILogger<LoyaltyProgramService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LoyaltyStatusDto> GetLoyaltyStatusAsync(int userId)
    {
        var invoices = await _context.SalesInvoices
            .Where(s => s.CustomerId == userId)
            .Include(s => s.SalesInvoiceItems)
            .ToListAsync();

        var totalSpent = invoices.Sum(s => s.FinalAmount);

        var discountPercentage = CalculateDiscount(totalSpent) * 100;
        var currentTier = GetCurrentTier(totalSpent);
        var nextTierThreshold = GetNextTierThreshold(totalSpent);
        var remainingForNextTier = Math.Max(0, nextTierThreshold - totalSpent);

        var recentTransactions = invoices
            .OrderByDescending(s => s.InvoiceDate)
            .Take(5)
            .Select(s => new LoyaltyTransactionDto
            {
                TransactionId = s.SalesInvoiceId,
                Amount = s.FinalAmount,
                DiscountApplied = s.DiscountAmount,
                TransactionDate = s.InvoiceDate
            }).ToList();

        return new LoyaltyStatusDto
        {
            TotalSpent = totalSpent,
            TransactionCount = invoices.Count,
            DiscountPercentage = discountPercentage,
            EligibleForNextTier = remainingForNextTier > 0,
            RemainingForNextTier = remainingForNextTier,
            CurrentTier = currentTier,
            RecentTransactions = recentTransactions
        };
    }

    public decimal CalculateDiscount(decimal purchaseAmount)
    {
        if (purchaseAmount >= Tier3Threshold)
            return Tier3Discount;
        if (purchaseAmount >= Tier2Threshold)
            return Tier2Discount;
        if (purchaseAmount >= Tier1Threshold)
            return Tier1Discount;

        return 0;
    }

    public async Task ApplyLoyaltyDiscountAsync(int userId, int invoiceId)
    {
        try
        {
            var invoice = await _context.SalesInvoices
                .FirstOrDefaultAsync(s => s.SalesInvoiceId == invoiceId && s.CustomerId == userId);

            if (invoice == null)
                throw new Exception("Invoice not found or does not belong to this customer.");

            var status = await GetLoyaltyStatusAsync(userId);
            var discountAmount = invoice.FinalAmount * (decimal)(status.DiscountPercentage / 100);

            // Apply discount to user credits
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.CreditsRemaining += discountAmount;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Loyalty discount applied to user {userId}: {discountAmount}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying loyalty discount");
            throw;
        }
    }

    private string GetCurrentTier(decimal totalSpent)
    {
        if (totalSpent >= Tier3Threshold)
            return "Premium (20% discount)";
        if (totalSpent >= Tier2Threshold)
            return "Gold (15% discount)";
        if (totalSpent >= Tier1Threshold)
            return "Silver (10% discount)";

        return "Bronze (0% discount)";
    }

    private decimal GetNextTierThreshold(decimal totalSpent)
    {
        if (totalSpent >= Tier3Threshold)
            return Tier3Threshold + 5000; // Next milestone

        if (totalSpent >= Tier2Threshold)
            return Tier3Threshold;

        if (totalSpent >= Tier1Threshold)
            return Tier2Threshold;

        return Tier1Threshold;
    }
}
