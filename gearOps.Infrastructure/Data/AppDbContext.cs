using Microsoft.EntityFrameworkCore;
using gearOps.Domain.Entities;

namespace gearOps.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Staff> Staff { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Vendor> Vendors { get; set; } = null!;
    public DbSet<Part> Parts { get; set; } = null!;
    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;
    public DbSet<PartRequest> PartRequests { get; set; } = null!;
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } = null!;
    public DbSet<PurchaseOrderLog> PurchaseOrderLogs { get; set; } = null!;
    public DbSet<SalesInvoice> SalesInvoices { get; set; } = null!;
    public DbSet<SalesInvoiceItem> SalesInvoiceItems { get; set; } = null!;
    public DbSet<LoyaltyDiscount> LoyaltyDiscounts { get; set; } = null!;
    public DbSet<ServiceRecord> ServiceRecords { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<LowStockAlert> LowStockAlerts { get; set; } = null!;
    public DbSet<ScheduledNotification> ScheduledNotifications { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Phone)
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");

        // Primary Key configurations for non-standard ID names based on your DDL logic
        modelBuilder.Entity<RefreshToken>().HasKey(rt => rt.TokenId);
        modelBuilder.Entity<LoyaltyDiscount>().HasKey(ld => ld.LoyaltyId);
        modelBuilder.Entity<LowStockAlert>().HasKey(lsa => lsa.AlertId);
        modelBuilder.Entity<ScheduledNotification>().HasKey(sn => sn.NotificationId);

        // Staff-User relationship (Staff references User with UserId)
        modelBuilder.Entity<Staff>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Explicitly map 'User' to 'SalesInvoice' multiple relationships
        modelBuilder.Entity<SalesInvoice>()
            .HasOne(s => s.Customer)
            .WithMany(u => u.CustomerInvoices)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SalesInvoice>()
            .HasOne(s => s.Staff)
            .WithMany(u => u.StaffInvoices)
            .HasForeignKey(s => s.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // Explicitly map 'User' to 'ServiceRecord' for Staff
        modelBuilder.Entity<ServiceRecord>()
            .HasOne(sr => sr.Staff)
            .WithMany(u => u.ServiceRecords)
            .HasForeignKey(sr => sr.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // Appointment - ApprovedByStaff relationship (optional, staff can approve appointments)
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.ApprovedByStaff)
            .WithMany()
            .HasForeignKey(a => a.ApprovedByStaffId)
            .OnDelete(DeleteBehavior.SetNull);

        // SalesInvoice - Appointment optional link
        modelBuilder.Entity<SalesInvoice>()
            .HasOne(si => si.Appointment)
            .WithMany(a => a.SalesInvoices)
            .HasForeignKey(si => si.AppointmentId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // PartRequest - SuggestedPart (optional Part link for staff/admin)
        modelBuilder.Entity<PartRequest>()
            .HasOne(pr => pr.SuggestedPart)
            .WithMany()
            .HasForeignKey(pr => pr.SuggestedPartId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
