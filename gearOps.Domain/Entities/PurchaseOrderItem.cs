namespace gearOps.Domain.Entities;

public class PurchaseOrderItem
{
    public int PurchaseOrderItemId { get; set; }
    public int PurchaseOrderId { get; set; }
    public int PartId { get; set; }
    public int Quantity { get; set; }
    public decimal PricePerUnit { get; set; }

    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Part Part { get; set; } = null!;
}
