using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FluentValidation;
using SmartTask.Domain.Common;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Enums;
using SmartTask.Application.DTOs;
using SmartTask.Application.Validators;
using Xunit;
using TaskStatus = SmartTask.Domain.Enums.TaskStatus;

namespace SmartTask.Tests;

public class Tier1UnitTests
{
    // ==========================================
    // Feature 1: TaskItem Domain Entity
    // ==========================================

    [Fact]
    public void T1_TaskItem_Constructor_ShouldSetInitialStateCorrectly()
    {
        // Arrange
        var id = TaskId.New();
        var title = "Test Task";
        var description = "This is a test task";
        var dueDate = DateTimeOffset.UtcNow.AddDays(2);
        var priority = TaskPriority.High;
        var category = "Work";
        var position = 1000.0;

        // Act
        var task = new TaskItem(id, title, description, dueDate, priority, category, position);

        // Assert
        task.Id.Should().Be(id);
        task.Title.Should().Be(title);
        task.Description.Should().Be(description);
        task.DueDate.Should().Be(dueDate);
        task.Priority.Should().Be(priority);
        task.Category.Should().Be(category);
        task.Status.Should().Be(TaskStatus.Todo);
        task.BoardPosition.Should().Be(position);
        task.CreatedAt.Should().BeBefore(DateTimeOffset.UtcNow.AddSeconds(1));
        task.UpdatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void T1_TaskItem_Constructor_ShouldThrowArgumentException_WhenTitleIsEmpty(string? invalidTitle)
    {
        // Arrange
        var id = TaskId.New();

        // Act
        Action act = () => new TaskItem(id, invalidTitle!, null, null, TaskPriority.Medium, null, 1000.0);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void T1_TaskItem_UpdateDetails_ShouldModifyFieldsAndSetUpdatedAt()
    {
        // Arrange
        var task = new TaskItem(TaskId.New(), "Original Title", "Original Desc", null, TaskPriority.Low, "Personal", 1000.0);
        var newTitle = "Updated Title";
        var newDesc = "Updated Desc";
        var newDueDate = DateTimeOffset.UtcNow.AddDays(5);
        var newPriority = TaskPriority.Medium;
        var newCategory = "Work";

        // Act
        task.UpdateDetails(newTitle, newDesc, newDueDate, newPriority, newCategory);

        // Assert
        task.Title.Should().Be(newTitle);
        task.Description.Should().Be(newDesc);
        task.DueDate.Should().Be(newDueDate);
        task.Priority.Should().Be(newPriority);
        task.Category.Should().Be(newCategory);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void T1_TaskItem_UpdateStatus_ShouldChangeStatusAndSetUpdatedAt()
    {
        // Arrange
        var task = new TaskItem(TaskId.New(), "Test Task", null, null, TaskPriority.Medium, null, 1000.0);

        // Act
        task.UpdateStatus(TaskStatus.InProgress);

        // Assert
        task.Status.Should().Be(TaskStatus.InProgress);
        task.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void T1_TaskItem_UpdatePosition_ShouldModifyBoardPosition()
    {
        // Arrange
        var task = new TaskItem(TaskId.New(), "Test Task", null, null, TaskPriority.Medium, null, 1000.0);

        // Act
        task.UpdatePosition(2500.5);

        // Assert
        task.BoardPosition.Should().Be(2500.5);
        task.UpdatedAt.Should().NotBeNull();
    }

    // ==========================================
    // Feature 2: CreateTaskDto Validation
    // ==========================================

    [Fact]
    public void T1_CreateTaskDto_ValidInput_ShouldPassValidation()
    {
        // Arrange
        var dto = new CreateTaskDto("Valid Title", "Description", DateTimeOffset.UtcNow.AddDays(1), "High", "Work");
        var validator = new CreateTaskValidator();

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void T1_CreateTaskDto_TitleTooShort_ShouldFailValidation()
    {
        // Arrange
        var dto = new CreateTaskDto("Ab", "Valid Description", null, "Low", "General");
        var validator = new CreateTaskValidator();

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Title");
    }

    [Fact]
    public void T1_CreateTaskDto_TitleTooLong_ShouldFailValidation()
    {
        // Arrange
        var longTitle = new string('A', 101);
        var dto = new CreateTaskDto(longTitle, "Valid Description", null, "Low", "General");
        var validator = new CreateTaskValidator();

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Title");
    }

    [Fact]
    public void T1_CreateTaskDto_DescriptionTooLong_ShouldFailValidation()
    {
        // Arrange
        var longDesc = new string('B', 1001);
        var dto = new CreateTaskDto("Valid Title", longDesc, null, "Medium", "General");
        var validator = new CreateTaskValidator();

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Description");
    }

    [Fact]
    public void T1_CreateTaskDto_CategoryTooLong_ShouldFailValidation()
    {
        // Arrange
        var longCategory = new string('C', 51);
        var dto = new CreateTaskDto("Valid Title", "Desc", null, "Medium", longCategory);
        var validator = new CreateTaskValidator();

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Category");
    }

    // ==========================================
    // Feature 3: UpdateTaskDto Validation
    // ==========================================

    [Fact]
    public void T1_UpdateTaskDto_ValidInput_ShouldPassValidation()
    {
        // Arrange
        var dto = new UpdateTaskDto("Valid Edit", "Desc", DateTimeOffset.UtcNow.AddDays(2), "Low", "Home", "InProgress", 2000.0);
        var validator = new UpdateTaskValidator();

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void T1_UpdateTaskDto_TitleTooShort_ShouldFailValidation()
    {
        // Arrange
        var dto = new UpdateTaskDto("T", "Desc", null, "Low", "Home", "Todo", 1000.0);
        var validator = new UpdateTaskValidator();

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Title");
    }

    [Fact]
    public void T1_UpdateTaskDto_DescriptionTooLong_ShouldFailValidation()
    {
        // Arrange
        var longDesc = new string('D', 1001);
        var dto = new UpdateTaskDto("Valid Title", longDesc, null, "Low", "Home", "Todo", 1000.0);
        var validator = new UpdateTaskValidator();

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Description");
    }

    [Fact]
    public void T1_UpdateTaskDto_NegativeBoardPosition_ShouldFailValidation()
    {
        // Arrange
        var dto = new UpdateTaskDto("Valid Title", "Desc", null, "Low", "Home", "Todo", -1.0);
        var validator = new UpdateTaskValidator();

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "BoardPosition");
    }

    [Fact]
    public void T1_UpdateTaskDto_CategoryTooLong_ShouldFailValidation()
    {
        // Arrange
        var longCategory = new string('E', 51);
        var dto = new UpdateTaskDto("Valid Title", "Desc", null, "Low", longCategory, "Todo", 1000.0);
        var validator = new UpdateTaskValidator();

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Category");
    }

    // ==========================================
    // Feature 4: Board Sorting & Position Logic
    // ==========================================

    [Fact]
    public void T1_BoardSorting_TasksSortedByStatusThenPosition_ShouldOrderCorrectly()
    {
        // Arrange
        var t1 = new TaskItem(TaskId.New(), "Task 1", null, null, TaskPriority.Medium, null, 2000.0);
        t1.UpdateStatus(TaskStatus.Todo);

        var t2 = new TaskItem(TaskId.New(), "Task 2", null, null, TaskPriority.Medium, null, 1000.0);
        t2.UpdateStatus(TaskStatus.Todo);

        var t3 = new TaskItem(TaskId.New(), "Task 3", null, null, TaskPriority.Medium, null, 500.0);
        t3.UpdateStatus(TaskStatus.InProgress);

        var list = new List<TaskItem> { t1, t2, t3 };

        // Act
        var sorted = list.OrderBy(t => t.Status).ThenBy(t => t.BoardPosition).ToList();

        // Assert
        sorted[0].Should().Be(t2); // Todo, 1000
        sorted[1].Should().Be(t1); // Todo, 2000
        sorted[2].Should().Be(t3); // InProgress, 500
    }

    [Fact]
    public void T1_BoardPositioning_NewTask_ShouldHavePositionOneThousandWhenEmpty()
    {
        // Arrange
        var existingTasks = new List<TaskItem>();

        // Act
        double nextPosition = existingTasks.Any() ? existingTasks.Max(t => t.BoardPosition) + 1000.0 : 1000.0;

        // Assert
        nextPosition.Should().Be(1000.0);
    }

    [Fact]
    public void T1_BoardPositioning_NextTask_ShouldBeMaxPositionPlusOneThousand()
    {
        // Arrange
        var existingTasks = new List<TaskItem>
        {
            new TaskItem(TaskId.New(), "Task 1", null, null, TaskPriority.Low, null, 1000.0),
            new TaskItem(TaskId.New(), "Task 2", null, null, TaskPriority.Low, null, 2500.0)
        };

        // Act
        double nextPosition = existingTasks.Any() ? existingTasks.Max(t => t.BoardPosition) + 1000.0 : 1000.0;

        // Assert
        nextPosition.Should().Be(3500.0);
    }

    [Fact]
    public void T1_BoardSorting_TasksWithSamePosition_ShouldOrderDeterministic()
    {
        // Arrange
        var guid1 = new Guid("11111111-1111-1111-1111-111111111111");
        var guid2 = new Guid("22222222-2222-2222-2222-222222222222");

        var t1 = new TaskItem(new TaskId(guid2), "Task 2", null, null, TaskPriority.Medium, null, 1000.0);
        var t2 = new TaskItem(new TaskId(guid1), "Task 1", null, null, TaskPriority.Medium, null, 1000.0);

        var list = new List<TaskItem> { t1, t2 };

        // Act
        var sorted = list.OrderBy(t => t.Status).ThenBy(t => t.BoardPosition).ThenBy(t => t.Id.Value).ToList();

        // Assert
        sorted[0].Should().Be(t2); // ID guid1 comes first
        sorted[1].Should().Be(t1); // ID guid2 comes second
    }

    [Fact]
    public void T1_BoardPositioning_TasksReordered_ShouldMaintainCorrectOrder()
    {
        // Arrange
        var t1 = new TaskItem(TaskId.New(), "T1", null, null, TaskPriority.Low, null, 1000.0);
        var t2 = new TaskItem(TaskId.New(), "T2", null, null, TaskPriority.Low, null, 2000.0);
        var t3 = new TaskItem(TaskId.New(), "T3", null, null, TaskPriority.Low, null, 3000.0);

        var list = new List<TaskItem> { t1, t2, t3 };

        // Act - Reorder t3 to be between t1 and t2
        t3.UpdatePosition((t1.BoardPosition + t2.BoardPosition) / 2.0); // 1500.0
        var sorted = list.OrderBy(t => t.BoardPosition).ToList();

        // Assert
        sorted[0].Should().Be(t1);
        sorted[1].Should().Be(t3);
        sorted[2].Should().Be(t2);
    }

    // ==========================================
    // Feature 5: AI Smart Parsing DTO & Fallback
    // ==========================================

    [Fact]
    public void T1_SmartParseRequest_EmptyText_ShouldFailValidation()
    {
        // Arrange
        var dto = new SmartParseRequestDto("");
        var validator = new SmartParseRequestValidator();

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Text");
    }

    [Fact]
    public void T1_SmartParseResponse_SuccessfulParse_ShouldContainAllFields()
    {
        // Arrange & Act
        var dto = new SmartParseResponseDto(
            IsParsed: true,
            Title: "Reunión de mockups",
            Description: "Reunión de mockups mañana a las 15:00",
            DueDate: DateTimeOffset.UtcNow.AddDays(1),
            Priority: "High",
            Category: "Meeting"
        );

        // Assert
        dto.IsParsed.Should().BeTrue();
        dto.Title.Should().Be("Reunión de mockups");
        dto.Description.Should().Be("Reunión de mockups mañana a las 15:00");
        dto.DueDate.Should().NotBeNull();
        dto.Priority.Should().Be("High");
        dto.Category.Should().Be("Meeting");
    }

    [Fact]
    public void T1_SmartParseResponse_FallbackParse_ShouldHaveIsParsedFalseAndDefaultValues()
    {
        // Arrange & Act
        var dto = new SmartParseResponseDto(
            IsParsed: false,
            Title: "Random text with no context",
            Description: null,
            DueDate: null,
            Priority: "Medium",
            Category: "General"
        );

        // Assert
        dto.IsParsed.Should().BeFalse();
        dto.Title.Should().Be("Random text with no context");
        dto.Description.Should().BeNull();
        dto.DueDate.Should().BeNull();
        dto.Priority.Should().Be("Medium");
        dto.Category.Should().Be("General");
    }

    [Fact]
    public void T1_SmartParseRequest_ValidText_ShouldPassValidation()
    {
        // Arrange
        var dto = new SmartParseRequestDto("Buy milk tomorrow");
        var validator = new SmartParseRequestValidator();

        // Act
        var result = validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void T1_SmartParseResponse_IsParsedTrue_ShouldPopulateTitleAndCategory()
    {
        // Arrange & Act
        var dto = new SmartParseResponseDto(
            IsParsed: true,
            Title: "Go to dentist",
            Description: null,
            DueDate: DateTimeOffset.UtcNow.AddDays(3),
            Priority: "Medium",
            Category: "Health"
        );

        // Assert
        dto.IsParsed.Should().BeTrue();
        dto.Title.Should().Be("Go to dentist");
        dto.Category.Should().Be("Health");
    }

    [Fact]
    public async Task T1_MockSmartParserService_ShouldParseUrgenteMañanaCorrectly()
    {
        // Arrange
        var service = new SmartTask.Infrastructure.Services.MockSmartParserService();
        var text = "Reunión de mockups mañana a las 15:00 urgente";

        // Act
        var result = await service.ParseAsync(text, default);

        // Assert
        result.IsParsed.Should().BeTrue();
        result.Title.Should().Be("Reunión de mockups");
        result.Priority.Should().Be("High");
        result.Category.Should().Be("Meeting");
        result.DueDate.Should().NotBeNull();
        result.DueDate!.Value.Hour.Should().Be(15);
        result.DueDate!.Value.Minute.Should().Be(0);
    }

    [Fact]
    public async Task T1_MockSmartParserService_ShouldFallbackForGarbageText()
    {
        // Arrange
        var service = new SmartTask.Infrastructure.Services.MockSmartParserService();
        var text = "asdfghjklqwertyuiop";

        // Act
        var result = await service.ParseAsync(text, default);

        // Assert
        result.IsParsed.Should().BeFalse();
        result.Title.Should().Be(text);
        result.Priority.Should().Be("Medium");
        result.Category.Should().Be("General");
        result.DueDate.Should().BeNull();
    }
}
