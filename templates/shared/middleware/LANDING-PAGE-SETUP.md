# Landing Page Middleware Setup Guide

This guide shows how to add a professional landing page to your API root path (`/`) that displays health status and navigation links.

---

## What It Does

When users visit `http://localhost:5000/`:

### In Development 🔧
```
┌─────────────────────────────────────┐
│  🚀 API Dashboard                   │
│                                     │
│  🔧 DEVELOPMENT MODE                │
│                                     │
│  Health Status: Operational ✓ OK    │
│                                     │
│  [📖 API Documentation (Swagger)]   │
│  [💚 Health Check Endpoint]         │
└─────────────────────────────────────┘

Provides:
✅ Swagger link (for testing endpoints)
✅ Health check link
✅ Development indicator
```

### In Production 🚀
```
┌─────────────────────────────────────┐
│  🚀 API Dashboard                   │
│                                     │
│  Health Status: Operational ✓ OK    │
│                                     │
│  [💚 Health Check Status]           │
└─────────────────────────────────────┘

Provides:
✅ Health status only
❌ No Swagger (hidden in production)
```

---

## Installation Steps

### Step 1: Add the Middleware File

Copy `landing-page.template.cs` to your project:

```bash
cp landing-page.template.cs \
   src/YourApi.Presentation/Middleware/LandingPageMiddleware.cs
```

Update the namespace:
```csharp
namespace YourApi.Presentation.Middleware;  // Change to your namespace
```

### Step 2: Register in Program.cs

Add to your `Program.cs`:

```csharp
var app = builder.Build();

// Add Landing Page Middleware (BEFORE other middleware)
app.UseMiddleware<LandingPageMiddleware>();

// Health checks
app.MapHealthChecks("/health");

// Swagger setup
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "API Documentation";
    });
}

// Your other middleware and routes
app.MapControllers();

app.Run();
```

**Important**: Register `LandingPageMiddleware` **FIRST** before other middleware!

### Step 3: Add Missing Health Check (if not already present)

In `Program.cs`, add health checks:

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"));

var app = builder.Build();

// Map health endpoint
app.MapHealthChecks("/health");

// ... rest of setup
```

---

## Complete Program.cs Example

```csharp
using YourApi.Presentation.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API is running"));

var app = builder.Build();

// Order matters! Landing page middleware FIRST
app.UseMiddleware<LandingPageMiddleware>();

// Map endpoints
app.MapHealthChecks("/health");

// Swagger only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "API Documentation";
    });
}

app.MapControllers();

app.Run();
```

---

## Features

### ✨ Visual Design
- Professional gradient background
- Clean white card layout
- Responsive design (works on mobile)
- Smooth hover effects

### 🔍 Status Information
- Health status badge (green ✓ OK)
- Environment indicator (🔧 DEVELOPMENT MODE)
- Available endpoints list

### 🔗 Smart Navigation
- **Development**: Shows Swagger link + Health check link
- **Production**: Shows only Health check link
- Direct clickable buttons to common endpoints

### 📱 Responsive
- Works on desktop, tablet, mobile
- Adapts to screen size
- Touch-friendly buttons

---

## Customization

### Change Colors

Edit the `style` section:

```csharp
// Primary color (blue)
background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);

// Change to your brand colors
background: linear-gradient(135deg, #FF6B6B 0%, #FE5196 100%);
```

### Change Title

```csharp
<h1>API Dashboard</h1>
// Change to
<h1>My Company API</h1>
```

### Add Custom Logo

Replace the emoji:
```csharp
<div class="logo">🚀</div>
// Change to
<div class="logo"><img src="/logo.png" alt="Logo" width="32"></div>
```

### Add Version Info

```csharp
<p class="subtitle">Welcome to your API</p>
// Add
<p class="subtitle">Version 1.0.0 • API Ready</p>
```

---

## Behavior by Environment

### Development Environment
- Middleware shows full UI with Swagger link
- Environment badge displays: 🔧 DEVELOPMENT MODE
- Includes endpoints for documentation

### Production Environment  
- Middleware shows minimal UI (health only)
- No environment badge
- No Swagger link (security)
- Swagger endpoint not registered

---

## Testing

### Start your API

```bash
dotnet run
```

### Test the landing page

```bash
# Should show HTML dashboard
curl http://localhost:5000/

# Check health
curl http://localhost:5000/health

# Access Swagger (dev only)
curl http://localhost:5000/swagger
```

### In Browser

Visit: `http://localhost:5000/`

Should see:
- Professional dashboard
- Health status (green)
- Clickable links to Swagger/Health

---

## Middleware Order Matters

Middleware processes requests in order. `LandingPageMiddleware` must be early:

```csharp
// ✅ CORRECT ORDER
app.UseMiddleware<LandingPageMiddleware>();   // 1. Landing page first
app.UseSwagger();                              // 2. Swagger
app.UseSwaggerUI();                            // 3. Swagger UI
app.UseAuthorization();                        // 4. Auth
app.MapControllers();                          // 5. Routes
```

If Swagger is registered first, it might intercept the root path request.

---

## Security Considerations

### Development
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseMiddleware<LandingPageMiddleware>();  // Show UI
    app.UseSwagger();                              // Show API docs
}
```

### Production
```csharp
else
{
    app.UseMiddleware<LandingPageMiddleware>();  // Minimal info only
    // NO Swagger, NO API docs
}
```

The middleware automatically:
- ✅ Shows health status in all environments
- ✅ Shows Swagger link only in Development
- ✅ Hides sensitive endpoints in Production

---

## Troubleshooting

### Landing page not showing?

**Problem**: Blank page or 404  
**Solution**: 
1. Check middleware is registered in `Program.cs`
2. Verify it's registered BEFORE other middleware
3. Restart the application

```csharp
// Should be near the top
app.UseMiddleware<LandingPageMiddleware>();
```

### Swagger link not working?

**Problem**: Click Swagger link, get 404  
**Solution**:
1. Make sure Swagger is configured in `Program.cs`
2. Check route is `/swagger` not `/swagger/`
3. Verify `UseSwaggerUI(options => options.RoutePrefix = "swagger")`

### Health endpoint not working?

**Problem**: `/health` returns 404  
**Solution**:
1. Add health checks to services:
   ```csharp
   builder.Services.AddHealthChecks()
   ```
2. Map health checks endpoint:
   ```csharp
   app.MapHealthChecks("/health");
   ```

### Wrong environment being detected?

**Problem**: Shows dev mode in production  
**Solution**:
1. Check `ASPNETCORE_ENVIRONMENT` variable
2. Set it correctly:
   ```bash
   export ASPNETCORE_ENVIRONMENT=Production
   dotnet run
   ```

---

## Performance Impact

- Minimal: Just HTML generation on root requests
- No database calls
- No external dependencies
- Fast: < 1ms response time

---

## HTML Source Rendering

The middleware generates clean HTML with:
- ✅ Inline CSS (no external stylesheets needed)
- ✅ No JavaScript (faster, more secure)
- ✅ Self-contained (no dependencies)
- ✅ SEO-friendly headers
- ✅ Mobile-responsive viewport meta tag

---

## Next Steps

1. Copy `landing-page.template.cs` to your project
2. Register in `Program.cs`
3. Add health checks endpoint
4. Run and visit `/`
5. Customize colors/content as needed

**Result**: Professional-looking API landing page! 🎉
