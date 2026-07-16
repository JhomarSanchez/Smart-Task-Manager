using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using SmartTask.Application.DTOs;
using SmartTask.Application.Features.Tasks;
using SmartTask.Application.Interfaces.Services;
using SmartTask.Application.Validators;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SmartTask.Api.Endpoints;

public static class TaskEndpoints
{
    public static void MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks");

        group.MapGet("/", async (
            GetTasksHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(cancellationToken);
            return Results.Ok(result.Value);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetTaskByIdHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);
            if (result.IsFailure)
            {
                return Results.NotFound(new { Message = result.Error.Message });
            }
            return Results.Ok(result.Value);
        });

        group.MapPost("/", async (
            CreateTaskDto dto,
            IValidator<CreateTaskDto> validator,
            CreateTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(dto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary();
                return Results.BadRequest(new { Errors = errors });
            }

            var result = await handler.HandleAsync(dto, cancellationToken);
            return Results.Created($"/api/tasks/{result.Value.Id}", result.Value);
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTaskDto dto,
            IValidator<UpdateTaskDto> validator,
            UpdateTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(dto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary();
                return Results.BadRequest(new { Errors = errors });
            }

            var result = await handler.HandleAsync(id, dto, cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error.Type == Domain.Common.Errors.ErrorType.NotFound)
                {
                    return Results.NotFound(new { Message = result.Error.Message });
                }
                return Results.BadRequest(new { Message = result.Error.Message });
            }
            return Results.Ok(result.Value);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            DeleteTaskHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);
            if (result.IsFailure)
            {
                if (result.Error.Type == Domain.Common.Errors.ErrorType.NotFound)
                {
                    return Results.NotFound(new { Message = result.Error.Message });
                }
                return Results.BadRequest(new { Message = result.Error.Message });
            }
            return Results.Ok();
        });

        group.MapPost("/smart-parse", async (
            SmartParseRequestDto dto,
            IValidator<SmartParseRequestDto> validator,
            ISmartParserService parserService,
            CancellationToken cancellationToken) =>
        {
            var validationResult = await validator.ValidateAsync(dto, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.ToDictionary();
                return Results.BadRequest(new { Errors = errors });
            }

            var response = await parserService.ParseAsync(dto.Text, cancellationToken);
            return Results.Ok(response);
        });
    }
}
