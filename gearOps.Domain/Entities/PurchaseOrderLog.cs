using System;
using gearOps.Domain.Enums;

namespace gearOps.Domain.Entities;

public class PurchaseOrderLog
{
    public int PurchaseOrderLogId { get; set; }
    public int PurchaseOrderId { get; set; }
    public PurchaseOrderStatus? FromStatus { get; set; }
    public PurchaseOrderStatus? ToStatus { get; set; }
    public string Action { get; set; } = null!;
    public string? Notes { get; set; }
    public bool EmailSentToVendor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
}
