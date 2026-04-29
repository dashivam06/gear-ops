using System;
using System.Collections.Generic;

namespace gearOps.Domain.Entities;

public class Part
{
    public int PartId { get; set; }
    public int VendorId { get; set; }
    public string PartName { get; set; } = null!;
    public string? Description { get; set; }
    public string Category { get; set; } = null!;
    public int StockQuantity { get; set; } = 0;
    public string Unit { get; set; } = null!;
    public decimal CostPricePerUnit { get; set; }
    public decimal SellingPricePerUnit { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Vendor Vendor { get; set; } = null!;
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<SalesInvoiceItem> SalesInvoiceItems { get; set; } = new List<SalesInvoiceItem>();
    public ICollection<LowStockAlert> LowStockAlerts { get; set; } = new List<LowStockAlert>();
}
