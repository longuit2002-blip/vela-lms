using Lms.Api;
using Lms.Api.Auth;
using Lms.Api.Endpoints;
using Lms.Application;
using Lms.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Missing connection string 'Postgres'.");

// Composition root — the only place Infrastructure is wired into the app.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddAuthInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

const string webCorsPolicy = "web";
var webOrigin = builder.Configuration["Cors:WebOrigin"] ?? "http://localhost:3000";
builder.Services.AddCors(options => options.AddPolicy(webCorsPolicy, policy =>
    policy.WithOrigins(webOrigin).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// Liveness = process up (no dependencies). Readiness = Postgres reachable (the dependency the
// skeleton actually uses). Redis/MinIO get non-gating checks in a later phase so an unused
// dependency being down never fails readiness.
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<Lms.Api.Health.DatabaseReadyHealthCheck>("postgres", tags: ["ready"]);

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Dev-only: provision the first org + OrgOwner when seeding is enabled (no-op otherwise).
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<Lms.Infrastructure.Seeding.IdentitySeeder>().SeedAsync();
}

app.UseCors(webCorsPolicy);

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ForcedPasswordChangeMiddleware>();

app.MapAuthEndpoints();
app.MapOrganizationEndpoints();
app.MapDepartmentEndpoints();
app.MapPositionEndpoints();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

app.Run();

// Exposed for WebApplicationFactory in integration tests (U7).
public partial class Program;
