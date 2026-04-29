using System;
using System.Collections.Generic;

namespace gearOps.Domain.Entities;

public class Vendor
{
    public int VendorId { get; set; }
    public string VendorName { get; set; } = null!;
    public string? ContactPerson { get; set; }
    public string Phone { get; set; } = null!;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Part> Parts { get; set; } = new List<Part>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
