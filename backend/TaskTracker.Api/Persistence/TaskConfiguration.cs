using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Api.Tasks;

namespace TaskTracker.Api.Persistence;

public sealed class TaskConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> task)
    {
        // Enforce valid task values in PostgreSQL as well as in API validation.
        task.ToTable("Tasks", table =>
        {
            table.HasCheckConstraint("CK_Tasks_Title", "length(btrim(\"Title\")) > 0 AND \"Title\" = btrim(\"Title\")");
            table.HasCheckConstraint("CK_Tasks_Status", "\"Status\" IN ('Todo', 'InProgress', 'Done')");
            table.HasCheckConstraint("CK_Tasks_Priority", "\"Priority\" IN ('Low', 'Medium', 'High')");
        });
        task.HasKey(x => x.Id);
        task.Property(x => x.Title).HasMaxLength(150).IsRequired();
        task.Property(x => x.Description).HasMaxLength(2000);
        task.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        task.Property(x => x.Priority).HasConversion<string>().HasMaxLength(10);
        task.HasIndex(x => new { x.CreatedAt, x.Id });
        task.HasIndex(x => new { x.Status, x.CreatedAt, x.Id });
    }
}
