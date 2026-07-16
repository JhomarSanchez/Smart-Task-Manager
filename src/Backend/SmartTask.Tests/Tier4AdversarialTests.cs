using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Common;
using SmartTask.Domain.Enums;
using SmartTask.Application.DTOs;
using Xunit;

namespace SmartTask.Tests;

public class Tier4AdversarialTests : BaseIntegrationTest
{
    public Tier4AdversarialTests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task T4_Adversarial_SqlInjectionPayload_InTextFields()
    {
        // Arrange
        var sqlPayload = "'; DROP TABLE TaskItems; --";
        var dto = new CreateTaskDto($"SqlInjection {sqlPayload}", $"Description {sqlPayload}", DateTimeOffset.UtcNow.AddDays(1), "High", "Security");

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        created.Should().NotBeNull();
        created!.Title.Should().Contain("DROP TABLE");

        // Verify database is still intact by querying it
        var dbTask = await DbContext.TaskItems.FindAsync(new TaskId(created.Id));
        dbTask.Should().NotBeNull();
        dbTask!.Title.Should().Be(dto.Title);
    }

    [Fact]
    public async Task T4_Adversarial_HtmlAndXssPayload_InTextFields()
    {
        // Arrange
        var xssPayload = "<script>alert('XSS Attack!');</script>";
        var dto = new CreateTaskDto("XssTask", $"Description {xssPayload}", DateTimeOffset.UtcNow.AddDays(1), "Medium", "Security");

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        created.Should().NotBeNull();
        created!.Description.Should().Contain("<script>");

        // Verify HTML tags are stored exactly as text (no execution or crashing)
        var dbTask = await DbContext.TaskItems.FindAsync(new TaskId(created.Id));
        dbTask.Should().NotBeNull();
        dbTask!.Description.Should().Be(dto.Description);
    }

    [Fact]
    public async Task T4_Adversarial_ExtremelyLargePayload()
    {
        // Arrange - Title exceeding 100 characters constraint
        var extremelyLargeTitle = new string('A', 500);
        var dto = new CreateTaskDto(extremelyLargeTitle, "Valid Description", null, "Low", null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T4_Adversarial_OutOfRangeDates()
    {
        // Arrange - DueDate is in the past
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);
        var dto = new CreateTaskDto("Past Task", "Due in past", pastDate, "Low", null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T4_Adversarial_RapidFireSequencedUpdates()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "Rapid Task", null, null, TaskPriority.Low, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        var tasks = new List<Task>();
        // Fire 20 successive status updates
        for (int i = 0; i < 20; i++)
        {
            var priority = i % 2 == 0 ? "High" : "Low";
            var status = i % 3 == 0 ? "Todo" : (i % 3 == 1 ? "InProgress" : "Done");
            var dto = new UpdateTaskDto("Rapid Task", null, null, priority, null, status, 1000.0);
            
            tasks.Add(Client.PutAsJsonAsync($"/api/tasks/{id.Value}", dto));
        }

        // Act & Assert
        Func<Task> act = async () => await Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();

        // Verify task exists and is not corrupted
        var getResponse = await Client.GetAsync($"/api/tasks/{id.Value}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalTask = await getResponse.Content.ReadFromJsonAsync<TaskResponseDto>();
        finalTask.Should().NotBeNull();
        finalTask!.Title.Should().Be("Rapid Task");
    }

    [Fact]
    public async Task T4_Adversarial_PriorityCaseSensitivity_ShouldAcceptLowercase()
    {
        // Arrange
        var dto = new CreateTaskDto("Lowercase Priority Task", "Should be accepted because priority is case insensitive", DateTimeOffset.UtcNow.AddDays(1), "high", "Testing");

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        created.Should().NotBeNull();
        created!.Priority.Should().Be("High");
    }

    [Fact]
    public async Task T4_Adversarial_StatusCaseSensitivity_ShouldAcceptLowercase()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "Update Status Case", null, null, TaskPriority.Low, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        var dto = new UpdateTaskDto("Update Status Case", null, null, "Low", null, "inprogress", 1000.0);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/tasks/{id.Value}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        updated.Should().NotBeNull();
        updated!.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task T4_Adversarial_BoardPositionInfinity_ShouldBeRejected()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "Infinity Position Task", null, null, TaskPriority.Low, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        var dto = new UpdateTaskDto("Infinity Position Task", null, null, "Low", null, "Todo", double.PositiveInfinity);
        var options = new System.Text.Json.JsonSerializerOptions
        {
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/tasks/{id.Value}", dto, options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T4_Adversarial_BoardPositionNaN_ShouldBeRejected()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "NaN Position Task", null, null, TaskPriority.Low, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        var dto = new UpdateTaskDto("NaN Position Task", null, null, "Low", null, "Todo", double.NaN);
        var options = new System.Text.Json.JsonSerializerOptions
        {
            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/tasks/{id.Value}", dto, options);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T4_Adversarial_MalformedJsonPayload_ShouldReturnBadRequest()
    {
        // Arrange
        var content = new StringContent("{invalid_json: true,", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/tasks", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T4_Adversarial_SmartParse_ExtremelyLongText_ShouldBeHandled()
    {
        // Arrange
        var longText = new string('A', 50000);
        var dto = new SmartParseRequestDto(longText);

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks/smart-parse", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var parsed = await response.Content.ReadFromJsonAsync<SmartParseResponseDto>();
        parsed.Should().NotBeNull();
        parsed!.IsParsed.Should().BeFalse();
        parsed.Title.Should().Be(longText);
    }
}
