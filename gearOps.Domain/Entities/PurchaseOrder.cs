using System;
using System.Collections.Generic;

namespace gearOps.Domain.Entities;

public class PurchaseOrder
{
    public int PurchaseOrderId { get; set; }
    public int VendorId { get; set; }
    public DateTime OrderDate { get; set; }
    public string InvoiceNumber { get; set; } = null!;
    public decimal TotalAmount { get; set; } = 0.00m;

    public Vendor Vendor { get; set; } = null!;
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
}
