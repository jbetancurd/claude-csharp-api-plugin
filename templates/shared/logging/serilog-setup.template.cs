// Template: Complete Serilog Setup
// Copy to Program.cs and customize for your environment

using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using System;
using System.Diagnostics;
using System.IO;

namespace YourApp.API
{
    /// <summary>
    /// Serilog Configuration Template
    ///
    /// Features:
    /// - File logging with daily rollover
    /// - Separate error log file
    /// - Console output (colored in development)
    /// - Structured properties (JSON)
    /// - Enrichment (environment, machine, thread)
    /// - Request/response logging
    /// - Performance tracking
    /// </summary>
    public static class SerilogConfiguration
    {
        /// <summary>
        /// Configure Serilog for the application
        /// Call in Program.cs: builder.Host.UseSerilog(ConfigureLogger);
        /// </summary>
        public static void ConfigureLogger(
            HostBuilderContext context,
            LoggerConfiguration loggerConfig)
        {
            var environment = context.HostingEnvironment.EnvironmentName;
            var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");

            // Ensure logs directory exists
            Directory.CreateDirectory(logsDirectory);

            loggerConfig
                // Minimum level
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)

                // Console output (development)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")

                // File output - all logs (rolling daily)
                .WriteTo.File(
                    path: Path.Combine(logsDirectory, "app-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 1073741824, // 1GB
                    rollOnFileSizeLimit: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")

                // File output - errors only
                .WriteTo.File(
                    path: Path.Combine(logsDirectory, "errors-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 1073741824,
                    rollOnFileSizeLimit: true,
                    restrictedToMinimumLevel: LogEventLevel.Error,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}{NewLine}Properties: {@Properties}")

                // JSON output for structured logging (production)
                .WriteTo.File(
                    formatter: new JsonFormatter(),
                    path: Path.Combine(logsDirectory, "app-json-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 1073741824,
                    rollOnFileSizeLimit: true)

                // Enrichment
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", "YourAppName")
                .Enrich.WithProperty("Environment", environment)

                // Custom enricher for HTTP context
                .Enrich.When(
                    le => LogContext.PeekProperty("RequestId") != null,
                    e => e.WithProperty("RequestId", LogContext.PeekProperty("RequestId")))

                // Destructuring (make objects readable in logs)
                .Destructure.ToMaximumDepth(4)
                .Destructure.ToMaximumStringLength(100)
                .Destructure.ToMaximumCollectionCount(10);
        }

        /// <summary>
        /// Register Serilog in DI container
        /// Call in Program.cs:
        /// builder.Services.AddSerilogServices();
        /// </summary>
        public static IServiceCollection AddSerilogServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<HttpContextEnricher>();
            services.AddScoped<RequestResponseLoggingMiddleware>();
            return services;
        }

        /// <summary>
        /// Add Serilog to host builder
        /// Call in Program.cs:
        /// builder.Host.UseSerilog(ConfigureLogger);
        /// </summary>
        public static IHostBuilder UseSerilogConfigured(this IHostBuilder builder)
        {
            return builder.UseSerilog(ConfigureLogger);
        }
    }

    // ============ HTTP CONTEXT ENRICHER ============

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

            // Request ID (for tracing)
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "RequestId", httpContext.TraceIdentifier));

            // User ID
            var userId = httpContext.User?.FindFirst("sub")?.Value
                ?? httpContext.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
                ?? "Anonymous";
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));

            // Request path
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "Path", httpContext.Request.Path.Value));

            // HTTP method
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "Method", httpContext.Request.Method));

            // Query string (use with caution - may contain sensitive data)
            if (!string.IsNullOrEmpty(httpContext.Request.QueryString.Value))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "QueryString", httpContext.Request.QueryString.Value));
            }
        }
    }

    // ============ REQUEST/RESPONSE LOGGING MIDDLEWARE ============

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
            using (LogContext.PushProperty("UserId", GetUserId(context)))
            {
                var stopwatch = Stopwatch.StartNew();

                // Log incoming request
                _logger.LogInformation(
                    "Incoming {Method} {Path}",
                    context.Request.Method,
                    context.Request.Path);

                try
                {
                    await _next(context);
                    stopwatch.Stop();

                    // Log successful response
                    LogResponse(context, stopwatch);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    // Log failed request
                    _logger.LogError(ex,
                        "Request {Method} {Path} failed after {ElapsedMs}ms",
                        context.Request.Method,
                        context.Request.Path,
                        stopwatch.ElapsedMilliseconds);

                    throw;
                }
            }
        }

        private void LogResponse(HttpContext context, Stopwatch stopwatch)
        {
            var statusCode = context.Response.StatusCode;
            var logLevel = statusCode >= 500 ? LogLevel.Error :
                          statusCode >= 400 ? LogLevel.Warning :
                          LogLevel.Information;

            _logger.Log(logLevel,
                "Response {Method} {Path} completed with status {StatusCode} in {ElapsedMs}ms",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                stopwatch.ElapsedMilliseconds);
        }

        private static string GetUserId(HttpContext context)
        {
            return context.User?.FindFirst("sub")?.Value
                ?? context.User?.Identity?.Name
                ?? "Anonymous";
        }
    }

    // ============ USAGE IN PROGRAM.CS ============

    /*
    var builder = WebApplicationBuilder.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog(SerilogConfiguration.ConfigureLogger);

    // Add services
    builder.Services.AddSerilogServices();
    builder.Services.AddControllers();

    var app = builder.Build();

    // Add request/response logging middleware
    app.UseMiddleware<RequestResponseLoggingMiddleware>();

    // Log startup
    Log.Information("Application starting, Environment: {Environment}",
        builder.Environment.EnvironmentName);

    app.UseRouting();
    app.MapControllers();

    try
    {
        app.Run();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Application terminated unexpectedly");
    }
    finally
    {
        Log.CloseAndFlush();
    }
    */

    // ============ APPSETTINGS.JSON CONFIGURATION ============

    /*
    {
      "Serilog": {
        "MinimumLevel": {
          "Default": "Information",
          "Override": {
            "Microsoft": "Warning",
            "System": "Warning",
            "YourApp.Domain": "Debug"
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
              "retainedFileCountLimit": 30,
              "restrictedToEventType": "Error"
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
    */
}
