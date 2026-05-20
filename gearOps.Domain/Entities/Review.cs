using System;

namespace gearOps.Domain.Entities;

public class Review
{
    public int ReviewId { get; set; }
    public int CustomerId { get; set; }
    public int AppointmentId { get; set; }
    public byte Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime ReviewedDate { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

    public User Customer { get; set; } = null!;
    public Appointment Appointment { get; set; } = null!;
}
