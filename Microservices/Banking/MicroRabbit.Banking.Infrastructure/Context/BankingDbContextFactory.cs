using System;
using System.IO;
using MicroRabbit.Banking.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ActisGolf.Infrastructure.Data
{
    // IDesignTimeDbContextFactory is used when the DbContext exists in another project, like a shared library.
    // IDesignTimeDbContextFactory is used usually when you execute EF Core commands like Add-Migration, Update-Database, and so on.
    // IDesignTimeDbContextFactory is afactory for creating derived DbContext instances.
    public class BankingDbContextFactory : IDesignTimeDbContextFactory<BankingDbContext>
    {      
        // Creates a new instance of a derived context.
        public BankingDbContext CreateDbContext(string[] args)
        {
            // Build the configuration
            var configuration = new ConfigurationBuilder()

                // Sets the FileProvider for file-based providers to a PhysicalFileProvider with the base path.
                .SetBasePath(Directory.GetCurrentDirectory()) // requires Microsoft.Extensions.Configuration.Json

                // Configuration providers read configuration data from key-value pairs.
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true) // requires Microsoft.Extensions.Configuration.Json

                // This would be override the default appsetting.json. If the environment is not specified set the production
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true) // requires Microsoft.Extensions.Configuration.Json

                // Reads configuration values from environment variables.
                .AddEnvironmentVariables() // requires Microsoft.Extensions.Configuration.EnvironmentVariables

                // Build an IConfiguration
                .Build();

            // Use BankingDbContext with options.
            var builder = new DbContextOptionsBuilder<BankingDbContext>();

            // Get the connection string from appsetting.json added above.
            var connectionString = configuration.GetConnectionString("BankingDbConnection");

            // Configures the context to connect to a Microsoft SQL Server database.
            builder.UseSqlServer(connectionString);

            return new BankingDbContext(builder.Options);
        }
    }
}