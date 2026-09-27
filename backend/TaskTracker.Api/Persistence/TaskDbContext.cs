using Microsoft.EntityFrameworkCore;
using TaskTracker.Api.Tasks;

namespace TaskTracker.Api.Persistence;

public sealed class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfiguration(new TaskConfiguration());
}
