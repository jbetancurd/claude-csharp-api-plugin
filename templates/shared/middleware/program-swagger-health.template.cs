// Template: Program.cs with Swagger UI & Health Checks
// Default landing page, health check endpoint, configurable port

using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplicationBuilder.CreateBuilder(args);

// ============ PORT CONFIGURATION ============
// Priority: Environment Variable > appsettings.json > Default (5000)

var port = builder.Configuration.GetValue<int?>("PORT")
    ?? int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "5000");

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(port);
});

// ============ SWAGGER CONFIGURATION ============

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "Production-grade C# API with architecture best practices",
        Contact = new OpenApiContact
        {
            Name = "Your Name",
            Email = "your.email@example.com"
        }
    });

    // Include XML documentation comments (optional)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add security definition for JWT (if using authentication)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============ HEALTH CHECKS CONFIGURATION ============

builder.Services.AddHealthChecks()
    // Self check
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"))

    // Database check (SQL Server example)
    .AddSqlServer(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection") ?? "",
        name: "database",
        tags: new[] { "db", "sql" }
    );

// ============ APPLICATION SERVICES ============

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add CORS if needed
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// ============ MIDDLEWARE PIPELINE ============

// CORS
app.UseCors("AllowAll");

// Health checks endpoint
// Available at: /health or /health/ready or /health/live
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    AllowCachingResponses = false
});

// Swagger UI - only in development
if (app.Environment.IsDevelopment())
{
    // Enable Swagger middleware
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "swagger/{documentName}/swagger.json";
    });

    // Configure Swagger UI
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");

        // Set Swagger UI as default landing page (root path)
        options.RoutePrefix = string.Empty;

        // Swagger UI settings
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.DefaultModelsExpandDepth(1);
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
    });
}

// Standard middleware
app.UseHttpsRedirection();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// ============ STARTUP MESSAGE ============

var environment = app.Environment.EnvironmentName;
var startupMessage = $@"
╔════════════════════════════════════╗
║        API Started Successfully    ║
╚════════════════════════════════════╝

🌍 Environment:  {environment}
🔌 Port:         {port}
🌐 Base URL:     http://localhost:{port}
{(app.Environment.IsDevelopment() ? $@"📄 Swagger:      http://localhost:{port}/
❤️  Health:       http://localhost:{port}/health" : "")}

════════════════════════════════════
";

Console.WriteLine(startupMessage);

app.Run();

// ============ OPTIONAL: HELPER METHODS ============

// Example custom health check
public class CustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Add your health check logic here
            // Example: check external service, cache, etc.

            return await Task.FromResult(HealthCheckResult.Healthy("All systems operational"));
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}
