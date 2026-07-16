using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Common;
using SmartTask.Domain.Enums;
using SmartTask.Application.DTOs;
using Xunit;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Tests;

public class Tier2IntegrationTests : BaseIntegrationTest
{
    public Tier2IntegrationTests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    // ==========================================
    // Feature 1: GET /api/tasks
    // ==========================================

    [Fact]
    public async Task T2_GetTasks_ShouldReturnSuccessStatusCode()
    {
        // Act
        var response = await Client.GetAsync("/api/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task T2_GetTasks_ShouldReturnJsonArray()
    {
        // Act
        var response = await Client.GetAsync("/api/tasks");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.Content.ReadFromJsonAsync<IEnumerable<TaskResponseDto>>();
        tasks.Should().NotBeNull();
    }

    [Fact]
    public async Task T2_GetTasks_ShouldSortByStatusThenPosition()
    {
        // Arrange
        var t1 = new TaskItem(TaskId.New(), "Task A", null, null, TaskPriority.Low, null, 2000.0);
        var t2 = new TaskItem(TaskId.New(), "Task B", null, null, TaskPriority.Low, null, 1000.0);
        t2.UpdateStatus(TaskStatus.InProgress);
        DbContext.TaskItems.AddRange(t1, t2);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = (await response.Content.ReadFromJsonAsync<IEnumerable<TaskResponseDto>>())?.ToList();
        tasks.Should().NotBeNull();
        tasks!.Count.Should().Be(2);
        // Sorted: Todo first (t1), then InProgress (t2)
        tasks[0].Title.Should().Be("Task A");
        tasks[1].Title.Should().Be("Task B");
    }

    [Fact]
    public async Task T2_GetTasks_EmptyDatabase_ShouldReturnEmptyList()
    {
        // Act
        var response = await Client.GetAsync("/api/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.Content.ReadFromJsonAsync<IEnumerable<TaskResponseDto>>();
        tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task T2_GetTasks_ShouldContainSeededTasks()
    {
        // Arrange
        var t = new TaskItem(TaskId.New(), "Seeded Task", "Verify", null, TaskPriority.Medium, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/tasks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tasks = await response.Content.ReadFromJsonAsync<IEnumerable<TaskResponseDto>>();
        tasks.Should().ContainSingle(x => x.Title == "Seeded Task");
    }

    // ==========================================
    // Feature 2: GET /api/tasks/{id}
    // ==========================================

    [Fact]
    public async Task T2_GetTaskById_WithValidId_ShouldReturnTaskResponse()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "Get Task", null, null, TaskPriority.High, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/tasks/{id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        task.Should().NotBeNull();
        task!.Id.Should().Be(id.Value);
    }

    [Fact]
    public async Task T2_GetTaskById_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var response = await Client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task T2_GetTaskById_WithEmptyGuid_ShouldReturnBadRequestOrNotFound()
    {
        // Act
        var response = await Client.GetAsync($"/api/tasks/{Guid.Empty}");

        // Assert
        response.StatusCode.Should().Match(code => code == HttpStatusCode.NotFound || code == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T2_GetTaskById_ResponseShouldContainCorrectFields()
    {
        // Arrange
        var id = TaskId.New();
        var date = DateTimeOffset.UtcNow.AddDays(2);
        var t = new TaskItem(id, "Fields Task", "Verify Fields", date, TaskPriority.High, "VerifyCategory", 1234.5);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/tasks/{id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var task = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        task.Should().NotBeNull();
        task!.Id.Should().Be(id.Value);
        task.Title.Should().Be("Fields Task");
        task.Description.Should().Be("Verify Fields");
        task.DueDate.Should().BeCloseTo(date, TimeSpan.FromSeconds(1));
        task.Priority.Should().Be("High");
        task.Category.Should().Be("VerifyCategory");
        task.Status.Should().Be("Todo");
        task.BoardPosition.Should().Be(1234.5);
    }

    [Fact]
    public async Task T2_GetTaskById_ShouldBeCaseInsensitiveForIds()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "Case Task", null, null, TaskPriority.Medium, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        var lowerId = id.Value.ToString().ToLower();
        var upperId = id.Value.ToString().ToUpper();

        // Act & Assert
        var res1 = await Client.GetAsync($"/api/tasks/{lowerId}");
        res1.StatusCode.Should().Be(HttpStatusCode.OK);

        var res2 = await Client.GetAsync($"/api/tasks/{upperId}");
        res2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ==========================================
    // Feature 3: POST /api/tasks
    // ==========================================

    [Fact]
    public async Task T2_CreateTask_WithValidDto_ShouldReturnCreatedStatusCode()
    {
        // Arrange
        var dto = new CreateTaskDto("New Task Title", "New Description", DateTimeOffset.UtcNow.AddDays(1), "High", "Work");

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        created.Should().NotBeNull();
        created!.Title.Should().Be(dto.Title);
    }

    [Fact]
    public async Task T2_CreateTask_WithInvalidDto_ShouldReturnBadRequest()
    {
        // Arrange - Title too short
        var dto = new CreateTaskDto("Ab", "Valid Description", null, "Medium", "General");

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T2_CreateTask_ShouldSetDefaultPositionToMaxPlusOneThousand()
    {
        // Arrange
        var t = new TaskItem(TaskId.New(), "Existing", null, null, TaskPriority.Low, null, 1500.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        var dto = new CreateTaskDto("New Position Task", null, null, "Medium", null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        created.Should().NotBeNull();
        created!.BoardPosition.Should().Be(2500.0);
    }

    [Fact]
    public async Task T2_CreateTask_ShouldPersistInDatabase()
    {
        // Arrange
        var dto = new CreateTaskDto("Persist Task", "Should show up in DB", null, "Low", "Testing");

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        created.Should().NotBeNull();

        // Assert
        var dbTask = await DbContext.TaskItems.FindAsync(new TaskId(created!.Id));
        dbTask.Should().NotBeNull();
        dbTask!.Title.Should().Be("Persist Task");
    }

    [Fact]
    public async Task T2_CreateTask_ResponseShouldContainGeneratedIdAndAuditTimestamps()
    {
        // Arrange
        var dto = new CreateTaskDto("Timestamp Task", null, null, "Low", null);

        // Act
        var response = await Client.PostAsJsonAsync("/api/tasks", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ==========================================
    // Feature 4: PUT /api/tasks/{id}
    // ==========================================

    [Fact]
    public async Task T2_UpdateTask_WithValidDto_ShouldReturnOkStatusCode()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "Update Me", null, null, TaskPriority.Low, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        var dto = new UpdateTaskDto("Updated OK", "New Desc", DateTimeOffset.UtcNow.AddDays(1), "Medium", "UpdateCat", "InProgress", 1200.0);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/tasks/{id.Value}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        updated.Should().NotBeNull();
        updated!.Title.Should().Be("Updated OK");
    }

    [Fact]
    public async Task T2_UpdateTask_WithInvalidDto_ShouldReturnBadRequest()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "Update Me", null, null, TaskPriority.Low, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        // Invalid title (empty)
        var dto = new UpdateTaskDto("", "New Desc", null, "Medium", null, "InProgress", 1200.0);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/tasks/{id.Value}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task T2_UpdateTask_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var dto = new UpdateTaskDto("Valid Title", "Desc", null, "Low", null, "Todo", 1000.0);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task T2_UpdateTask_ShouldModifyTaskDetailsInDatabase()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "DB Update Title", null, null, TaskPriority.Low, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        var dto = new UpdateTaskDto("Persisted Update", "Updated Description", null, "High", "Home", "Todo", 1000.0);

        // Act
        var response = await Client.PutAsJsonAsync($"/api/tasks/{id.Value}", dto);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        DbContext.ChangeTracker.Clear();
        var dbTask = await DbContext.TaskItems.FindAsync(id);
        dbTask.Should().NotBeNull();
        dbTask!.Title.Should().Be("Persisted Update");
        dbTask.Description.Should().Be("Updated Description");
        dbTask.Priority.Should().Be(TaskPriority.High);
    }

    [Fact]
    public async Task T2_UpdateTask_ShouldSupportStateTransitionToInProgressAndDone()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "Transition Task", null, null, TaskPriority.Low, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        // 1. Move to InProgress
        var dto1 = new UpdateTaskDto("Transition Task", null, null, "Low", null, "InProgress", 1000.0);
        var res1 = await Client.PutAsJsonAsync($"/api/tasks/{id.Value}", dto1);
        res1.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Move to Done
        var dto2 = new UpdateTaskDto("Transition Task", null, null, "Low", null, "Done", 1000.0);
        var res2 = await Client.PutAsJsonAsync($"/api/tasks/{id.Value}", dto2);
        res2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert final state in database
        DbContext.ChangeTracker.Clear();
        var dbTask = await DbContext.TaskItems.FindAsync(id);
        dbTask.Should().NotBeNull();
        dbTask!.Status.Should().Be(TaskStatus.Done);
    }

    // ==========================================
    // Feature 5: DELETE /api/tasks/{id}
    // ==========================================

    [Fact]
    public async Task T2_DeleteTask_WithValidId_ShouldReturnOkStatusCode()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "Delete Me", null, null, TaskPriority.Low, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/tasks/{id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task T2_DeleteTask_WithNonExistentId_ShouldReturnNotFound()
    {
        // Act
        var response = await Client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task T2_DeleteTask_ShouldRemoveTaskFromDatabase()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "Delete From DB", null, null, TaskPriority.Low, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/tasks/{id.Value}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        DbContext.ChangeTracker.Clear();
        var dbTask = await DbContext.TaskItems.FindAsync(id);
        dbTask.Should().BeNull();
    }

    [Fact]
    public async Task T2_DeleteTask_ShouldBeIdempotent_SecondCallShouldReturnNotFound()
    {
        // Arrange
        var id = TaskId.New();
        var t = new TaskItem(id, "Idempotent Delete", null, null, TaskPriority.Low, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        // Act & Assert
        var res1 = await Client.DeleteAsync($"/api/tasks/{id.Value}");
        res1.StatusCode.Should().Be(HttpStatusCode.OK);

        var res2 = await Client.DeleteAsync($"/api/tasks/{id.Value}");
        res2.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task T2_DeleteTask_WithInvalidFormatId_ShouldReturnBadRequestOrNotFound()
    {
        // Act
        var response = await Client.DeleteAsync("/api/tasks/not-a-valid-guid");

        // Assert
        response.StatusCode.Should().Match(code => code == HttpStatusCode.NotFound || code == HttpStatusCode.BadRequest);
    }
}
