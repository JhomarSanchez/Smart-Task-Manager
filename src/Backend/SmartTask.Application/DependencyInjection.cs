using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using SmartTask.Application.Features.Tasks;

namespace SmartTask.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        
        services.AddScoped<GetTasksHandler>();
        services.AddScoped<GetTaskByIdHandler>();
        services.AddScoped<CreateTaskHandler>();
        services.AddScoped<UpdateTaskHandler>();
        services.AddScoped<DeleteTaskHandler>();

        return services;
    }
}
