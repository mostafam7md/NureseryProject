using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using NurseryLink.Api;
using NurseryLink.Api.Authorization;
using NurseryLink.Api.Middleware;
using NurseryLink.Application;
using NurseryLink.Infrastructure;
using NurseryLink.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Authorization -------------------------------------------------------------------------
// All of it lives here, in one place. The custom policy provider must out-live the TryAdd inside
// AddAuthorization; keeping both calls in the same file makes that ordering visible instead of
// depending on what AddInfrastructure happened to register.
builder.Services.AddSingleton<IAuthorizationHandler, PrivilegeAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PrivilegePolicyProvider>();
builder.Services.AddAuthorization(options =>
{
    // Secure by default: every endpoint requires authentication unless it opts out explicitly.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ---- Rate limiting -------------------------------------------------------------------------
// Login is the one anonymous endpoint that does real work, so it is also the one worth throttling.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(RateLimitPolicies.Login, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

var app = builder.Build();

await DatabaseInitializer.InitializeAsync(app.Services);

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    // Without AllowAnonymous the fallback policy above makes the OpenAPI document itself 401.
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }))
    .AllowAnonymous();

app.Run();

public partial class Program;
