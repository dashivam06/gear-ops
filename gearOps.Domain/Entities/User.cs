using System;
using System.Collections.Generic;
using gearOps.Domain.Enums;

namespace gearOps.Domain.Entities;

public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? Address { get; set; }
    public string PasswordHash { get; set; } = null!;
    public Role Role { get; set; } = Role.Customer;
    public decimal CreditsRemaining { get; set; } = 0.00m;
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<PartRequest> PartRequests { get; set; } = new List<PartRequest>();
    public ICollection<SalesInvoice> CustomerInvoices { get; set; } = new List<SalesInvoice>();
    public ICollection<SalesInvoice> StaffInvoices { get; set; } = new List<SalesInvoice>();
    public ICollection<LoyaltyDiscount> LoyaltyDiscounts { get; set; } = new List<LoyaltyDiscount>();
    public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
