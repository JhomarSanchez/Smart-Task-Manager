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
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Tests;

public class Tier3WorkflowTests : BaseIntegrationTest
{
    public Tier3WorkflowTests(CustomWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task T3_Workflow_TaskLifeCycle_CreateUpdateCompleteDelete()
    {
        // 1. Create Task
        var createDto = new CreateTaskDto("LifeCycle Task", "Description", DateTimeOffset.UtcNow.AddDays(1), "High", "Life");
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var task = await createResponse.Content.ReadFromJsonAsync<TaskResponseDto>();
        task.Should().NotBeNull();
        var id = task!.Id;

        // 2. Update Details
        var updateDto = new UpdateTaskDto("LifeCycle Task - Edited", "New description", DateTimeOffset.UtcNow.AddDays(2), "Medium", "Life", "InProgress", 1000.0);
        var updateResponse = await Client.PutAsJsonAsync($"/api/tasks/{id}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Mark Complete
        var completeDto = updateDto with { Status = "Done" };
        var completeResponse = await Client.PutAsJsonAsync($"/api/tasks/{id}", completeDto);
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Delete
        var deleteResponse = await Client.DeleteAsync($"/api/tasks/{id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Verify gone
        var getResponse = await Client.GetAsync($"/api/tasks/{id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task T3_Workflow_SmartParseAndCreateTask()
    {
        // 1. Post to Smart Parse
        var parseReq = new SmartParseRequestDto("Reunión de mockups mañana a las 15:00 urgente");
        var parseResponse = await Client.PostAsJsonAsync("/api/tasks/smart-parse", parseReq);
        parseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var parseResult = await parseResponse.Content.ReadFromJsonAsync<SmartParseResponseDto>();
        parseResult.Should().NotBeNull();
        parseResult!.IsParsed.Should().BeTrue();
        parseResult.Title.Should().Be("Reunión de mockups");
        parseResult.Priority.Should().Be("High");

        // 2. Use details to create task
        var createDto = new CreateTaskDto(parseResult.Title, parseResult.Description, parseResult.DueDate, parseResult.Priority, parseResult.Category);
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Verify in list
        var listResponse = await Client.GetAsync("/api/tasks");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<IEnumerable<TaskResponseDto>>();
        list.Should().ContainSingle(t => t.Title == "Reunión de mockups" && t.Priority == "High");
    }

    [Fact]
    public async Task T3_Workflow_ReorderTasksOnBoard()
    {
        // 1. Create 3 Tasks
        var t1 = new TaskItem(TaskId.New(), "Task 1", null, null, TaskPriority.Low, null, 1000.0);
        var t2 = new TaskItem(TaskId.New(), "Task 2", null, null, TaskPriority.Low, null, 2000.0);
        var t3 = new TaskItem(TaskId.New(), "Task 3", null, null, TaskPriority.Low, null, 3000.0);
        DbContext.TaskItems.AddRange(t1, t2, t3);
        await DbContext.SaveChangesAsync();

        // 2. Move Task 3 (position 3000) between Task 1 (1000) and Task 2 (2000)
        var updateDto = new UpdateTaskDto("Task 3", null, null, "Low", null, "Todo", 1500.0);
        var updateResponse = await Client.PutAsJsonAsync($"/api/tasks/{t3.Id.Value}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 3. Fetch List and verify order
        var response = await Client.GetAsync("/api/tasks");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var tasks = (await response.Content.ReadFromJsonAsync<IEnumerable<TaskResponseDto>>())?.ToList();
        tasks.Should().NotBeNull();
        tasks!.Count.Should().Be(3);

        // Expect: Task 1 (1000), Task 3 (1500), Task 2 (2000)
        tasks[0].Id.Should().Be(t1.Id.Value);
        tasks[1].Id.Should().Be(t3.Id.Value);
        tasks[2].Id.Should().Be(t2.Id.Value);
    }

    [Fact]
    public async Task T3_Workflow_SmartParseFallbackAndManualCreate()
    {
        // 1. Send text that fails AI parsing
        var parseReq = new SmartParseRequestDto("asdfghjklqwertyuiop");
        var parseResponse = await Client.PostAsJsonAsync("/api/tasks/smart-parse", parseReq);
        parseResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var parseResult = await parseResponse.Content.ReadFromJsonAsync<SmartParseResponseDto>();
        parseResult.Should().NotBeNull();
        parseResult!.IsParsed.Should().BeFalse();
        parseResult.Title.Should().Be(parseReq.Text); // Fallback pre-fills the original text
        parseResult.Priority.Should().Be("Medium"); // Fallback defaults
        parseResult.Category.Should().Be("General");

        // 2. Create the task with fallback details
        var createDto = new CreateTaskDto(parseResult.Title, parseResult.Description, parseResult.DueDate, parseResult.Priority, parseResult.Category);
        var createResponse = await Client.PostAsJsonAsync("/api/tasks", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<TaskResponseDto>();
        created.Should().NotBeNull();
        created!.Title.Should().Be("asdfghjklqwertyuiop");
        created.Priority.Should().Be("Medium");
        created.Category.Should().Be("General");
    }

    [Fact]
    public async Task T3_Workflow_ConcurrentOperationsOnTasks()
    {
        // 1. Create a task
        var id = TaskId.New();
        var t = new TaskItem(id, "Concurrent Task", null, null, TaskPriority.Low, null, 1000.0);
        DbContext.TaskItems.Add(t);
        await DbContext.SaveChangesAsync();

        // 2. Perform concurrent updates
        var update1 = new UpdateTaskDto("Concurrent Task", "First description update", null, "Low", null, "InProgress", 1000.0);
        var update2 = new UpdateTaskDto("Concurrent Task", "Second description update", null, "High", null, "Todo", 1000.0);

        var task1 = Client.PutAsJsonAsync($"/api/tasks/{id.Value}", update1);
        var task2 = Client.PutAsJsonAsync($"/api/tasks/{id.Value}", update2);

        await Task.WhenAll(task1, task2);

        // 3. Verify task is still in a consistent state
        var response = await Client.GetAsync($"/api/tasks/{id.Value}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var taskResult = await response.Content.ReadFromJsonAsync<TaskResponseDto>();
        taskResult.Should().NotBeNull();
        // It must have resolved to either of the two valid DTO configurations
        taskResult!.Title.Should().Be("Concurrent Task");
        taskResult.Status.Should().Match(s => s == "InProgress" || s == "Todo");
        taskResult.Priority.Should().Match(p => p == "Low" || p == "High");
    }
}
