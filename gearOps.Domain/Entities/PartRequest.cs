using System;
using gearOps.Domain.Enums;

namespace gearOps.Domain.Entities;

public class PartRequest
{
    public int PartRequestId { get; set; }
    public int CustomerId { get; set; }
    public int? VehicleId { get; set; }
    public string PartName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
    public PartRequestStatus Status { get; set; } = PartRequestStatus.Pending;
    public int? ReviewedByStaffId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? DecisionNote { get; set; }
    public int? SuggestedPartId { get; set; }

    public User Customer { get; set; } = null!;
    public Vehicle? Vehicle { get; set; }
    public Part? SuggestedPart { get; set; }
}
