using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SmartTask.Application;
using SmartTask.Infrastructure;
using SmartTask.Api.Endpoints;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Register Application Services (Handlers, Validators)
builder.Services.AddApplicationServices();

// Register Infrastructure Services (DbContext, PostgreSQL, Repositories)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Configure HTTP JSON options to serialize enums as strings for API client convenience
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
});

// Configure CORS for Angular Frontend local development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular local dev server
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseCors();

app.MapGet("/", () => new { Message = "Smart Task Manager API is running..." });

// Map Minimal API Endpoints
app.MapTaskEndpoints();

app.Run();

public partial class Program { }
