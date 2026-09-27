using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TaskTracker.Api.Infrastructure;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger, IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var conflict = exception is DbUpdateConcurrencyException;
        // Log the full error on the server, but return a safe message and trace ID to the client.
        logger.LogError(exception, "Request {Method} {Path} failed. Trace: {TraceId}",
            context.Request.Method, context.Request.Path, context.TraceIdentifier);
        context.Response.StatusCode = conflict ? 409 : 500;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = context.Response.StatusCode,
                Title = conflict ? "The task changed. Refresh and try again." : "An unexpected error occurred.",
                Extensions = { ["traceId"] = context.TraceIdentifier }
            }
        });
    }
}
