# Swagger UI & Health Checks Setup

Configure Swagger UI as your default landing page with health checks to avoid white page on startup.

## Overview

**Without Setup**: App starts → navigate to `/swagger` manually → white page is confusing
**With Setup**: App starts → Swagger UI loads automatically → health check available → dev link to debug

## Complete Setup

### 1. Installation

```bash
dotnet add package Swashbuckle.AspNetCore
dotnet add package AspNetCore.HealthChecks.UI
dotnet add package AspNetCore.HealthChecks.SqlServer
```

### 2. Program.cs Configuration

```csharp
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplicationBuilder.CreateBuilder(args);

// ============ PORT CONFIGURATION ============

var port = builder.Configuration.GetValue<int?>("PORT") 
    ?? int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "5000");

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(port);
});

// ============ SWAGGER SETUP ============

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "API documentation"
    });

    // Add XML comments (optional)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ============ HEALTH CHECKS ============

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy());

// Add database health check (if using SQL Server)
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "");

// ============ SERVICES ============

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// ============ MIDDLEWARE ============

// Health checks endpoint
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Enable Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
        
        // Set Swagger as default landing page
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ============ STARTUP MESSAGE ============

var env = app.Environment.EnvironmentName;
Console.WriteLine($"\n=== API Started ===");
Console.WriteLine($"Environment: {env}");
Console.WriteLine($"Port: {port}");
if (app.Environment.IsDevelopment())
{
    Console.WriteLine($"📄 Swagger: http://localhost:{port}/");
    Console.WriteLine($"❤️  Health: http://localhost:{port}/health");
}
Console.WriteLine($"===================\n");

app.Run();
```

### 3. appsettings.json Configuration

```json
{
  "PORT": 5000,
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MyDb;Trusted_Connection=true;"
  }
}
```

### 4. appsettings.Development.json

```json
{
  "PORT": 5000,
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

### 5. appsettings.Production.json

```json
{
  "PORT": 80,
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

## Port Configuration Options

### Option 1: Environment Variable (Recommended)

```bash
# Linux/Mac
export PORT=8080
dotnet run

# Windows
set PORT=8080
dotnet run

# Or in PowerShell
$env:PORT=8080
dotnet run
```

### Option 2: Command Line Argument

```bash
dotnet run -- --port 8080
```

### Option 3: Docker Environment

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY . /app
WORKDIR /app
ENV PORT=80
EXPOSE 80
ENTRYPOINT ["dotnet", "MyApi.dll"]
```

### Option 4: appsettings.json

```json
{
  "PORT": 8080
}
```

## Startup Output

```
=== API Started ===
Environment: Development
Port: 5000
📄 Swagger: http://localhost:5000/
❤️  Health: http://localhost:5000/health
===================
```

## Health Check Response

```json
{
  "status": "Healthy",
  "checks": {
    "self": {
      "status": "Healthy"
    },
    "SqlServer": {
      "status": "Healthy",
      "description": "OK",
      "duration": "00:00:00.0123456"
    }
  },
  "totalDuration": "00:00:00.0456789"
}
```

## Development vs Production

### Development Environment
✅ Swagger UI available at root: `http://localhost:5000/`
✅ Health check endpoint: `http://localhost:5000/health`
✅ Debug logging enabled
✅ Auto-starts with clear landing page

### Production Environment
❌ Swagger disabled (remove if statement)
✅ Health check still available: `/health`
✅ Minimal logging
✅ Clean startup

## Custom Health Check Endpoint

```csharp
// Custom health check
public class CustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check something (database, external service, etc)
            var isHealthy = await SomeCheckAsync();
            
            if (isHealthy)
                return HealthCheckResult.Healthy("Service is operational");
            else
                return HealthCheckResult.Unhealthy("Service degraded");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Service unavailable", ex);
        }
    }
}

// Register
builder.Services.AddHealthChecks()
    .AddCheck<CustomHealthCheck>("custom-check");
```

## Multiple Swagger Versions

```csharp
// In Program.cs
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "My API", 
        Version = "v1" 
    });
    options.SwaggerDoc("v2", new OpenApiInfo 
    { 
        Title = "My API", 
        Version = "v2" 
    });
});

// In UseSwaggerUI
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "API V2");
    options.RoutePrefix = string.Empty;
});
```

## Troubleshooting

### White Page on Startup
**Cause**: Swagger not configured as default route
**Fix**: Add `options.RoutePrefix = string.Empty;`

### Health Check Shows Unhealthy
**Cause**: Database connection string incorrect
**Fix**: Verify connection string in appsettings.json

### Port Already in Use
**Cause**: Port 5000 already taken
**Fix**: Set PORT environment variable to different port

### Swagger Not Showing in Production
**Cause**: Swagger wrapped in `if (app.Environment.IsDevelopment())`
**Fix**: Remove condition if you want Swagger in production (not recommended)

## Security Considerations

### Development Only
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = string.Empty;
    });
}
```

### With Authentication (Production)
```csharp
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = string.Empty;
    options.EnableDeepLinking();
    
    // Add JWT auth to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
});
```

## Health Check Monitoring

Use health check endpoint for:
- **Kubernetes probes** - liveness and readiness checks
- **Load balancers** - route health validation
- **Monitoring dashboards** - uptime tracking
- **Automated alerts** - notify on unhealthy status

```yaml
# Kubernetes example
livenessProbe:
  httpGet:
    path: /health
    port: 5000
  initialDelaySeconds: 5
  periodSeconds: 10
```

---

**See Also**:
- [Swagger Documentation](https://swagger.io/)
- [Health Checks Guide](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)
- [Kestrel Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)
