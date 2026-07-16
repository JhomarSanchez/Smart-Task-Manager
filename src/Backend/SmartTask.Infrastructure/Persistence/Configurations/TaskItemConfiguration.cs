using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTask.Domain.Entities;
using SmartTask.Domain.Common;

namespace SmartTask.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("TaskItems");

        // Primary Key configuration
        builder.HasKey(t => t.Id);

        // Convert strongly-typed TaskId to Guid for database mapping
        builder.Property(t => t.Id)
            .HasConversion(
                id => id.Value,
                value => new TaskId(value))
            .IsRequired();

        builder.Property(t => t.Title)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.DueDate);

        // Category supports board:column naming format (up to 100 chars)
        builder.Property(t => t.Category)
            .HasMaxLength(100);

        // Map enum to integer in PostgreSQL
        builder.Property(t => t.Priority)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(t => t.BoardPosition)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.UpdatedAt);
        
        // Add index on status and position for query efficiency when loading columns
        builder.HasIndex(t => new { t.Status, t.BoardPosition });
    }
}
