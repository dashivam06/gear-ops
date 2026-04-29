using System;

namespace gearOps.Domain.Entities;

public class LowStockAlert
{
    public int AlertId { get; set; }
    public int PartId { get; set; }
    public int StockAtAlert { get; set; }
    public bool Resolved { get; set; } = false;
    public DateTime AlertedAt { get; set; } = DateTime.UtcNow;

    public Part Part { get; set; } = null!;
}
