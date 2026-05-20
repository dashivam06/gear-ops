using System;
using System.Collections.Generic;

namespace gearOps.Domain.Entities;

public class Staff
{
    public int StaffId { get; set; }
    public int UserId { get; set; }  // Foreign key linking to User
    public string Position { get; set; } = null!;
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property to User
    public User User { get; set; } = null!;
}
