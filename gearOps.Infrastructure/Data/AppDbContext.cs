using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using gearOps.Domain.Entities;

namespace gearOps.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User, IdentityRole<int>, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Users is already defined in IdentityDbContext, but we can leave it or remove it. Actually it's defined as Users so we can remove DbSet<User> or keep it. It's better to remove it.
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
        
        // Identity configurations
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<IdentityRole<int>>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

        // Note: IdentityUser already has unique indexes for NormalizedEmail and NormalizedUserName. 
        // We can remove the custom indexes for Email. 
        modelBuilder.Entity<User>()
            .HasIndex(u => u.PhoneNumber)
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
