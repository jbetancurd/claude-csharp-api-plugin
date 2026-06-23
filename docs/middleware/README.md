# Middleware & Configuration Guides

This directory contains comprehensive guides for ASP.NET Core middleware, configuration, and startup setup.

## Swagger UI & Health Checks

### Quick Start (5 Minutes)
**File**: [QUICK-SWAGGER-SETUP.md](../QUICK-SWAGGER-SETUP.md)

Get Swagger UI and health checks running immediately with minimal setup. Perfect for getting started fast.

- ✅ 5-minute setup guide
- ✅ Copy-paste code snippets
- ✅ Port configuration
- ✅ Testing instructions
- ✅ Troubleshooting tips

### Complete Guide
**File**: [swagger-health-checks.md](swagger-health-checks.md)

Comprehensive reference covering all aspects of Swagger UI and health checks configuration.

**Topics Covered**:
- Setup and installation
- Program.cs configuration (detailed)
- Health check endpoints
- Development vs production security
- Custom health checks
- Multiple API versions
- Health check monitoring (Kubernetes, load balancers)
- Security considerations
- Troubleshooting

### Production-Ready Template
**File**: [../templates/shared/middleware/program-swagger-health.template.cs](../../templates/shared/middleware/program-swagger-health.template.cs)

Complete Program.cs template with:
- Swagger UI configuration
- Health checks setup
- Port configuration with environment variables
- CORS setup
- Startup message
- JWT security definitions
- Production-ready error handling

### Setup Checklist
**File**: [../checklists/swagger-health-check-setup.md](../../checklists/swagger-health-check-setup.md)

11-step verification checklist to ensure proper Swagger/health check configuration:

1. Install packages
2. Configure Program.cs
3. Configure middleware pipeline
4. Setup startup message
5. Configure appsettings files
6. Test startup
7. Verify Swagger UI
8. Verify health check
9. Docker deployment
10. Production hardening
11. Troubleshooting

---

## Docker & Kubernetes Deployment

### Docker Setup Guide
**File**: [docker-swagger-setup.md](docker-swagger-setup.md)

Complete guide for containerizing your Swagger-enabled API with Docker and Docker Compose.

**Topics Covered**:
- Basic Dockerfile with multi-stage builds
- Docker Compose for local development
- Multiple environments (development, production)
- Port configuration in Docker
- Health checks in Docker
- Environment variables and .env files
- SSL/HTTPS configuration
- Networking for microservices
- Kubernetes setup with liveness/readiness probes
- Troubleshooting Docker issues

**Included**:
- Production-ready Dockerfile
- docker-compose.yml for development
- docker-compose.override.yml examples
- Kubernetes deployment manifest
- Health check examples for Docker and K8s

---

## Configuration Patterns

### Port Configuration
Three levels of configuration (in order of precedence):
1. **Environment Variable** (recommended for Docker/cloud)
2. **appsettings.json** (configuration file)
3. **Code Default** (fallback to 5000)

### Environment-Specific Settings
- **appsettings.json** - Base configuration
- **appsettings.Development.json** - Dev overrides
- **appsettings.Production.json** - Production overrides

Example:
```json
{
  "PORT": 5000,
  "Logging": { "LogLevel": { "Default": "Information" } }
}
```

### Startup Message
Clear console output showing:
- Environment name
- Listening port
- Swagger URL (development only)
- Health check URL

Example:
```
╔════════════════════════════════════╗
║        API Started Successfully    ║
╚════════════════════════════════════╝

🌍 Environment:  Development
🔌 Port:         5000
🌐 Base URL:     http://localhost:5000
📄 Swagger:      http://localhost:5000/
❤️  Health:       http://localhost:5000/health

════════════════════════════════════
```

---

## Integration with Decision Tree

These middleware guides are referenced in the architecture decision tree:

- **Step 11**: Swagger UI & Health Checks Configuration
  - Decision point for Swagger setup
  - Port configuration options
  - Development vs production

- **Step 12**: Your Complete Path
  - Step 8 includes Swagger setup
  - Recommends this documentation
  - Links to templates and checklists

**Reference**: [../decision-tree.md#step-11](../decision-tree.md#step-11-swagger-ui--health-checks-configuration)

---

## Health Check Types

### Self Check
Verify the API itself is running:
```csharp
.AddCheck("self", () => HealthCheckResult.Healthy("API is running"))
```

### Database Check
Verify database connectivity:
```csharp
.AddSqlServer(connectionString, name: "database")
```

### Custom Check
Implement IHealthCheck for custom logic:
```csharp
public class CustomHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(...) { ... }
}
```

### Health Check Response
```json
{
  "status": "Healthy",
  "checks": {
    "self": { "status": "Healthy" },
    "database": { "status": "Healthy" }
  },
  "totalDuration": "00:00:00.1234567"
}
```

---

## Swagger Configurations

### Basic Setup
Swagger UI as default landing page:
```csharp
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = string.Empty;  // Root path
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
});
```

### Development Only
Hide Swagger in production:
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(...);
}
```

### Multiple Versions
Support multiple API versions:
```csharp
options.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
options.SwaggerDoc("v2", new OpenApiInfo { Title = "API", Version = "v2" });

// In UI:
options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
options.SwaggerEndpoint("/swagger/v2/swagger.json", "API v2");
```

### JWT Authentication
Add security scheme to Swagger:
```csharp
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT"
});
```

---

## Troubleshooting Guide

### Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| White page on startup | Swagger not default | Add `RoutePrefix = ""` |
| Health check unhealthy | DB connection wrong | Check connection string |
| Port already in use | Port taken by other process | Set `PORT=8080` env var |
| Swagger not in prod | Wrapped in `IsDevelopment()` | This is correct behavior |
| Docker health check fails | Container not ready | Increase `start_period` |
| K8s pod not ready | Readiness probe timing | Check probe configuration |

---

## Best Practices

### Security
- ✅ Disable Swagger in production
- ✅ Health check always available (for monitoring)
- ✅ No sensitive data in health responses
- ✅ Use HTTPS in production

### Performance
- ✅ Health checks respond quickly (< 1 second)
- ✅ Cache health check results if needed
- ✅ Use appropriate probe intervals (30s recommended)

### Monitoring
- ✅ Export health check metrics
- ✅ Alert on unhealthy status
- ✅ Track health check response times
- ✅ Use liveness + readiness probes in K8s

### Configuration
- ✅ Use environment variables for deployment
- ✅ Environment-specific appsettings files
- ✅ Clear startup logging
- ✅ Document port requirements

---

## File Structure

```
docs/middleware/
├── README.md                              ← You are here
├── swagger-health-checks.md               ← Complete guide
├── docker-swagger-setup.md                ← Docker/K8s guide
└── ../QUICK-SWAGGER-SETUP.md             ← 5-minute quickstart

templates/shared/middleware/
├── program-swagger-health.template.cs     ← Template Program.cs
└── global-exception-filter.template.cs    ← Error handling

checklists/
└── swagger-health-check-setup.md          ← Setup verification
```

---

## Related Documentation

- [Architecture Decision Tree](../decision-tree.md) - Step 11 covers Swagger setup
- [SOLID Principles](../architecture/solid-principles.md) - Applied to middleware
- [Onion Architecture](../architecture/onion-architecture.md) - Middleware in Presentation layer
- [Error Handling](../error-handling/aop-vs-filters.md) - Exception filter examples

---

## Quick Links

### Learn Swagger UI in 5 Minutes
→ [QUICK-SWAGGER-SETUP.md](../QUICK-SWAGGER-SETUP.md)

### Setup Complete Production API
→ [program-swagger-health.template.cs](../../templates/shared/middleware/program-swagger-health.template.cs)

### Deploy to Docker/Kubernetes
→ [docker-swagger-setup.md](docker-swagger-setup.md)

### Verify Your Setup
→ [swagger-health-check-setup.md](../../checklists/swagger-health-check-setup.md)

### Full Technical Reference
→ [swagger-health-checks.md](swagger-health-checks.md)

---

**Last Updated**: June 2026
**Covers**: .NET 8.0+, Docker, Kubernetes, Swagger 6.0+
