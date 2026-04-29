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

    public User Customer { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
}
