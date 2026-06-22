# Serilog Structured Logging Strategy

Serilog provides structured, semantic logging with flexible output (files, cloud, databases, etc.).

## Why Serilog?

✅ **Structured logging** - JSON format, searchable properties
✅ **Multiple sinks** - Write to files, console, cloud (Azure, Seq, etc.)
✅ **Enrichment** - Add context (user, request ID, environment)
✅ **Formatting** - Beautiful console output, JSON for production
✅ **Async by default** - Non-blocking I/O
✅ **Expression-based filters** - Log what you need, filter what you don't

## Installation

```bash
dotnet add package Serilog
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Context
```

## Basic Setup

### 1. Configure in Program.cs

```csharp
using Serilog;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/app-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .CreateLogger();

builder.Host.UseSerilog();

var app = builder.Build();

// Example: Log startup
Log.Information("Application starting, Environment: {Environment}", 
    builder.Environment.EnvironmentName);

app.Run();
```

## Advanced Setup with appsettings.json

### appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "YourApp.Application": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 1073741824,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/errors-.txt",
          "rollingInterval": "Day",
          "restrictedToEventType": "Error",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}Properties: {@Properties}"
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithEnvironmentName",
      "WithMachineName",
      "WithThreadId"
    ]
  }
}
```

### Program.cs (Using appsettings)

```csharp
using Serilog;

var builder = WebApplicationBuilder.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

Log.Information("Application started, Environment: {Environment}", 
    app.Environment.EnvironmentName);

app.Run();
```

## Structured Logging Patterns

### 1. Basic Logging

```csharp
// Bad - Concatenation
_logger.LogInformation($"Order {orderId} created by user {userId}");

// Good - Structured properties
_logger.LogInformation("Order created", new { OrderId = orderId, UserId = userId });

// Better - Named properties (searchable in Seq/Kibana)
_logger.LogInformation("Order {OrderId} created by {UserId}", orderId, userId);
```

### 2. Logging with Context

```csharp
public class OrderApplicationService
{
    private readonly ILogger<OrderApplicationService> _logger;

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
    {
        // Create correlation ID for tracking request
        var correlationId = Guid.NewGuid().ToString();
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", GetCurrentUserId()))
        {
            _logger.LogInformation("Creating order for customer {CustomerId}", dto.CustomerId);
            
            try
            {
                var order = new Order(dto.CustomerId);
                // ... business logic ...
                
                _logger.LogInformation("Order {OrderId} created successfully", order.Id);
                return MapToDto(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order for customer {CustomerId}", 
                    dto.CustomerId);
                throw;
            }
        }
    }
}
```

### 3. Performance Logging

```csharp
var stopwatch = Stopwatch.StartNew();

try
{
    await _orderRepository.AddAsync(order);
    stopwatch.Stop();
    
    _logger.LogInformation(
        "Order {OrderId} persisted in {ElapsedMs}ms",
        order.Id, stopwatch.ElapsedMilliseconds);
}
catch (Exception ex)
{
    stopwatch.Stop();
    _logger.LogError(ex, "Failed to persist order in {ElapsedMs}ms", 
        stopwatch.ElapsedMilliseconds);
    throw;
}
```

### 4. Audit Logging

```csharp
public interface IAuditLogger
{
    void LogAction(string action, object entity, object userId, object details);
}

public class AuditLogger : IAuditLogger
{
    private readonly ILogger<AuditLogger> _logger;

    public void LogAction(string action, object entity, object userId, object details)
    {
        _logger.LogInformation(
            "Audit: {Action} on {EntityType} by {UserId}, Details: {@Details}",
            action,
            entity.GetType().Name,
            userId,
            details);
    }
}

// Usage in service
public async Task<OrderDto> ApproveOrderAsync(int id)
{
    var order = await _orderRepository.GetByIdAsync(id);
    order.Approve();
    await _orderRepository.UpdateAsync(order);
    
    // Audit log
    _auditLogger.LogAction(
        "Approve",
        order,
        GetCurrentUserId(),
        new { Timestamp = DateTime.UtcNow, ApprovedBy = GetCurrentUserName() });
    
    return MapToDto(order);
}
```

## Serilog with External Sinks (Connectors)

### Azure Application Insights

```bash
dotnet add package Serilog.Sinks.ApplicationInsights
```

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.ApplicationInsights(
        instrumentationKey: builder.Configuration["ApplicationInsights:InstrumentationKey"],
        telemetryConverter: TelemetryConverter.Traces)
    .CreateLogger();
```

### Seq (Structured Event Query)

```bash
dotnet add package Serilog.Sinks.Seq
```

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Seq",
        "Args": { "serverUrl": "http://localhost:5341" }
      }
    ]
  }
}
```

Perfect for local development and testing.

### Datadog

```bash
dotnet add package Serilog.Sinks.Datadog.Logs
```

```csharp
var logger = new LoggerConfiguration()
    .WriteTo.DatadogLogs(
        apiKey: builder.Configuration["Datadog:ApiKey"],
        configuration: new DatadogConfiguration { Source = "csharp" })
    .CreateLogger();
```

### Splunk

```bash
dotnet add package Serilog.Sinks.SplunkHec
```

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.SplunkHec(
        splunkHost: "your-splunk-instance.com",
        eventCollectorToken: "your-token")
    .CreateLogger();
```

## Log Levels Strategy

```csharp
// TRACE - Detailed debugging (rarely used)
_logger.LogTrace("Entering method {MethodName} with parameters {@Parameters}", 
    nameof(GetOrder), new { orderId = 1 });

// DEBUG - Development/debugging info
_logger.LogDebug("Customer {CustomerId} discount calculated: {Discount}%", 
    customerId, discountPercentage);

// INFORMATION - Normal flow, important events
_logger.LogInformation("Order {OrderId} created by {UserId}", orderId, userId);

// WARNING - Unexpected but handled (retries, fallbacks)
_logger.LogWarning("Payment API retry {RetryCount}/{MaxRetries} for order {OrderId}", 
    retryCount, maxRetries, orderId);

// ERROR - Errors that should be investigated
_logger.LogError(ex, "Failed to process payment for order {OrderId}", orderId);

// FATAL/CRITICAL - System is shutting down
_logger.LogCritical("Database connection lost, application halting");
```

## Filtering Configuration

### By Namespace

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "YourApp.Domain": "Debug"
      }
    }
  }
}
```

### By Event Type

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/errors-.txt",
          "restrictedToEventType": "Error"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/all-.txt"
        }
      }
    ]
  }
}
```

## Enrichment

Add contextual information automatically:

```csharp
new LoggerConfiguration()
    .Enrich.FromLogContext()           // Add context in code
    .Enrich.WithEnvironmentName()      // Development/Production
    .Enrich.WithMachineName()          // Server name
    .Enrich.WithThreadId()             // Thread info
    .Enrich.With(new HttpContextEnricher())  // Custom enricher
    .CreateLogger();
```

### Custom Enricher

```csharp
public class HttpContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext == null) return;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
            "RequestId", httpContext.TraceIdentifier));

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
            "UserId", httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous"));

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
            "Path", httpContext.Request.Path.Value));
    }
}

// Register in DI
builder.Services.AddScoped<HttpContextEnricher>();
builder.Host.UseSerilog((context, services, config) =>
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.With(services.GetRequiredService<HttpContextEnricher>()));
```

## Request/Response Logging Middleware

```csharp
public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, 
        ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = Guid.NewGuid().ToString();
        using (LogContext.PushProperty("RequestId", requestId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "Incoming request {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            try
            {
                await _next(context);
                stopwatch.Stop();

                _logger.LogInformation(
                    "Request completed with status {StatusCode} in {ElapsedMs}ms",
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Request failed with exception after {ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}

// Register in Program.cs
app.UseMiddleware<RequestResponseLoggingMiddleware>();
```

## Sensitive Data Masking

```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.ToMaximumDepth(4)
    .Destructure.With<SensitiveDataDestructuringPolicy>()
    .CreateLogger();

public class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory, 
        out LogEventPropertyValue result)
    {
        if (value is CreateOrderDto dto)
        {
            var masked = new
            {
                CustomerId = dto.CustomerId,
                Email = MaskEmail(dto.Email),
                CreditCard = "****-****-****-" + dto.CreditCard?.Substring(dto.CreditCard.Length - 4)
            };

            result = factory.CreatePropertyValue(masked);
            return true;
        }

        result = null;
        return false;
    }

    private string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return email;
        var parts = email.Split('@');
        return $"{parts[0][0]}***@{parts[1]}";
    }
}
```

## Best Practices

✅ **Do:**
- Log at service boundaries (inputs/outputs)
- Use structured properties (not string interpolation)
- Log errors with full exception
- Use correlation IDs for request tracking
- Log performance metrics for critical operations
- Mask sensitive data (passwords, tokens, credit cards)
- Use appropriate log levels
- Configure different outputs for different environments

❌ **Don't:**
- Log in tight loops (performance killer)
- Log sensitive data unmasked
- Use string concatenation in log messages
- Log every method entry/exit (use AOP if needed)
- Write to console in production (use files/cloud)
- Ignore exceptions in log filters (causes data loss)
- Use synchronous logging in high-traffic paths

---

**Next**: See [AOP Error Handling](../error-handling/aop-error-handling.md) for combining Serilog with error handling decorators.
