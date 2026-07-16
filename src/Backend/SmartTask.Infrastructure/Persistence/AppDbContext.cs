using Microsoft.EntityFrameworkCore;
using SmartTask.Domain.Entities;

namespace SmartTask.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Dynamically register all configurations implementing IEntityTypeConfiguration in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
