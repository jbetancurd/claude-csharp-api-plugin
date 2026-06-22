// Template: AOP Error Handling Decorator Pattern
// Cross-cutting concerns: Error handling, logging, retry, metrics

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace YourApp.Application.Decorators
{
    /// <summary>
    /// AOP (Aspect-Oriented Programming) using Decorator Pattern
    ///
    /// Benefits:
    /// - Separation of concerns: Business logic vs. cross-cutting concerns
    /// - Reusability: Same decorator for multiple services
    /// - Testability: Test business logic separately from error handling
    /// - Configuration: Change behavior without modifying service
    ///
    /// Example: Instead of this in every service:
    /// try { ... } catch { _logger.Error(...); throw; }
    ///
    /// We wrap the service with a decorator that handles it
    /// </summary>

    // ============ INTERFACE ============

    public interface IOrderApplicationService
    {
        Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
        Task<OrderDto> ApproveOrderAsync(int id);
        Task<OrderDto> CancelOrderAsync(int id, string reason);
    }

    // ============ ORIGINAL SERVICE (Clean Business Logic) ============

    /// <summary>
    /// Service with ONLY business logic - no error handling, logging, or metrics
    /// </summary>
    public class OrderApplicationService : IOrderApplicationService
    {
        private readonly IOrderRepository _orderRepository;

        public OrderApplicationService(IOrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        // No try-catch, no logging, no metrics
        // Just business logic
        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
        {
            var order = new Order(dto.CustomerId);
            foreach (var item in dto.Items)
            {
                order.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
            }

            await _orderRepository.AddAsync(order);
            return MapToDto(order);
        }

        public async Task<OrderDto> ApproveOrderAsync(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            order.Approve();
            await _orderRepository.UpdateAsync(order);
            return MapToDto(order);
        }

        public async Task<OrderDto> CancelOrderAsync(int id, string reason)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            order.Cancel(reason);
            await _orderRepository.UpdateAsync(order);
            return MapToDto(order);
        }

        private OrderDto MapToDto(Order order) => new();
    }

    // ============ DECORATOR 1: LOGGING & ERROR HANDLING ============

    /// <summary>
    /// Decorator: Adds logging and error handling
    /// Wraps the service transparently
    /// </summary>
    public class LoggingErrorHandlingDecorator : IOrderApplicationService
    {
        private readonly IOrderApplicationService _innerService;
        private readonly ILogger<LoggingErrorHandlingDecorator> _logger;

        public LoggingErrorHandlingDecorator(
            IOrderApplicationService innerService,
            ILogger<LoggingErrorHandlingDecorator> logger)
        {
            _innerService = innerService;
            _logger = logger;
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
        {
            try
            {
                _logger.LogInformation("Creating order for customer {CustomerId}", dto.CustomerId);
                var result = await _innerService.CreateOrderAsync(dto);
                _logger.LogInformation("Order {OrderId} created successfully", result.Id);
                return result;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid order data for customer {CustomerId}",
                    dto.CustomerId);
                throw new ApplicationException("Invalid order data", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create order for customer {CustomerId}",
                    dto.CustomerId);
                throw;
            }
        }

        public async Task<OrderDto> ApproveOrderAsync(int id)
        {
            try
            {
                _logger.LogInformation("Approving order {OrderId}", id);
                var result = await _innerService.ApproveOrderAsync(id);
                _logger.LogInformation("Order {OrderId} approved successfully", id);
                return result;
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Order {OrderId} not found for approval", id);
                throw new OrderNotFoundException(id);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot approve order {OrderId}: {Reason}", id, ex.Message);
                throw new ApplicationException($"Cannot approve order: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve order {OrderId}", id);
                throw;
            }
        }

        public async Task<OrderDto> CancelOrderAsync(int id, string reason)
        {
            try
            {
                _logger.LogInformation("Cancelling order {OrderId}, Reason: {Reason}", id, reason);
                var result = await _innerService.CancelOrderAsync(id, reason);
                _logger.LogInformation("Order {OrderId} cancelled successfully", id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel order {OrderId}", id);
                throw;
            }
        }
    }

    // ============ DECORATOR 2: PERFORMANCE METRICS ============

    /// <summary>
    /// Decorator: Adds performance monitoring
    /// Measures execution time for each operation
    /// </summary>
    public class PerformanceMonitoringDecorator : IOrderApplicationService
    {
        private readonly IOrderApplicationService _innerService;
        private readonly ILogger<PerformanceMonitoringDecorator> _logger;

        public PerformanceMonitoringDecorator(
            IOrderApplicationService innerService,
            ILogger<PerformanceMonitoringDecorator> logger)
        {
            _innerService = innerService;
            _logger = logger;
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.CreateOrderAsync(dto);
                stopwatch.Stop();

                LogPerformance(nameof(CreateOrderAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch
            {
                stopwatch.Stop();
                LogPerformance(nameof(CreateOrderAsync), stopwatch.ElapsedMilliseconds, isError: true);
                throw;
            }
        }

        public async Task<OrderDto> ApproveOrderAsync(int id)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.ApproveOrderAsync(id);
                stopwatch.Stop();

                LogPerformance(nameof(ApproveOrderAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch
            {
                stopwatch.Stop();
                LogPerformance(nameof(ApproveOrderAsync), stopwatch.ElapsedMilliseconds, isError: true);
                throw;
            }
        }

        public async Task<OrderDto> CancelOrderAsync(int id, string reason)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _innerService.CancelOrderAsync(id, reason);
                stopwatch.Stop();

                LogPerformance(nameof(CancelOrderAsync), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch
            {
                stopwatch.Stop();
                LogPerformance(nameof(CancelOrderAsync), stopwatch.ElapsedMilliseconds, isError: true);
                throw;
            }
        }

        private void LogPerformance(string methodName, long elapsedMs, bool isError = false)
        {
            var level = elapsedMs > 1000 ? LogLevel.Warning : LogLevel.Information;
            _logger.Log(level,
                "Method {MethodName} completed in {ElapsedMs}ms{Error}",
                methodName, elapsedMs, isError ? " (error)" : "");
        }
    }

    // ============ DECORATOR 3: RETRY POLICY (Polly) ============

    /// <summary>
    /// Decorator: Adds retry logic for transient failures
    /// Uses Polly for resilience patterns
    /// </summary>
    public class RetryDecorator : IOrderApplicationService
    {
        private readonly IOrderApplicationService _innerService;
        private readonly IAsyncPolicy<OrderDto> _policy;
        private readonly ILogger<RetryDecorator> _logger;

        public RetryDecorator(
            IOrderApplicationService innerService,
            ILogger<RetryDecorator> logger)
        {
            _innerService = innerService;
            _logger = logger;

            // Retry 3 times for transient exceptions
            _policy = Policy<OrderDto>
                .Handle<TimeoutException>()
                .Or<InvalidOperationException>(ex => ex.Message.Contains("temporarily unavailable"))
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (outcome, duration, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {RetryCount} after {DelayMs}ms due to: {Exception}",
                            retryCount, duration.TotalMilliseconds,
                            outcome.Exception?.Message ?? outcome.Result?.ToString());
                    });
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
        {
            return await _policy.ExecuteAsync(() =>
                _innerService.CreateOrderAsync(dto));
        }

        public async Task<OrderDto> ApproveOrderAsync(int id)
        {
            return await _policy.ExecuteAsync(() =>
                _innerService.ApproveOrderAsync(id));
        }

        public async Task<OrderDto> CancelOrderAsync(int id, string reason)
        {
            return await _policy.ExecuteAsync(() =>
                _innerService.CancelOrderAsync(id, reason));
        }
    }

    // ============ GENERIC DECORATOR (Reusable) ============

    /// <summary>
    /// Generic decorator for any service method
    /// Wraps sync and async methods
    /// </summary>
    public abstract class BaseServiceDecorator<TService> where TService : class
    {
        protected readonly TService InnerService;
        protected readonly ILogger<BaseServiceDecorator<TService>> Logger;

        protected BaseServiceDecorator(
            TService innerService,
            ILogger<BaseServiceDecorator<TService>> logger)
        {
            InnerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected async Task<T> ExecuteAsync<T>(
            string operationName,
            Func<Task<T>> operation,
            Action<Exception> onError = null)
        {
            try
            {
                Logger.LogInformation("Executing {OperationName}", operationName);
                var result = await operation();
                Logger.LogInformation("{OperationName} completed successfully", operationName);
                return result;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{OperationName} failed", operationName);
                onError?.Invoke(ex);
                throw;
            }
        }
    }

    // ============ DEPENDENCY INJECTION SETUP ============

    /// <summary>
    /// Register decorators in DI container
    /// </summary>
    public static class DecoratorServiceCollectionExtensions
    {
        public static IServiceCollection AddOrderApplicationServiceWithDecorators(
            this IServiceCollection services)
        {
            // Register the core service
            services.AddScoped<OrderApplicationService>();

            // Register decorated version (wraps original)
            services.AddScoped<IOrderApplicationService>(provider =>
            {
                var coreService = provider.GetRequiredService<OrderApplicationService>();
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                // Chain decorators: Performance → Logging → Retry
                IOrderApplicationService decorated = coreService;

                // Apply Retry decorator
                decorated = new RetryDecorator(decorated,
                    loggerFactory.CreateLogger<RetryDecorator>());

                // Apply Logging & Error Handling decorator
                decorated = new LoggingErrorHandlingDecorator(decorated,
                    loggerFactory.CreateLogger<LoggingErrorHandlingDecorator>());

                // Apply Performance Monitoring decorator
                decorated = new PerformanceMonitoringDecorator(decorated,
                    loggerFactory.CreateLogger<PerformanceMonitoringDecorator>());

                return decorated;
            });

            return services;
        }
    }

    // Usage in Program.cs:
    // builder.Services.AddOrderApplicationServiceWithDecorators();

    // ============ EXAMPLE: USING WITH FACTORY PATTERN ============

    /// <summary>
    /// Alternative: Use factory to reduce boilerplate
    /// </summary>
    public class ServiceDecoratorFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceDecoratorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IOrderApplicationService CreateOrderService()
        {
            var core = _serviceProvider.GetRequiredService<OrderApplicationService>();
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();

            return new RetryDecorator(
                new LoggingErrorHandlingDecorator(
                    new PerformanceMonitoringDecorator(
                        core,
                        loggerFactory.CreateLogger<PerformanceMonitoringDecorator>()),
                    loggerFactory.CreateLogger<LoggingErrorHandlingDecorator>()),
                loggerFactory.CreateLogger<RetryDecorator>());
        }
    }

    // ============ ADVANTAGES OF AOP WITH DECORATORS ============

    /*
    ✅ Clean separation of concerns:
       - Service: business logic only
       - Decorator: cross-cutting concerns (logging, error handling, metrics)

    ✅ Easy to test:
       - Test service without decorators
       - Test each decorator independently
       - Combine in tests as needed

    ✅ Reusable:
       - Same decorator for multiple services
       - Stack decorators for complex behavior

    ✅ Configuration:
       - Add/remove decorators without changing service
       - Order matters: applies outer to inner

    ✅ No attributes or reflection:
       - Explicit and performant
       - Clear dependency flow

    ✅ Testable:
       // Test without decorators
       var service = new OrderApplicationService(mockRepository);
       var result = await service.CreateOrderAsync(dto);

       // Test with specific decorator
       var decorated = new LoggingErrorHandlingDecorator(service, mockLogger);
       var result = await decorated.CreateOrderAsync(dto);
    */
}
