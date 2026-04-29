using System;
using System.Collections.Generic;

namespace gearOps.Domain.Entities;

public class SalesInvoice
{
    public int SalesInvoiceId { get; set; }
    public int CustomerId { get; set; }
    public int StaffId { get; set; }
    public int VehicleId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public decimal SubTotal { get; set; } = 0.00m;
    public decimal DiscountAmount { get; set; } = 0.00m;
    public decimal FinalAmount { get; set; } = 0.00m;
    public bool IsPaid { get; set; } = false;
    public DateTime? DueDate { get; set; }

    public User Customer { get; set; } = null!;
    public User Staff { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public ICollection<SalesInvoiceItem> SalesInvoiceItems { get; set; } = new List<SalesInvoiceItem>();
    public ICollection<LoyaltyDiscount> LoyaltyDiscounts { get; set; } = new List<LoyaltyDiscount>();
}
