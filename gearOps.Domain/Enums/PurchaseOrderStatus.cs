namespace gearOps.Domain.Enums;

public enum PurchaseOrderStatus
{
    Draft = 0,
    SentToVendor = 1,
    ConfirmedByVendor = 2,
    Delivered = 3,
    Cancelled = 4
}
