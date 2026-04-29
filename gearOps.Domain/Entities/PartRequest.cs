using System;
using gearOps.Domain.Enums;

namespace gearOps.Domain.Entities;

public class PartRequest
{
    public int PartRequestId { get; set; }
    public int CustomerId { get; set; }
    public string PartName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime RequestedDate { get; set; }
    public PartRequestStatus Status { get; set; } = PartRequestStatus.Pending;

    public User Customer { get; set; } = null!;
}
