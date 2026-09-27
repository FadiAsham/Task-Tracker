using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace TaskTracker.Api.Tasks;

// Fields the client can send when creating or editing a task. Creator and timestamps are set by the server.
public sealed class SaveTaskRequest
{
    // Trim before validation so spaces alone do not count as a title.
    private string title = "";
    [Description("Required task title, trimmed before validation. 1-150 characters.")]
    [Required, StringLength(150)]
    public string Title { get => title; set => title = value?.Trim() ?? ""; }
    [Description("Optional description; maximum 2000 characters.")]
    [StringLength(2000)] public string? Description { get; set; }
    [Description("Todo (default), InProgress, or Done. JSON strings only.")]
    [EnumDataType(typeof(WorkStatus))] public WorkStatus Status { get; set; }
    [Description("Low, Medium (default), or High. JSON strings only.")]
    [EnumDataType(typeof(TaskPriority))] public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    [Description("Optional calendar date in YYYY-MM-DD format. Past dates are allowed.")]
    public DateOnly? DueDate { get; set; }
}

// Optional URL parameters for filtering and paging the task list.
public sealed class TaskQuery
{
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 100)] public int PageSize { get; set; } = 20;
    [EnumDataType(typeof(WorkStatus))] public WorkStatus? Status { get; set; }
}

// Data returned to the frontend;.
// the internal creator ID is not exposed.
public sealed record TaskResponse(Guid Id, string Title, string? Description, WorkStatus Status,
    TaskPriority Priority, DateOnly? DueDate, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, string? CreatedByName)
{
    public static TaskResponse From(TaskItem task) => new(task.Id, task.Title, task.Description,
        task.Status, task.Priority, task.DueDate, task.CreatedAt, task.UpdatedAt, task.CreatedByName);
}
// Includes the total matching count so the frontend can calculate the number of pages.
public sealed record TaskPage(IReadOnlyList<TaskResponse> Items, int TotalCount, int Page, int PageSize);

