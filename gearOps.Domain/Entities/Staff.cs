using System;
using System.Collections.Generic;

namespace gearOps.Domain.Entities;

public class Staff
{
    public int StaffId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string Position { get; set; } = null!;
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
    public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
}
