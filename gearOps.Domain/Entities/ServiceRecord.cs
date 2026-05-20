using System;
using gearOps.Domain.Enums;

namespace gearOps.Domain.Entities;

public class ServiceRecord
{
    public int ServiceRecordId { get; set; }
    public int AppointmentId { get; set; }
    public int VehicleId { get; set; }
    public int StaffId { get; set; }
    public string ServiceDescription { get; set; } = null!;
    public decimal ServiceCost { get; set; } = 0.00m;
    public DateTime ServiceDate { get; set; }
    public ServiceRecordStatus Status { get; set; } = ServiceRecordStatus.InProgress;

    public Appointment Appointment { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public User Staff { get; set; } = null!;
}
