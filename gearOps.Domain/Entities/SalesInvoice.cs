using System;
using System.Collections.Generic;

namespace gearOps.Domain.Entities;

public class SalesInvoice
{
    public int SalesInvoiceId { get; set; }
    public int CustomerId { get; set; }
    public int StaffId { get; set; }
    public int VehicleId { get; set; }
    /// <summary>Optional link to the appointment this invoice covers.</summary>
    public int? AppointmentId { get; set; }
    /// <summary>"Parts" | "Appointment" | null (legacy).</summary>
    public string? InvoiceType { get; set; }
    public DateTime InvoiceDate { get; set; }
    public decimal SubTotal { get; set; } = 0.00m;
    /// <summary>Labor / service charge (separate from parts). Added to SubTotal for FinalAmount calculation.</summary>
    public decimal ServiceCharge { get; set; } = 0.00m;
    public decimal DiscountAmount { get; set; } = 0.00m;
    public decimal FinalAmount { get; set; } = 0.00m;
    public bool IsPaid { get; set; } = false;
    public DateTime? DueDate { get; set; }

    public User Customer { get; set; } = null!;
    public User Staff { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public Appointment? Appointment { get; set; }
    public ICollection<SalesInvoiceItem> SalesInvoiceItems { get; set; } = new List<SalesInvoiceItem>();
    public ICollection<LoyaltyDiscount> LoyaltyDiscounts { get; set; } = new List<LoyaltyDiscount>();
}
