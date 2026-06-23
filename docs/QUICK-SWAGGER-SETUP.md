# Quick Swagger UI Setup (5 Minutes)

Get Swagger UI running as your default landing page with health checks.

## Step 1: Install Packages (1 min)

```bash
dotnet add package Swashbuckle.AspNetCore
dotnet add package AspNetCore.HealthChecks.UI
```

## Step 2: Copy Program.cs Code (2 min)

Replace your `Program.cs` with the template:
```bash
# Copy from:
templates/shared/middleware/program-swagger-health.template.cs
```

Or manually add these sections to your existing `Program.cs`:

### A) Port Configuration
```csharp
var port = builder.Configuration.GetValue<int?>("PORT")
    ?? int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "5000");

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(port);
});
```

### B) Swagger Service
```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "My API",
        Version = "v1"
    });
});
```

### C) Health Checks
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"));
```

### D) Health Endpoint
```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### E) Swagger UI Middleware
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        options.RoutePrefix = string.Empty;  // Root path!
    });
}
```

### F) Startup Message
```csharp
var env = app.Environment.EnvironmentName;
var startupMsg = $@"
╔════════════════════════════════════╗
║        API Started Successfully    ║
╚════════════════════════════════════╝

🌍 Environment:  {env}
🔌 Port:         {port}
📄 Swagger:      http://localhost:{port}/
❤️  Health:       http://localhost:{port}/health

════════════════════════════════════
";
Console.WriteLine(startupMsg);
```

## Step 3: Run and Test (1 min)

```bash
dotnet run
```

### Expected Output
```
╔════════════════════════════════════╗
║        API Started Successfully    ║
╚════════════════════════════════════╝

🌍 Environment:  Development
🔌 Port:         5000
📄 Swagger:      http://localhost:5000/
❤️  Health:       http://localhost:5000/health

════════════════════════════════════
```

### Test in Browser
1. **Swagger**: http://localhost:5000/
   - See all endpoints
   - Try them out interactively
   - See request/response examples

2. **Health Check**: http://localhost:5000/health
   ```json
   {
     "status": "Healthy",
     "checks": {
       "self": {
         "status": "Healthy"
       }
     }
   }
   ```

## Step 4: Configure Port (Optional)

### Option 1: Environment Variable (Recommended)
```bash
# Linux/Mac
export PORT=8080
dotnet run

# Windows (CMD)
set PORT=8080
dotnet run

# Windows (PowerShell)
$env:PORT=8080
dotnet run
```

### Option 2: appsettings.json
```json
{
  "PORT": 8080
}
```

## Done! ✅

Your API now has:
- ✅ Swagger UI at root path (no white page!)
- ✅ Health check endpoint at `/health`
- ✅ Clean startup message showing URLs
- ✅ Configurable port
- ✅ Development/Production separation

## Common Tasks

### Add Database Health Check
```csharp
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
```

### Hide Swagger in Production
```csharp
// This is already in the template!
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(...);
}
```

### Add Custom Health Check
```csharp
public class CustomHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // Add your check here
        return HealthCheckResult.Healthy("All good!");
    }
}

// Register:
builder.Services.AddHealthChecks()
    .AddCheck<CustomHealthCheck>("custom");
```

### Multiple Swagger Versions
```csharp
// In AddSwaggerGen:
options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
options.SwaggerDoc("v2", new OpenApiInfo { Title = "My API", Version = "v2" });

// In UseSwaggerUI:
options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
options.SwaggerEndpoint("/swagger/v2/swagger.json", "API v2");
```

## Troubleshooting

**Q: White page appears?**
A: Add `options.RoutePrefix = string.Empty;` to SwaggerUI config

**Q: Port already in use?**
A: `export PORT=8081 && dotnet run`

**Q: Health check unhealthy?**
A: Check database connection string in appsettings.json

**Q: Swagger not showing in production?**
A: Good! It's wrapped in `if (app.Environment.IsDevelopment())`

## Next Steps

1. **Add XML Documentation Comments** to your controllers
   ```csharp
   /// <summary>Get user by ID</summary>
   /// <param name="id">User ID</param>
   public async Task<UserDto> GetUser(int id)
   ```

2. **Add Security Definitions** if using JWT
   ```csharp
   options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
   {
       Type = SecuritySchemeType.Http,
       Scheme = "bearer"
   });
   ```

3. **Read Full Guide**: `/docs/middleware/swagger-health-checks.md`

4. **Use Checklist**: `/checklists/swagger-health-check-setup.md`

---

**⏱️ Total Time: ~5 minutes**
**📝 Lines Added: ~50**
**🚀 Impact: Beautiful, self-documenting API!**
