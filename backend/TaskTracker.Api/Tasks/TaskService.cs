using Microsoft.EntityFrameworkCore;
using TaskTracker.Api.Persistence;

namespace TaskTracker.Api.Tasks;

public sealed class TaskService(TaskDbContext db)
{
    public async Task<TaskPage> ListAsync(TaskQuery request, CancellationToken cancellationToken)
    {
        // This query only reads tasks, so EF does not need to track changes.
        var query = db.Tasks.AsNoTracking();
        if (request.Status is { } status) query = query.Where(x => x.Status == status);
        var count = await query.CountAsync(cancellationToken);
        // Use a long to avoid overflow when a very large page number is requested.
        var offset = ((long)request.Page - 1) * request.PageSize;
        if (offset >= count) return new TaskPage([], count, request.Page, request.PageSize);
        // The ID breaks ties when tasks have the same creation time.
        var tasks = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip((int)offset).Take(request.PageSize).ToListAsync(cancellationToken);
        return new TaskPage(tasks.Select(TaskResponse.From).ToArray(), count, request.Page, request.PageSize);
    }

    public async Task<TaskResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return task is null ? null : TaskResponse.From(task);
    }

    public async Task<TaskResponse> CreateAsync(SaveTaskRequest request, string creatorId, string creatorName, CancellationToken cancellationToken)
    {
        var task = new TaskItem { CreatedAt = UtcNowMicroseconds(), CreatedById = creatorId, CreatedByName = creatorName };
        Apply(task, request);
        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        return TaskResponse.From(task);
    }

    public async Task<TaskResponse?> UpdateAsync(Guid id, SaveTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await db.Tasks.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (task is null) return null;
        Apply(task, request);
        await db.SaveChangesAsync(cancellationToken);
        return TaskResponse.From(task);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Tasks.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;

    private static DateTimeOffset UtcNowMicroseconds()
    {
        // PostgreSQL stores timestamps at microsecond precision.
        var now = DateTimeOffset.UtcNow;
        return now.AddTicks(-(now.Ticks % 10));
    }

    // Copy only editable fields, preserving the original creator and creation time.
    private static void Apply(TaskItem task, SaveTaskRequest request)
    {
        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.DueDate = request.DueDate;
        task.UpdatedAt = UtcNowMicroseconds();
    }
}

