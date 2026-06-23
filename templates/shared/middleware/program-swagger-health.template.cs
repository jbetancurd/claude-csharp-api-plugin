// Template: Program.cs with Swagger UI & Health Checks
// Default landing page at root, health check endpoint, configurable port

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

    // Database check (SQL Server example) - Comment out if not using SQL Server
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

// Add static files to serve default.html
builder.Services.AddStaticFiles();

var app = builder.Build();

// ============ MIDDLEWARE PIPELINE (ORDER MATTERS!) ============

// 1. HTTPS Redirect (first, before any other middleware)
app.UseHttpsRedirection();

// 2. CORS (before auth and endpoints)
app.UseCors("AllowAll");

// 3. Static files (serve default.html at root in production)
if (!app.Environment.IsDevelopment())
{
    app.UseStaticFiles(new StaticFileOptions
    {
        DefaultContentType = "text/html"
    });
}

// 4. Swagger UI - only in development
if (app.Environment.IsDevelopment())
{
    // Enable Swagger middleware (must come before default file route)
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "swagger/{documentName}/swagger.json";
    });

    // Configure Swagger UI as root landing page
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");

        // IMPORTANT: Set empty string to make Swagger UI default at root
        options.RoutePrefix = string.Empty;

        // Swagger UI customization
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
        options.DefaultModelsExpandDepth(1);
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.CustomHeadContent = "<title>API Documentation</title>";
    });
}
else
{
    // Production: Serve default.html at root
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        DefaultFileNames = new List<string> { "default.html" }
    });
    app.UseStaticFiles();
}

// 5. Authorization (before endpoints)
app.UseAuthorization();

// 6. Health check endpoint - Map BEFORE controllers
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    AllowCachingResponses = false
});

// Also add JSON health check endpoint
app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// 7. Map controllers (last)
app.MapControllers();

// 8. Fallback route (if no route matches, return default page or health check)
if (!app.Environment.IsDevelopment())
{
    app.MapFallback(async context =>
    {
        // If requesting root, return default.html
        if (context.Request.Path == "/")
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(
                Path.Combine(app.Environment.WebRootPath, "default.html")
            );
        }
        else
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsJsonAsync(new { error = "Not found" });
        }
    });
}

// ============ STARTUP MESSAGE ============

var environment = app.Environment.EnvironmentName;
var startupMessage = $@"
╔════════════════════════════════════╗
║        API Started Successfully    ║
╚════════════════════════════════════╝

🌍 Environment:  {environment}
🔌 Port:         {port}
🌐 Base URL:     http://localhost:{port}
{(app.Environment.IsDevelopment()
    ? $@"📄 Swagger UI:    http://localhost:{port}/ (root landing page)
📊 API Docs:     http://localhost:{port}/swagger/v1/swagger.json
❤️  Health Check:  http://localhost:{port}/health"
    : $@"🏠 Home Page:     http://localhost:{port}/ (default.html)
❤️  Health Check:  http://localhost:{port}/health")}

════════════════════════════════════
";

Console.WriteLine(startupMessage);

app.Run();

// ============ OPTIONAL: CUSTOM HEALTH CHECK ============

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

            return await Task.FromResult(
                HealthCheckResult.Healthy("All systems operational")
            );
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Health check failed", ex);
        }
    }
}
