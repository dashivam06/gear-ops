using Microsoft.EntityFrameworkCore;
using gearOps.Domain.Entities;

namespace gearOps.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Vendor> Vendors { get; set; } = null!;
    public DbSet<Part> Parts { get; set; } = null!;
    public DbSet<Vehicle> Vehicles { get; set; } = null!;
    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<Review> Reviews { get; set; } = null!;
    public DbSet<PartRequest> PartRequests { get; set; } = null!;
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; } = null!;
    public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; } = null!;
    public DbSet<SalesInvoice> SalesInvoices { get; set; } = null!;
    public DbSet<SalesInvoiceItem> SalesInvoiceItems { get; set; } = null!;
    public DbSet<LoyaltyDiscount> LoyaltyDiscounts { get; set; } = null!;
    public DbSet<ServiceRecord> ServiceRecords { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<LowStockAlert> LowStockAlerts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
            
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Phone)
            .IsUnique();

        // Primary Key configurations for non-standard ID names based on your DDL logic
        modelBuilder.Entity<RefreshToken>().HasKey(rt => rt.TokenId);
        modelBuilder.Entity<LoyaltyDiscount>().HasKey(ld => ld.LoyaltyId);
        modelBuilder.Entity<LowStockAlert>().HasKey(lsa => lsa.AlertId);

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
    }
}
