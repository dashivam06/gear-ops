using System;
using gearOps.Application.Interfaces;
using gearOps.Infrastructure.Data;
using gearOps.Infrastructure.Repositories;
using gearOps.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace gearOps.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        // DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql($"Host={config["DB_HOST"] ?? "corerouter.postgres.database.azure.com"};" +
                              $"Port={config["DB_PORT"] ?? "5432"};" +
                              $"Database={config["DB_NAME"] ?? "gearOps"};" +
                              $"Username={config["DB_USERNAME"] ?? "corerouter"};" +
                              $"Password={config["DB_PASSWORD"] ?? "Islington@63"}")
                   .UseSnakeCaseNamingConvention());

        // Redis
        var redisConfig = $"{config["REDIS_HOST"] ?? "redis-16383.crce276.ap-south-1-3.ec2.cloud.redislabs.com"}:{config["REDIS_PORT"] ?? "16383"},password={config["REDIS_PASSWORD"] ?? "O2B34kkiyIX6hF7aBhkzLSq0um1Ygui7"}";
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));
        
        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        
        // Services
        services.AddScoped<IRedisService, RedisService>();
        services.AddScoped<IOtpService, OtpService>();
        services.AddScoped<IEmailService, AcsEmailService>();
        services.AddScoped<ITokenService, TokenService>();
        
        // Admin Services
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IPartService, PartService>();
        services.AddScoped<IVendorService, VendorService>();
        services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();
        services.AddScoped<IReportService, ReportService>();
        
        // Customer Services
        services.AddScoped<ICustomerProfileService, CustomerProfileService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IPartRequestService, PartRequestService>();
        services.AddScoped<IPurchaseHistoryService, PurchaseHistoryService>();
        services.AddScoped<ILoyaltyProgramService, LoyaltyProgramService>();
        services.AddScoped<IInvoicePdfService, InvoicePdfService>();
        services.AddScoped<ICreditService, CreditService>();
        
        // Staff Services
        services.AddScoped<IStaffProfileService, StaffProfileService>();
        services.AddScoped<IStaffScheduleService, StaffScheduleService>();
        services.AddScoped<IStaffServiceRecordService, StaffServiceRecordService>();
        services.AddScoped<IStaffReportService>(provider =>
            new StaffReportService(
                provider.GetRequiredService<AppDbContext>(),
                provider.GetRequiredService<ILogger<StaffReportService>>(),
                provider.GetRequiredService<IStaffProfileService>(),
                provider.GetRequiredService<IStaffScheduleService>()
            )
        );
        services.AddScoped<IStaffCustomerService, StaffCustomerService>();
        services.AddScoped<IStaffSalesService, StaffSalesService>();
        
        return services;
    }
}
