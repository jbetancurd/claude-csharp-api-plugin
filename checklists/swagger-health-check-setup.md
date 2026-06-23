# Swagger UI & Health Check Setup Checklist

Use this checklist when configuring Swagger and health checks in your ASP.NET Core API.

## Pre-Setup

- [ ] .NET SDK 6.0+ installed
- [ ] Project created with `dotnet new webapi`
- [ ] Solution structure follows Onion Architecture

## 1. Install NuGet Packages

```bash
dotnet add package Swashbuckle.AspNetCore
dotnet add package AspNetCore.HealthChecks.UI
```

For database health checks (choose one):
```bash
# SQL Server
dotnet add package AspNetCore.HealthChecks.SqlServer

# PostgreSQL
dotnet add package AspNetCore.HealthChecks.NpgSql

# SQLite
dotnet add package AspNetCore.HealthChecks.Sqlite

# MySQL
dotnet add package AspNetCore.HealthChecks.MySql
```

- [ ] Swashbuckle.AspNetCore installed
- [ ] HealthChecks.UI installed
- [ ] Database health check package installed (if needed)

## 2. Configure Program.cs

### Swagger Service Registration

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1",
        Description = "API documentation"
    });
});
```

- [ ] `AddSwaggerGen` registered in services
- [ ] `SwaggerDoc` configured with title and version
- [ ] Contact info added (optional)

### Health Checks Service Registration

```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"))
    .AddSqlServer(connectionString);  // If using database
```

- [ ] `AddHealthChecks()` registered
- [ ] Self-check added
- [ ] Database health check added (if applicable)

### Port Configuration

```csharp
var port = builder.Configuration.GetValue<int?>("PORT")
    ?? int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "5000");

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(port);
});
```

- [ ] Port configuration from environment/config
- [ ] Kestrel configured to listen on port
- [ ] Port defaults to 5000 if not specified

## 3. Configure Middleware Pipeline

### Health Checks Endpoint

```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

- [ ] Health checks endpoint mapped to `/health`
- [ ] `UIResponseWriter` used for JSON response
- [ ] Placed **before** Swagger middleware

### Swagger UI Middleware (Development Only)

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        options.RoutePrefix = string.Empty;  // Root landing page
    });
}
```

- [ ] Swagger wrapped in `IsDevelopment()` check
- [ ] `UseSwagger()` middleware added
- [ ] `UseSwaggerUI()` configured
- [ ] `RoutePrefix = string.Empty` sets root path
- [ ] `SwaggerEndpoint` points to correct swagger.json

### Standard Middleware Order

```
CORS → Health Checks → Swagger UI → HTTPS Redirect → Auth → Route Mapping
```

- [ ] CORS middleware configured (if needed)
- [ ] Health checks before Swagger UI
- [ ] Swagger UI before HTTPS redirect
- [ ] Standard middleware after API setup

## 4. Startup Message

```csharp
var environment = app.Environment.EnvironmentName;
Console.WriteLine($@"
╔════════════════════════════════════╗
║        API Started Successfully    ║
╚════════════════════════════════════╝

🌍 Environment:  {environment}
🔌 Port:         {port}
🌐 Base URL:     http://localhost:{port}
{(app.Environment.IsDevelopment() ? $@"📄 Swagger:      http://localhost:{port}/
❤️  Health:       http://localhost:{port}/health" : "")}

════════════════════════════════════
");
```

- [ ] Console output shows environment
- [ ] Port displayed in startup message
- [ ] Development mode shows Swagger/Health links
- [ ] Production mode hides documentation links

## 5. Configuration Files

### appsettings.json

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

- [ ] Default PORT set to 5000
- [ ] Logging configured
- [ ] Connection string included (if using database)

### appsettings.Development.json

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

- [ ] Development port specified (optional override)
- [ ] Debug logging enabled
- [ ] ASP.NET Core logging limited (to reduce noise)

### appsettings.Production.json

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

- [ ] Production port set to 80 (or as needed)
- [ ] Swagger disabled (not in code for prod)
- [ ] Warning-level logging only

## 6. Test Startup

```bash
# Development
dotnet run

# Verify outputs:
# ✅ Console shows startup message
# ✅ http://localhost:5000/ → Swagger UI loads
# ✅ http://localhost:5000/health → JSON health response
```

- [ ] `dotnet run` starts without errors
- [ ] Console shows formatted startup message
- [ ] Port number appears in console
- [ ] Swagger/Health URLs shown for development

## 7. Verify Swagger UI

### Manual Testing

```bash
# Start API
dotnet run

# In browser:
# 1. Visit http://localhost:5000/
# 2. Swagger UI should load
# 3. All endpoints listed
# 4. Can expand/collapse endpoints
# 5. Can try endpoints with "Try it out"
```

- [ ] Swagger UI loads at root path
- [ ] API endpoints visible in UI
- [ ] Models section shows DTOs
- [ ] "Try it out" feature works

### Security Setup

```csharp
// If using JWT authentication, add to Swagger
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT"
});
```

- [ ] Security scheme defined (if using auth)
- [ ] Bearer token field visible in Swagger UI
- [ ] Can test authenticated endpoints

## 8. Verify Health Check

```bash
# Curl the health endpoint
curl http://localhost:5000/health

# Response should be:
{
  "status": "Healthy",
  "checks": {
    "self": {
      "status": "Healthy"
    },
    "database": {
      "status": "Healthy"
    }
  },
  "totalDuration": "00:00:00.1234567"
}
```

- [ ] Health endpoint responds with JSON
- [ ] "status" field shows "Healthy" or "Unhealthy"
- [ ] All checks listed with status
- [ ] Timing information included

## 9. Docker Deployment

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
COPY . /app
WORKDIR /app
ENV PORT=80
EXPOSE 80
ENTRYPOINT ["dotnet", "MyApi.dll"]
```

- [ ] Environment variable PORT set
- [ ] EXPOSE matches PORT
- [ ] Correct .NET image version

### Docker Compose (Optional)

```yaml
version: '3'
services:
  api:
    build: .
    ports:
      - "8080:80"
    environment:
      - PORT=80
      - ASPNETCORE_ENVIRONMENT=Production
```

- [ ] Port mapping configured
- [ ] Environment variables set
- [ ] ASPNETCORE_ENVIRONMENT specified

## 10. Production Hardening

### Before Deploying to Production

```csharp
// ❌ REMOVE THIS (production should not expose Swagger)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(...);
}
```

- [ ] Swagger middleware wrapped in `IsDevelopment()` check
- [ ] Swagger NOT exposed in production
- [ ] Health check STILL available at `/health`
- [ ] No sensitive information in startup logs

### Security Checklist

- [ ] Swagger disabled in production
- [ ] HTTPS enabled (`.UseHttpsRedirection()`)
- [ ] CORS properly configured (not wildcard)
- [ ] Authentication middleware in place
- [ ] API version in health check response (optional)

## 11. Troubleshooting

### Issue: White Page on Startup
**Solution**: Add `options.RoutePrefix = string.Empty;` to SwaggerUI configuration

- [ ] RoutePrefix set to empty string
- [ ] Swagger reachable at root path

### Issue: Health Check Returns Unhealthy
**Solution**: Verify database connection string

- [ ] Connection string correct in appsettings.json
- [ ] Database running and accessible
- [ ] Credentials correct

### Issue: Port Already in Use
**Solution**: Change PORT environment variable or check for process using port

```bash
# Linux/Mac
lsof -i :5000

# Windows
netstat -ano | findstr :5000
```

- [ ] Port available before startup
- [ ] No other process using PORT
- [ ] Firewall allows port access

### Issue: Swagger Shows "Loading API definition"
**Solution**: Ensure swagger.json endpoint is working

- [ ] Swagger middleware enabled (IsDevelopment)
- [ ] Check browser console for 404 errors
- [ ] Verify port in swagger endpoint URL matches actual port

## Testing Checklist

### Manual Testing
- [ ] Start API in development mode
- [ ] Visit Swagger UI at root path
- [ ] Expand an endpoint
- [ ] Click "Try it out"
- [ ] Send test request
- [ ] Verify response
- [ ] Check health endpoint returns 200

### Automated Testing
```csharp
[Fact]
public async Task SwaggerEndpoint_Returns200()
{
    var response = await _client.GetAsync("/swagger/v1/swagger.json");
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
}

[Fact]
public async Task HealthEndpoint_ReturnsHealthy()
{
    var response = await _client.GetAsync("/health");
    Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    Assert.Contains("Healthy", await response.Content.ReadAsStringAsync());
}
```

- [ ] Unit tests for Swagger endpoint
- [ ] Unit tests for health endpoint
- [ ] Integration tests with real startup

## Documentation

### API Documentation
- [ ] All endpoints have XML documentation comments
- [ ] All DTOs have property descriptions
- [ ] Example requests/responses shown in Swagger
- [ ] Error codes documented (400, 404, 500, etc)

### Deployment Documentation
- [ ] Port configuration documented
- [ ] Environment variables listed
- [ ] Startup message shown in README
- [ ] Health check usage explained
- [ ] Swagger access instructions (dev only)

---

## Related Documentation

- [Swagger Health Checks Guide](../docs/middleware/swagger-health-checks.md)
- [Program.cs Template](../templates/shared/middleware/program-swagger-health.template.cs)
- [Architecture Decisions](../docs/decision-tree.md#step-11-swagger-ui--health-checks-configuration)
