using System;

namespace gearOps.Domain.Entities;

public class LoyaltyDiscount
{
    public int LoyaltyId { get; set; }
    public int SalesInvoiceId { get; set; }
    public int CustomerId { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal DiscountApplied { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SalesInvoice SalesInvoice { get; set; } = null!;
    public User Customer { get; set; } = null!;
}
