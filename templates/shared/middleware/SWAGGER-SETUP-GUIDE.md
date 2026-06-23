# Swagger UI & Health Check Setup - Complete Guide

Fix the three issues: Swagger redirect, default HTML page, and health check endpoint.

## Issues Fixed

✅ Swagger UI now properly redirects to root path in development  
✅ Default HTML page shows health check dashboard in production  
✅ Health check endpoint properly mapped at `/health`  

---

## Setup Instructions

### Step 1: Update Program.cs

Copy the corrected template:
```bash
# From:
templates/shared/middleware/program-swagger-health.template.cs

# To:
src/YourApi.Presentation/Program.cs
```

**Key fixes in the new template:**

1. **Correct middleware order** (ORDER MATTERS!):
   ```
   1. HTTPS Redirect
   2. CORS
   3. Static Files
   4. Swagger UI (development) OR Default Files (production)
   5. Authorization
   6. Health Checks (MapHealthChecks BEFORE controllers!)
   7. Controllers
   ```

2. **Development mode** - Swagger UI at root:
   ```csharp
   if (app.Environment.IsDevelopment())
   {
       app.UseSwagger();
       app.UseSwaggerUI(options =>
       {
           options.RoutePrefix = string.Empty;  // ROOT PATH!
       });
   }
   ```

3. **Production mode** - Serve default.html:
   ```csharp
   else
   {
       app.UseDefaultFiles(new DefaultFilesOptions
       {
           DefaultFileNames = new List<string> { "default.html" }
       });
       app.UseStaticFiles();
   }
   ```

4. **Health check endpoint BEFORE controllers**:
   ```csharp
   app.MapHealthChecks("/health", new HealthCheckOptions
   {
       ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
   });
   ```

### Step 2: Create wwwroot Folder

The `default.html` file must be in the **wwwroot** folder (web root).

```bash
# Create if it doesn't exist
mkdir -p src/YourApi.Presentation/wwwroot

# Copy the default HTML template
cp templates/shared/middleware/default.html \
   src/YourApi.Presentation/wwwroot/default.html
```

### Step 3: Update Project File (if needed)

If static files don't work, ensure your `.csproj` file includes:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsingsEnabled>true</ImplicitUsingsEnabled>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- IMPORTANT: Static files need wwwroot -->
  <ItemGroup>
    <Content Update="wwwroot\**">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

</Project>
```

### Step 4: Verify appsettings.json

Ensure you have the database connection string (or comment out if not using SQL):

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

### Step 5: Build and Run

```bash
# Build
dotnet build

# Run in Development (Swagger at root)
dotnet run --project src/YourApi.Presentation

# Visit: http://localhost:5000/
# Expected: Swagger UI loads at root
# Expected: /health endpoint works
```

---

## Development Mode Behavior

### http://localhost:5000/
```
Swagger UI loads automatically
- All endpoints visible
- Try-it-out feature works
- Can send test requests
```

### http://localhost:5000/health
```json
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
  "totalDuration": "00:00:00.0123456"
}
```

### Startup Message
```
╔════════════════════════════════════╗
║        API Started Successfully    ║
╚════════════════════════════════════╝

🌍 Environment:  Development
🔌 Port:         5000
🌐 Base URL:     http://localhost:5000
📄 Swagger UI:    http://localhost:5000/ (root landing page)
📊 API Docs:     http://localhost:5000/swagger/v1/swagger.json
❤️  Health Check:  http://localhost:5000/health

════════════════════════════════════
```

---

## Production Mode Behavior

### http://localhost:5000/
```
default.html loads with health check dashboard
- Shows API status
- Click "Check Health" to test endpoint
- Links to API endpoints
- Professional looking dashboard
```

### Folder Structure
```
src/YourApi.Presentation/
├── Program.cs
├── appsettings.json
├── appsettings.Production.json
└── wwwroot/
    └── default.html          ← Served at root in production
```

### Startup Message (Production)
```
╔════════════════════════════════════╗
║        API Started Successfully    ║
╚════════════════════════════════════╝

🌍 Environment:  Production
🔌 Port:         80
🌐 Base URL:     http://example.com
🏠 Home Page:     http://example.com/ (default.html)
❤️  Health Check:  http://example.com/health

════════════════════════════════════
```

---

## Troubleshooting

### Issue: Swagger shows blank page / not loading at root

**Cause**: RoutePrefix not set correctly

**Solution**:
```csharp
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
    options.RoutePrefix = string.Empty;  // MUST be empty string!
});
```

### Issue: 404 error on /health endpoint

**Cause**: Health checks mapped AFTER controllers (middleware order)

**Solution**:
```csharp
// WRONG - after controllers
app.MapControllers();
app.MapHealthChecks("/health", ...);

// CORRECT - before controllers
app.MapHealthChecks("/health", ...);
app.MapControllers();
```

### Issue: default.html not found in production

**Cause**: wwwroot folder missing or UseStaticFiles() not enabled

**Solution**:
```bash
# 1. Create wwwroot folder
mkdir -p src/YourApi.Presentation/wwwroot

# 2. Copy default.html there
cp default.html src/YourApi.Presentation/wwwroot/

# 3. Ensure UseDefaultFiles & UseStaticFiles are enabled
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "default.html" }
});
app.UseStaticFiles();
```

### Issue: Health check returns 500 error

**Cause**: Database connection string incorrect or database unavailable

**Solution**:
```csharp
// Option 1: Fix connection string
// Check: appsettings.json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=MyDb;Trusted_Connection=true;"
}

// Option 2: Comment out database check if not needed
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"));
    // .AddSqlServer(...) ← Remove or comment out
```

---

## Middleware Order Explained

The **order matters** in ASP.NET Core middleware:

```csharp
var app = builder.Build();

// 1. HTTPS Redirect - Convert HTTP to HTTPS
app.UseHttpsRedirection();

// 2. CORS - Allow cross-origin requests
app.UseCors("AllowAll");

// 3. Static Files - Serve wwwroot files (CSS, JS, images)
app.UseStaticFiles();

// 4. Swagger UI - Only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => { ... });
}

// 5. Authorization - Check permissions
app.UseAuthorization();

// 6. Health Checks - Endpoint for monitoring
app.MapHealthChecks("/health", ...);

// 7. Controllers - Your API endpoints
app.MapControllers();

// 8. Fallback - Catch-all for unmapped routes
app.MapFallback(...);
```

**Why order matters**:
- Middleware pipeline is sequential
- First match wins (stops processing)
- Must map endpoints BEFORE they can be requested
- Static files must be before routing

---

## File Locations Summary

```
Project Root/
├── src/
│   └── YourApi.Presentation/
│       ├── Program.cs                          ← Updated template
│       ├── appsettings.json                    ← Check connection string
│       ├── appsettings.Development.json
│       ├── appsettings.Production.json
│       ├── YourApi.Presentation.csproj
│       └── wwwroot/                            ← NEW FOLDER!
│           └── default.html                    ← Copy here from templates
│
└── templates/
    └── shared/
        └── middleware/
            ├── program-swagger-health.template.cs   ← Copy to Program.cs
            └── default.html                         ← Copy to wwwroot/
```

---

## Testing Checklist

### Development Mode
- [ ] Run: `dotnet run --project src/YourApi.Presentation`
- [ ] Visit: http://localhost:5000/
- [ ] ✅ Swagger UI loads (not blank page)
- [ ] ✅ All endpoints visible in Swagger
- [ ] ✅ Can click "Try it out" on endpoints
- [ ] ✅ http://localhost:5000/health returns JSON
- [ ] ✅ Console shows startup message with URLs

### Production Mode
```bash
# Build production
dotnet publish -c Release

# Run published version
cd bin/Release/net8.0/publish
dotnet YourApi.Presentation.dll --urls http://localhost:5000
```

- [ ] ✅ http://localhost:5000/ shows default.html dashboard
- [ ] ✅ Health check button works and shows status
- [ ] ✅ http://localhost:5000/health returns JSON
- [ ] ✅ Swagger UI is NOT visible (correct for production)

---

## Docker Support

If using Docker, ensure `default.html` is copied to image:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app
COPY --from=publish /app/publish .

# Ensure wwwroot is included
COPY --from=publish /app/wwwroot ./wwwroot

ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=80

EXPOSE 80
ENTRYPOINT ["dotnet", "YourApi.Presentation.dll"]
```

---

## Summary of Changes

| Issue | Fix | Location |
|-------|-----|----------|
| **Swagger not at root** | Set `RoutePrefix = string.Empty` | Program.cs |
| **Blank page on startup** | Add default.html serving | Program.cs + wwwroot |
| **Health check 404** | Map before controllers | Program.cs middleware order |
| **Database errors** | Check connection string | appsettings.json |

---

## Next Steps

1. ✅ Update Program.cs from template
2. ✅ Create wwwroot folder
3. ✅ Copy default.html to wwwroot/
4. ✅ Update appsettings.json (if needed)
5. ✅ Test development mode: `dotnet run`
6. ✅ Test endpoints: http://localhost:5000/, /health
7. ✅ Verify Swagger UI loads at root
8. ✅ Test production build: `dotnet publish -c Release`

---

**All three issues fixed!** 🎉

- ✅ Swagger UI redirects to root
- ✅ Default HTML page shows health check
- ✅ Health check endpoint properly mapped
