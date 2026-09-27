using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Api.Persistence;
using TaskTracker.Api.Tasks;
using TaskTracker.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;


var builder = WebApplication.CreateBuilder(args);
// Register controllers and the MVC services needed to validate antiforgery tokens.
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute())).AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer((document, context, cancellationToken) =>
{
    document.Info.Title = "Task Tracker API";
    document.Info.Version = "v1";
    document.Info.Description = "Manage shared tasks with status, priority, and optional due dates. " +
        "Tasks require sign-in. Mutations require an X-XSRF-TOKEN header from /api/auth/me. Dates use YYYY-MM-DD; timestamps use UTC. " +
        "Requests use string enums. Errors use Problem Details. Try-it requests modify the connected database.";
    return Task.CompletedTask;
}));
builder.Services.AddDbContext<TaskDbContext>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("Tasks")
    ?? throw new InvalidOperationException("Configure ConnectionStrings:Tasks using user secrets or environment variables.")));
builder.Services.AddScoped<TaskService>();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-XSRF-TOKEN");
builder.Services.AddAuthorization();
// Cookies identify API requests; OpenID Connect handles sign-in with Keycloak.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TaskTracker.Session";
        // Let Angular handle an expired session instead of redirecting an API call to a login page.
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    })
    .AddOpenIdConnect(options =>
    {
        builder.Configuration.GetSection("Keycloak").Bind(options);
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect((builder.Configuration["Frontend:Origin"] ?? "http://localhost:4200")
                + "/tasks?authError=true");
            return Task.CompletedTask;
        };
    });
var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
// Identify the user before checking whether they can access the endpoint.
app.UseAuthentication();
app.UseAuthorization();



if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", options => options.WithTitle("Task Tracker API"));
}
app.MapControllers();
app.Run();




