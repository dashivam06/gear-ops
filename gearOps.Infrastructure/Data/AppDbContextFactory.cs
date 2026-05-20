using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace gearOps.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder
            .UseNpgsql(BuildConnectionString())
            .UseSnakeCaseNamingConvention();

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string BuildConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "corerouter.postgres.database.azure.com";
        var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "gearOps";
        var username = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "corerouter";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "Islington@63";

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }
}
