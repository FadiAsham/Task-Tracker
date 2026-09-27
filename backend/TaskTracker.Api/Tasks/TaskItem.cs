namespace TaskTracker.Api.Tasks;

public enum WorkStatus { Todo, InProgress, Done }
public enum TaskPriority { Low, Medium, High }

public sealed class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public WorkStatus Status { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

