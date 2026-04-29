namespace gearOps.Domain.Entities;

public class SalesInvoiceItem
{
    public int SalesInvoiceItemId { get; set; }
    public int SalesInvoiceId { get; set; }
    public int PartId { get; set; }
    public int Quantity { get; set; }
    public decimal PricePerUnit { get; set; }

    public SalesInvoice SalesInvoice { get; set; } = null!;
    public Part Part { get; set; } = null!;
}
