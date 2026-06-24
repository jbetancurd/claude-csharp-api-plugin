// Complete Program.cs Template with Landing Page, Health Checks, and Swagger
// This is a full working example for a REST API

using YourApi.Domain.Repositories;
using YourApi.Infrastructure.Persistence;
using YourApi.Presentation.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplicationBuilder.CreateBuilder(args);

// ============================================================
// 1. ADD SERVICES
// ============================================================

// Controllers
builder.Services.AddControllers();

// Database (Example: EF Core with SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Dependency Injection - Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Dependency Injection - Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"))
    .AddDbContextCheck<ApplicationDbContext>("database");

// Swagger/OpenAPI
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Your API",
        Version = "v1.0.0",
        Description = "Production-grade REST API with Onion Architecture",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });
});

// CORS (if needed)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ============================================================
// 2. BUILD AND CONFIGURE PIPELINE
// ============================================================

var app = builder.Build();

// Environment info
var isDevelopment = app.Environment.IsDevelopment();

// ============================================================
// 3. MIDDLEWARE PIPELINE - ORDER MATTERS!
// ============================================================

// Exception handling
app.UseExceptionHandler("/error");

// Landing Page Middleware - MUST BE FIRST!
app.UseMiddleware<LandingPageMiddleware>();

// Security headers (recommended)
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    await next();
});

// HTTPS redirection (production)
if (!isDevelopment)
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// CORS
app.UseCors("AllowAll");

// Routing
app.UseRouting();

// ============================================================
// 4. ENDPOINT MAPPING
// ============================================================

// Health checks endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse
});

// Swagger (Development only)
if (isDevelopment)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        options.RoutePrefix = "swagger";  // Access at /swagger
        options.DocumentTitle = "Your API Documentation";
        options.DefaultModelsExpandDepth(2);
        options.DefaultModelExpandDepth(2);
    });

    // Add development endpoints
    app.MapGet("/", async context =>
    {
        // This is overridden by LandingPageMiddleware
        await context.Response.WriteAsync("Landing page handled by middleware");
    });
}

// API Controllers
app.MapControllers();

// Error endpoint (for exception handler)
app.MapGet("/error", () =>
    Results.Problem("An error occurred", statusCode: StatusCodes.Status500InternalServerError))
    .ExcludeFromDescription();

// Not Found handler
app.MapFallback(() =>
    Results.Problem("Not found", statusCode: StatusCodes.Status404NotFound))
    .ExcludeFromDescription();

// ============================================================
// 5. DATABASE INITIALIZATION (Optional)
// ============================================================

try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Apply migrations automatically
        await context.Database.MigrateAsync();

        // Seed data (optional)
        // await context.SeedDataAsync();
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Database initialization failed");
}

// ============================================================
// 6. STARTUP MESSAGE
// ============================================================

var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("""
    ╔════════════════════════════════════════════════════╗
    ║          API Started Successfully                 ║
    ╚════════════════════════════════════════════════════╝

    🌍 Environment:    {Environment}
    🔌 Port:           {Port}
    🌐 Base URL:       http://localhost:{Port}
    📄 Landing Page:   http://localhost:{Port}/
    ❤️  Health:         http://localhost:{Port}/health
    {SwaggerMessage}

    ════════════════════════════════════════════════════
    """,
    app.Environment.EnvironmentName,
    app.Urls.FirstOrDefault()?.Split(':').Last() ?? "5000",
    app.Urls.FirstOrDefault()?.Split(':').Last() ?? "5000",
    isDevelopment ? $"📖 Swagger:      http://localhost:{app.Urls.FirstOrDefault()?.Split(':').Last() ?? "5000"}/swagger" : "");

// ============================================================
// 7. RUN APPLICATION
// ============================================================

app.Run();


// ============================================================
// HEALTH CHECK RESPONSE WRITER
// ============================================================

public static class HealthCheckResponseWriter
{
    public static Task WriteDetailedResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                duration = entry.Value.Duration.TotalMilliseconds,
                message = entry.Value.Description,
                error = entry.Value.Exception?.Message
            })
        };

        var json = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });

        return context.Response.WriteAsync(json);
    }
}
