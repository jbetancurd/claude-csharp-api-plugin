// Template: Global Exception Filter for ASP.NET Core
// Alternative to AOP Decorators: Filter-based error handling

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YourApp.API.Filters
{
    /// <summary>
    /// Global Exception Filter for ASP.NET Core
    ///
    /// Benefits over Decorators:
    /// - Simpler for HTTP-layer concerns
    /// - No decorator wrapping needed
    /// - Automatic for all controllers
    /// - Standardizes HTTP error responses
    ///
    /// Best for: HTTP-specific error handling
    /// Use Decorators for: Business logic error handling
    /// </summary>

    // ============ EXCEPTION FILTER ============

    /// <summary>
    /// Global exception filter - catches all exceptions from controllers
    /// </summary>
    public class GlobalExceptionFilter : IAsyncExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnExceptionAsync(ExceptionContext context)
        {
            var exception = context.Exception;

            // Log the exception
            LogException(exception, context);

            // Create appropriate response based on exception type
            var response = CreateErrorResponse(exception, context);

            // Set response
            context.Result = new ObjectResult(response)
            {
                StatusCode = response.StatusCode
            };

            // Mark as handled
            context.ExceptionHandled = true;

            await Task.CompletedTask;
        }

        private void LogException(Exception exception, ExceptionContext context)
        {
            var path = context.HttpContext.Request.Path;
            var method = context.HttpContext.Request.Method;
            var userId = context.HttpContext.User?.FindFirst("sub")?.Value ?? "Anonymous";

            switch (exception)
            {
                case ValidationException vex:
                    _logger.LogWarning(
                        "Validation error: {Path} {Method} by {UserId}, Errors: {@Errors}",
                        path, method, userId, vex.Errors);
                    break;

                case KeyNotFoundException knfex:
                    _logger.LogWarning(
                        "Resource not found: {Path} {Method} by {UserId}",
                        path, method, userId);
                    break;

                case UnauthorizedAccessException uaex:
                    _logger.LogWarning(
                        "Unauthorized access: {Path} {Method} by {UserId}",
                        path, method, userId);
                    break;

                case OperationCanceledException ocex:
                    _logger.LogWarning(
                        "Operation cancelled: {Path} {Method}",
                        path, method);
                    break;

                default:
                    _logger.LogError(exception,
                        "Unhandled exception: {Path} {Method} by {UserId}",
                        path, method, userId);
                    break;
            }
        }

        private ErrorResponse CreateErrorResponse(Exception exception, ExceptionContext context)
        {
            var traceId = context.HttpContext.TraceIdentifier;

            return exception switch
            {
                // Validation errors - 400
                ValidationException vex => new ErrorResponse
                {
                    StatusCode = 400,
                    Message = "Validation failed",
                    Errors = vex.Errors,
                    TraceId = traceId
                },

                // Not found - 404
                KeyNotFoundException => new ErrorResponse
                {
                    StatusCode = 404,
                    Message = "Resource not found",
                    TraceId = traceId
                },

                // Unauthorized - 401
                UnauthorizedAccessException => new ErrorResponse
                {
                    StatusCode = 401,
                    Message = "Unauthorized access",
                    TraceId = traceId
                },

                // Forbidden - 403
                ForbiddenException => new ErrorResponse
                {
                    StatusCode = 403,
                    Message = "Access forbidden",
                    TraceId = traceId
                },

                // Conflict - 409
                ConflictException cex => new ErrorResponse
                {
                    StatusCode = 409,
                    Message = cex.Message,
                    TraceId = traceId
                },

                // Business logic errors - 400
                ApplicationException aex => new ErrorResponse
                {
                    StatusCode = 400,
                    Message = aex.Message,
                    TraceId = traceId
                },

                // Operation cancelled - 408
                OperationCanceledException => new ErrorResponse
                {
                    StatusCode = 408,
                    Message = "Request timeout",
                    TraceId = traceId
                },

                // Default: Internal server error - 500
                _ => new ErrorResponse
                {
                    StatusCode = 500,
                    Message = "Internal server error",
                    TraceId = traceId,
                    Details = context.HttpContext.Request.Host.Host == "localhost"
                        ? exception.ToString() // Only in development
                        : null
                }
            };
        }
    }

    // ============ ACTION FILTER (Pre/Post Processing) ============

    /// <summary>
    /// Action filter for validation before action executes
    /// </summary>
    public class ValidateModelFilter : IAsyncActionFilter
    {
        private readonly ILogger<ValidateModelFilter> _logger;

        public ValidateModelFilter(ILogger<ValidateModelFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // Pre-execution: Validate model
            if (!context.ModelState.IsValid)
            {
                var errors = new List<string>();
                foreach (var state in context.ModelState.Values)
                {
                    foreach (var error in state.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }

                _logger.LogWarning("Model validation failed: {@Errors}", errors);

                context.Result = new BadRequestObjectResult(new ErrorResponse
                {
                    StatusCode = 400,
                    Message = "Model validation failed",
                    Errors = errors,
                    TraceId = context.HttpContext.TraceIdentifier
                });

                return;
            }

            // Execute action
            var executedContext = await next();

            // Post-execution: Can add response processing here
        }
    }

    // ============ RESOURCE FILTER (Authentication/Authorization) ============

    /// <summary>
    /// Resource filter for authentication/authorization checks
    /// Runs earlier in pipeline than action filters
    /// </summary>
    public class AuthorizationResourceFilter : IAsyncResourceFilter
    {
        private readonly ILogger<AuthorizationResourceFilter> _logger;

        public AuthorizationResourceFilter(ILogger<AuthorizationResourceFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnResourceExecutionAsync(
            ResourceExecutingContext context,
            ResourceExecutionDelegate next)
        {
            // Check if user is authenticated
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning("Unauthenticated request to {Path}",
                    context.HttpContext.Request.Path);

                context.Result = new UnauthorizedObjectResult(new ErrorResponse
                {
                    StatusCode = 401,
                    Message = "Authentication required",
                    TraceId = context.HttpContext.TraceIdentifier
                });

                return;
            }

            // Proceed
            await next();
        }
    }

    // ============ EXCEPTION CLASSES ============

    /// <summary>
    /// Domain exception base class
    /// </summary>
    public abstract class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }

    public class ValidationException : DomainException
    {
        public List<string> Errors { get; }

        public ValidationException(string message, List<string> errors = null)
            : base(message)
        {
            Errors = errors ?? new List<string>();
        }
    }

    public class ForbiddenException : DomainException
    {
        public ForbiddenException(string message) : base(message) { }
    }

    public class ConflictException : DomainException
    {
        public ConflictException(string message) : base(message) { }
    }

    // ============ ERROR RESPONSE ============

    /// <summary>
    /// Standardized error response DTO
    /// </summary>
    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public List<string> Errors { get; set; }
        public string TraceId { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // ============ PROBLEM DETAILS (RFC 7807) ============

    /// <summary>
    /// RFC 7807 Problem Details format
    /// Standardized error response for APIs
    /// </summary>
    public class ProblemDetailsErrorFilter : IAsyncExceptionFilter
    {
        private readonly ILogger<ProblemDetailsErrorFilter> _logger;

        public ProblemDetailsErrorFilter(ILogger<ProblemDetailsErrorFilter> logger)
        {
            _logger = logger;
        }

        public async Task OnExceptionAsync(ExceptionContext context)
        {
            var problemDetails = new ProblemDetails
            {
                Type = "https://api.example.com/errors/" + GetErrorCode(context.Exception),
                Title = GetErrorTitle(context.Exception),
                Detail = context.Exception.Message,
                Status = GetHttpStatusCode(context.Exception),
                Instance = context.HttpContext.Request.Path
            };

            problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

            _logger.LogError(context.Exception, "Request failed: {Title}", problemDetails.Title);

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = problemDetails.Status
            };

            context.ExceptionHandled = true;

            await Task.CompletedTask;
        }

        private string GetErrorCode(Exception ex) => ex.GetType().Name;

        private string GetErrorTitle(Exception ex) => ex switch
        {
            ValidationException => "Validation Failed",
            KeyNotFoundException => "Not Found",
            UnauthorizedAccessException => "Unauthorized",
            ForbiddenException => "Forbidden",
            ConflictException => "Conflict",
            OperationCanceledException => "Request Timeout",
            _ => "Internal Server Error"
        };

        private int GetHttpStatusCode(Exception ex) => ex switch
        {
            ValidationException => 400,
            KeyNotFoundException => 404,
            UnauthorizedAccessException => 401,
            ForbiddenException => 403,
            ConflictException => 409,
            OperationCanceledException => 408,
            _ => 500
        };
    }

    // ============ DEPENDENCY INJECTION SETUP ============

    /// <summary>
    /// Register global filters in DI
    /// </summary>
    public static class ErrorHandlingServiceCollectionExtensions
    {
        public static IServiceCollection AddGlobalErrorHandling(
            this IServiceCollection services)
        {
            services.AddScoped<GlobalExceptionFilter>();
            services.AddScoped<ValidateModelFilter>();
            services.AddScoped<AuthorizationResourceFilter>();
            services.AddScoped<ProblemDetailsErrorFilter>();

            return services;
        }

        public static IServiceCollection AddGlobalFilters(
            this IServiceCollection services)
        {
            services.AddControllersWithViews(options =>
            {
                // Add global exception filter
                options.Filters.Add<GlobalExceptionFilter>();

                // Add model validation filter
                options.Filters.Add<ValidateModelFilter>();

                // Add authorization filter
                options.Filters.Add<AuthorizationResourceFilter>();
            });

            return services;
        }
    }

    // Usage in Program.cs:
    // builder.Services.AddGlobalErrorHandling();
    // OR
    // builder.Services.AddGlobalFilters();
}
