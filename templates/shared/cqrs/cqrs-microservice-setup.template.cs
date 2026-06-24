// CQRS Setup for Microservices - Extension Methods
// Add to: src/YourMicroservice.Infrastructure/DependencyInjection.cs

using MediatR;
using YourMicroservice.Application.Commands;
using YourMicroservice.Application.Queries;
using YourMicroservice.Application.EventHandlers;

namespace YourMicroservice.Infrastructure.DependencyInjection;

/// <summary>
/// CQRS (Command Query Responsibility Segregation) setup for microservices.
/// Separates write operations (Commands) from read operations (Queries).
/// </summary>
public static class CqrsSetupExtensions
{
    /// <summary>
    /// Add complete CQRS infrastructure for microservice.
    /// </summary>
    public static IServiceCollection AddCqrsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ============================================================
        // 1. ADD MEDIATOR (Command/Query Router)
        // ============================================================
        services.AddMediatR(config =>
        {
            // Register all handlers from Application assembly
            config.RegisterServicesFromAssembly(typeof(ApplicationAssemblyReference).Assembly);

            // Add request/response logging
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));

            // Add validation
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // ============================================================
        // 2. ADD WRITE SIDE (Commands)
        // ============================================================
        services.AddScoped(typeof(IRequestHandler<>), typeof(CommandHandler<>));
        services.AddScoped(typeof(IRequestHandler<,>), typeof(CommandHandler<,>));

        // ============================================================
        // 3. ADD READ SIDE (Queries)
        // ============================================================
        services.AddScoped(typeof(IRequestHandler<>), typeof(QueryHandler<>));
        services.AddScoped(typeof(IRequestHandler<,>), typeof(QueryHandler<,>));

        // ============================================================
        // 4. ADD EVENT BUS
        // ============================================================
        services.AddSingleton<IEventBus, EventBus>();
        services.AddScoped<IEventPublisher, EventPublisher>();
        services.AddScoped<IEventStore, EventStore>();

        // ============================================================
        // 5. ADD EVENT HANDLERS
        // ============================================================
        services.AddEventHandlers();

        // ============================================================
        // 6. ADD REPOSITORIES (Separate for Write and Read)
        // ============================================================
        services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));

        return services;
    }

    /// <summary>
    /// Register all domain event handlers.
    /// Automatically discovers and registers all IEventHandler implementations.
    /// </summary>
    private static void AddEventHandlers(this IServiceCollection services)
    {
        // Get all event handler types
        var handlerType = typeof(IEventHandler<>);
        var handlers = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => p.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType));

        foreach (var handler in handlers)
        {
            var eventType = handler.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
                .GetGenericArguments()
                .First();

            var serviceType = handlerType.MakeGenericType(eventType);
            services.AddScoped(serviceType, handler);
        }
    }
}

/// <summary>
/// Marker class for assembly reference (for MediatR scanning).
/// </summary>
public class ApplicationAssemblyReference { }

// ============================================================
// COMMAND HANDLING
// ============================================================

/// <summary>
/// Base class for all command handlers.
/// Commands represent write operations (changes to state).
/// </summary>
public abstract class CommandHandler<TCommand> : IRequestHandler<TCommand>
    where TCommand : IRequest
{
    protected readonly IWriteRepository<AggregateRoot> WriteRepository;
    protected readonly IEventPublisher EventPublisher;
    protected readonly ILogger Logger;

    protected CommandHandler(
        IWriteRepository<AggregateRoot> writeRepository,
        IEventPublisher eventPublisher,
        ILogger logger)
    {
        WriteRepository = writeRepository;
        EventPublisher = eventPublisher;
        Logger = logger;
    }

    public abstract Task Handle(TCommand request, CancellationToken cancellationToken);
}

/// <summary>
/// Base class for commands with return value.
/// </summary>
public abstract class CommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : IRequest<TResponse>
{
    protected readonly IWriteRepository<AggregateRoot> WriteRepository;
    protected readonly IEventPublisher EventPublisher;
    protected readonly ILogger Logger;

    protected CommandHandler(
        IWriteRepository<AggregateRoot> writeRepository,
        IEventPublisher eventPublisher,
        ILogger logger)
    {
        WriteRepository = writeRepository;
        EventPublisher = eventPublisher;
        Logger = logger;
    }

    public abstract Task<TResponse> Handle(TCommand request, CancellationToken cancellationToken);

    /// <summary>
    /// Helper to publish domain events after command handling.
    /// </summary>
    protected async Task PublishEventsAsync(AggregateRoot aggregate)
    {
        var events = aggregate.GetUncommittedEvents();

        foreach (var domainEvent in events)
        {
            await EventPublisher.PublishAsync(domainEvent);
        }

        aggregate.MarkEventsAsCommitted();
    }
}

// ============================================================
// QUERY HANDLING
// ============================================================

/// <summary>
/// Base class for query handlers.
/// Queries represent read operations (no state changes).
/// Optimized for read performance.
/// </summary>
public abstract class QueryHandler<TQuery> : IRequestHandler<TQuery>
    where TQuery : IRequest
{
    protected readonly IReadRepository<object> ReadRepository;
    protected readonly IDistributedCache Cache;
    protected readonly ILogger Logger;

    protected QueryHandler(
        IReadRepository<object> readRepository,
        IDistributedCache cache,
        ILogger logger)
    {
        ReadRepository = readRepository;
        Cache = cache;
        Logger = logger;
    }

    public abstract Task Handle(TQuery request, CancellationToken cancellationToken);
}

/// <summary>
/// Base class for queries with return value.
/// </summary>
public abstract class QueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IRequest<TResponse>
{
    protected readonly IReadRepository<object> ReadRepository;
    protected readonly IDistributedCache Cache;
    protected readonly ILogger Logger;

    protected QueryHandler(
        IReadRepository<object> readRepository,
        IDistributedCache cache,
        ILogger logger)
    {
        ReadRepository = readRepository;
        Cache = cache;
        Logger = logger;
    }

    public abstract Task<TResponse> Handle(TQuery request, CancellationToken cancellationToken);

    /// <summary>
    /// Helper to get cached result or fetch and cache.
    /// </summary>
    protected async Task<T> GetCachedOrFetchAsync<T>(
        string cacheKey,
        Func<Task<T>> fetchFunc,
        TimeSpan? cacheDuration = null)
    {
        // Try cache first
        var cached = await Cache.GetAsync(cacheKey);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<T>(cached)!;
        }

        // Fetch from repository
        var result = await fetchFunc();

        // Cache result
        cacheDuration ??= TimeSpan.FromMinutes(5);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = cacheDuration
        };

        await Cache.SetAsync(
            cacheKey,
            JsonSerializer.SerializeToUtf8Bytes(result),
            options);

        return result;
    }
}

// ============================================================
// EXAMPLE: CREATE ORDER COMMAND
// ============================================================

/// <summary>
/// Command to create a new order.
/// Represents a write operation (change to state).
/// </summary>
public record CreateOrderCommand(
    int CustomerId,
    List<OrderItemDto> Items) : IRequest<int>;

/// <summary>
/// Handler for CreateOrderCommand.
/// Validates, creates aggregate, publishes events.
/// </summary>
public class CreateOrderCommandHandler(
    IWriteRepository<Order> orderRepository,
    IEventPublisher eventPublisher,
    ILogger<CreateOrderCommandHandler> logger)
    : CommandHandler<CreateOrderCommand, int>
{
    public override async Task<int> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        logger.LogInformation(
            "Handling CreateOrderCommand for customer {CustomerId}",
            command.CustomerId);

        // 1. Validate
        ValidateOrderItems(command.Items);

        // 2. Create aggregate (Domain Model)
        var order = Order.Create(command.CustomerId, command.Items);

        // 3. Persist to write database (normalized schema)
        await orderRepository.AddAsync(order, ct);

        // 4. Publish domain events to event bus
        // Events will be consumed by:
        // - Event handlers (update read model)
        // - Other microservices (inventory, payment, etc.)
        await PublishEventsAsync(order);

        logger.LogInformation(
            "Order {OrderId} created successfully for customer {CustomerId}",
            order.Id, command.CustomerId);

        return order.Id;
    }

    private static void ValidateOrderItems(List<OrderItemDto> items)
    {
        if (!items.Any())
            throw new ValidationException("Order must contain at least one item");

        if (items.Any(i => i.Quantity <= 0))
            throw new ValidationException("Item quantities must be positive");
    }
}

// ============================================================
// EXAMPLE: GET ORDER SUMMARY QUERY
// ============================================================

/// <summary>
/// Query to get order summary.
/// Represents a read operation (no state changes).
/// Optimized for read performance.
/// </summary>
public record GetOrderSummaryQuery(int OrderId) : IRequest<OrderSummaryDto>;

/// <summary>
/// Handler for GetOrderSummaryQuery.
/// Queries optimized read model (denormalized).
/// </summary>
public class GetOrderSummaryQueryHandler(
    IReadRepository<OrderSummary> readRepository,
    IDistributedCache cache,
    ILogger<GetOrderSummaryQueryHandler> logger)
    : QueryHandler<GetOrderSummaryQuery, OrderSummaryDto>
{
    public override async Task<OrderSummaryDto> Handle(
        GetOrderSummaryQuery query,
        CancellationToken ct)
    {
        logger.LogInformation(
            "Handling GetOrderSummaryQuery for order {OrderId}",
            query.OrderId);

        // Use cache to optimize reads
        var cacheKey = $"order-summary-{query.OrderId}";

        var result = await GetCachedOrFetchAsync(
            cacheKey,
            async () =>
            {
                var summary = await readRepository.GetByIdAsync(query.OrderId, ct)
                    ?? throw new NotFoundException($"Order {query.OrderId} not found");

                return new OrderSummaryDto(
                    summary.Id,
                    summary.CustomerId,
                    summary.Status,
                    summary.Total,
                    summary.CreatedAt);
            },
            cacheDuration: TimeSpan.FromMinutes(10));

        return result;
    }
}

// ============================================================
// SUPPORTING INTERFACES
// ============================================================

public interface IEventBus
{
    Task PublishAsync<T>(T @event) where T : DomainEvent;
}

public interface IEventPublisher
{
    Task PublishAsync(DomainEvent domainEvent);
}

public interface IEventStore
{
    Task AppendAsync<T>(T aggregate) where T : AggregateRoot;
    Task<T?> GetByIdAsync<T>(int id) where T : AggregateRoot;
}

public interface IEventHandler<TEvent> where TEvent : DomainEvent
{
    Task HandleAsync(TEvent @event);
}

public interface IWriteRepository<T> where T : IAggregateRoot
{
    Task AddAsync(T aggregate, CancellationToken ct = default);
    Task UpdateAsync(T aggregate, CancellationToken ct = default);
    Task DeleteAsync(T aggregate, CancellationToken ct = default);
}

public interface IReadRepository<T>
{
    Task<T?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<T>> GetAllAsync(CancellationToken ct = default);
    Task<List<T>> QueryAsync(Func<IQueryable<T>, IQueryable<T>> predicate, CancellationToken ct = default);
}

// ============================================================
// BEHAVIORS (Cross-cutting Concerns)
// ============================================================

/// <summary>
/// Logging behavior for commands and queries.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();
            _logger.LogInformation(
                "{RequestName} completed in {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "{RequestName} failed after {ElapsedMilliseconds}ms",
                requestName,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

/// <summary>
/// Validation behavior for commands.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = validationResults
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}

// ============================================================
// DTO CLASSES
// ============================================================

public record OrderItemDto(int ProductId, int Quantity, decimal UnitPrice);
public record OrderSummaryDto(int Id, int CustomerId, string Status, decimal Total, DateTime CreatedAt);
