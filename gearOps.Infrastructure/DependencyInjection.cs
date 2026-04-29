using System;
using gearOps.Application.Interfaces;
using gearOps.Infrastructure.Data;
using gearOps.Infrastructure.Repositories;
using gearOps.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddScoped<IEmailService, GraphEmailService>();
        services.AddScoped<ITokenService, TokenService>();
        
        return services;
    }
}
