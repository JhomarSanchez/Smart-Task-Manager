using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartTask.Infrastructure.Persistence;
using System;
using System.Data.Common;

namespace SmartTask.Tests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private DbConnection? _connection;

    public CustomWebApplicationFactory()
    {
        Environment.SetEnvironmentVariable("UseSqliteForTesting", "true");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("UseInMemoryDb", "true");
        builder.ConfigureServices(services =>
        {
            // Remove the PostgreSQL DbContext registrations for in-memory DB
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            services.AddScoped<SmartTask.Application.Interfaces.Services.ISmartParserService, SmartTask.Infrastructure.Services.MockSmartParserService>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                _connection?.Dispose();
            }
            catch
            {
                // Ignore double-disposal issues
            }
        }
        base.Dispose(disposing);
    }
}
