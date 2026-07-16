using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartTask.Application.Interfaces.Persistence;
using SmartTask.Application.Interfaces.Services;
using SmartTask.Infrastructure.Persistence;
using SmartTask.Infrastructure.Persistence.Repositories;
using SmartTask.Infrastructure.Services;

namespace SmartTask.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var useInMemoryDb = configuration["UseInMemoryDb"] == "true" || 
                            Environment.GetEnvironmentVariable("UseInMemoryDb") == "true" ||
                            Environment.GetEnvironmentVariable("UseSqliteForTesting") == "true";

        if (!useInMemoryDb)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, b => 
                    b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
        }

        services.AddScoped<ITaskRepository, TaskRepository>();
        
        var section = configuration.GetSection(Configuration.AiSettings.SectionName);
        var enabledStr = section["Enabled"];
        var provider = section["Provider"];
        var apiKey = section["ApiKey"];
        var modelName = section["ModelName"];

        var aiSettings = new Configuration.AiSettings
        {
            Enabled = !string.IsNullOrEmpty(enabledStr) && bool.TryParse(enabledStr, out var b) && b,
            Provider = string.IsNullOrWhiteSpace(provider) ? "Gemini" : provider,
            ApiKey = apiKey ?? string.Empty,
            ModelName = string.IsNullOrWhiteSpace(modelName) ? "gemini-1.5-flash" : modelName
        };
        
        services.AddSingleton(aiSettings);

        if (string.Equals(aiSettings.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<ISmartParserService, OpenAiParserService>();
        }
        else
        {
            services.AddScoped<ISmartParserService, GeminiParserService>();
        }

        return services;
    }
}
