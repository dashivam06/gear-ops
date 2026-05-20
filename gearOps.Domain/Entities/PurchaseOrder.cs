using System;
using System.Collections.Generic;
using gearOps.Domain.Enums;

namespace gearOps.Domain.Entities;

public class PurchaseOrder
{
    public int PurchaseOrderId { get; set; }
    public int VendorId { get; set; }
    public DateTime OrderDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal TotalAmount { get; set; } = 0.00m;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
    public DateTime? SentToVendorAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public Vendor Vendor { get; set; } = null!;
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<PurchaseOrderLog> Logs { get; set; } = new List<PurchaseOrderLog>();
}
