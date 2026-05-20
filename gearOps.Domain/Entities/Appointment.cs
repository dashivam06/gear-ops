using System;
using System.Collections.Generic;
using gearOps.Domain.Enums;

namespace gearOps.Domain.Entities;

public class Appointment
{
    public int AppointmentId { get; set; }
    public int CustomerId { get; set; }
    public int VehicleId { get; set; }
    public DateTime RequestedDate { get; set; }
    public string? Remarks { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public int? ApprovedByStaffId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Customer { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public User? ApprovedByStaff { get; set; }
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
    public ICollection<SalesInvoice> SalesInvoices { get; set; } = new List<SalesInvoice>();
}
