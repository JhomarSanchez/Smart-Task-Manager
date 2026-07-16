using Microsoft.Extensions.DependencyInjection;
using SmartTask.Infrastructure.Persistence;
using System.Net.Http;
using Xunit;

namespace SmartTask.Tests;

public class BaseIntegrationTest : IClassFixture<CustomWebApplicationFactory<Program>>, System.IDisposable
{
    protected readonly CustomWebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected readonly IServiceScope Scope;
    protected readonly AppDbContext DbContext;

    public BaseIntegrationTest(CustomWebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Reset database schema for a clean slate per test
        DbContext.Database.EnsureDeleted();
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Scope.Dispose();
    }
}
