# CQRS for Microservices - Implementation Guide

**When to use**: Complex microservice with event-driven architecture  
**Benefit**: Separate optimization for reads vs writes, event sourcing, audit trail

---

## Quick Decision

**Use CQRS for your microservice if:**

```
✅ 2+ different read patterns (list, summary, search)
✅ Read operations >> write operations
✅ Complex business validation on writes
✅ Event-driven communication with other services
✅ Need audit trail
✅ Team: 2+ developers

❌ Skip CQRS if:
   Simple CRUD (less than 5 operations)
   Read = Write patterns are similar
   Single developer
   Rapid prototyping
```

---

## Architecture Overview

### Traditional Microservice (No CQRS)
```
HTTP Request
    ↓
Controller
    ↓
Service (handles both read and write)
    ↓
Database (single model)
    ↓
HTTP Response
```

### CQRS Microservice
```
WRITE PATH (Commands)          READ PATH (Queries)
    ↓                                ↓
  Command                          Query
    ↓                                ↓
Command Handler              Query Handler
    ↓                                ↓
Write Logic                    Read Cache/DB
    ↓                                ↓
Write DB (normalized)          Read DB (denormalized)
    ↓
Publish Events
    ↓
Event Handlers
    ↓
Update Read Models
```

---

## Implementation Steps

### Step 1: Add NuGet Packages

```bash
dotnet add package MediatR
dotnet add package MediatR.Extensions.Microsoft.DependencyInjection
dotnet add package FluentValidation
dotnet add package FluentValidation.DependencyInjectionExtensions
```

### Step 2: Create Command and Query Classes

```csharp
// Create Order Command (write operation)
public record CreateOrderCommand(
    int CustomerId,
    List<OrderItemDto> Items
) : IRequest<int>;  // Returns order ID

// Get Order Summary Query (read operation)
public record GetOrderSummaryQuery(
    int OrderId
) : IRequest<OrderSummaryDto>;  // Returns DTO
```

**Key Points:**
- Commands: Change state, return result
- Queries: Don't change state, return data only
- Use records for immutability
- Implement `IRequest` or `IRequest<T>` from MediatR

### Step 3: Create Handlers

```csharp
// Command Handler
public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, int>
{
    private readonly IWriteRepository<Order> _repository;
    private readonly IEventPublisher _eventPublisher;
    
    public CreateOrderCommandHandler(
        IWriteRepository<Order> repository,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
    }
    
    public async Task<int> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        // 1. Validate
        if (!request.Items.Any())
            throw new ValidationException("Order must have items");
        
        // 2. Create aggregate
        var order = Order.Create(request.CustomerId, request.Items);
        
        // 3. Persist to write database
        await _repository.AddAsync(order, ct);
        
        // 4. Publish events
        foreach (var @event in order.DomainEvents)
            await _eventPublisher.PublishAsync(@event);
        
        return order.Id;
    }
}

// Query Handler
public class GetOrderSummaryQueryHandler : IRequestHandler<GetOrderSummaryQuery, OrderSummaryDto>
{
    private readonly IReadRepository<OrderSummary> _readRepository;
    private readonly IDistributedCache _cache;
    
    public GetOrderSummaryQueryHandler(
        IReadRepository<OrderSummary> readRepository,
        IDistributedCache cache)
    {
        _readRepository = readRepository;
        _cache = cache;
    }
    
    public async Task<OrderSummaryDto> Handle(GetOrderSummaryQuery request, CancellationToken ct)
    {
        // 1. Check cache
        var cacheKey = $"order-summary-{request.OrderId}";
        var cached = await _cache.GetAsync(cacheKey, ct);
        if (cached != null)
            return JsonSerializer.Deserialize<OrderSummaryDto>(cached)!;
        
        // 2. Query read model
        var summary = await _readRepository.GetByIdAsync(request.OrderId, ct);
        
        // 3. Cache result
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetAsync(cacheKey, JsonSerializer.SerializeToUtf8Bytes(summary), cacheOptions, ct);
        
        return summary;
    }
}
```

### Step 4: Register in Program.cs

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// Add CQRS
builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// Add Validation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Add Write Repository
builder.Services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));

// Add Read Repository
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));

// Add Event Publisher
builder.Services.AddScoped<IEventPublisher, EventPublisher>();

// Add Distributed Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

var app = builder.Build();

// Map endpoints
app.MapControllers();
app.Run();
```

### Step 5: Dispatch Commands and Queries from Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    // WRITE: Create Order
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderCommand command)
    {
        var orderId = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetOrder), new { id = orderId }, orderId);
    }
    
    // READ: Get Order Summary
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var query = new GetOrderSummaryQuery(id);
        var summary = await _mediator.Send(query);
        return Ok(summary);
    }
}
```

---

## File Structure

```
YourMicroservice/
├── src/
│   ├── Domain/
│   │   ├── Aggregates/
│   │   │   └── Order.cs
│   │   ├── Events/
│   │   │   ├── OrderCreatedEvent.cs
│   │   │   └── OrderShippedEvent.cs
│   │   └── ValueObjects/
│   │
│   ├── Application/
│   │   ├── Commands/
│   │   │   ├── CreateOrderCommand.cs
│   │   │   └── CreateOrderCommandHandler.cs
│   │   ├── Queries/
│   │   │   ├── GetOrderSummaryQuery.cs
│   │   │   └── GetOrderSummaryQueryHandler.cs
│   │   ├── EventHandlers/
│   │   │   └── OrderCreatedEventHandler.cs
│   │   └── DTOs/
│   │       └── OrderSummaryDto.cs
│   │
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── WriteRepository.cs
│   │   │   ├── ReadRepository.cs
│   │   │   ├── EventStore.cs
│   │   │   └── DbContext.cs
│   │   ├── EventPublishing/
│   │   │   └── EventPublisher.cs
│   │   └── DependencyInjection.cs
│   │
│   └── Presentation/
│       └── Controllers/
│           └── OrdersController.cs
```

---

## Event Handling

### Synchronize Read Model

When a domain event is published, event handlers update the read model:

```csharp
// Domain event published from command handler
await _eventPublisher.PublishAsync(order.DomainEvents);

// Event handler receives event
public class OrderCreatedEventHandler : INotificationHandler<OrderCreatedEvent>
{
    private readonly IReadRepository<OrderSummary> _readRepository;
    
    public async Task Handle(OrderCreatedEvent @event, CancellationToken ct)
    {
        // Create optimized read model
        var summary = new OrderSummary
        {
            Id = @event.OrderId,
            CustomerId = @event.CustomerId,
            Status = "Created",
            Total = @event.Items.Sum(i => i.Total),
            CreatedAt = DateTime.UtcNow
        };
        
        // Write to read database (denormalized)
        await _readRepository.AddAsync(summary, ct);
    }
}
```

### Publish to Event Bus (Other Services)

```csharp
// In command handler, after persisting:
foreach (var @event in order.DomainEvents)
{
    // Publish to event bus (RabbitMQ, Service Bus, etc.)
    await _eventBus.PublishAsync(@event);
}

// Other microservices subscribe:
// - Inventory Service: Reserves stock
// - Payment Service: Processes payment
// - Notification Service: Sends email
```

---

## Caching Strategy

### Query-Side Caching

```csharp
// Read model with cache
public class GetOrderSummaryQueryHandler : IRequestHandler<GetOrderSummaryQuery, OrderSummaryDto>
{
    private readonly IReadRepository<OrderSummary> _repo;
    private readonly IDistributedCache _cache;
    
    public async Task<OrderSummaryDto> Handle(GetOrderSummaryQuery request, CancellationToken ct)
    {
        var cacheKey = $"order-summary-{request.OrderId}";
        
        // 1. Try cache (very fast)
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (cached != null)
            return JsonSerializer.Deserialize<OrderSummaryDto>(cached)!;
        
        // 2. Query read database (medium speed)
        var summary = await _repo.GetByIdAsync(request.OrderId, ct);
        
        // 3. Cache for next time
        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(summary),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            },
            ct);
        
        return summary;
    }
}
```

### Invalidate on Write

```csharp
// Command handler invalidates cache after write
public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand>
{
    private readonly IDistributedCache _cache;
    
    public async Task Handle(UpdateOrderCommand request, CancellationToken ct)
    {
        // ... update logic ...
        
        // Invalidate cache
        await _cache.RemoveAsync($"order-summary-{request.OrderId}", ct);
    }
}
```

---

## Benefits vs Complexity

### Benefits ✅
- **Performance**: Optimized reads with caching, normalized writes
- **Scalability**: Scale read and write independently
- **Auditability**: All changes recorded as events
- **Debugging**: Event log shows exact sequence of changes
- **Integration**: Events for other services to consume

### Complexity ⚠️
- **Eventual Consistency**: Read model updates after write completes
- **Debugging**: Multiple handlers and databases to trace
- **Deployment**: Synchronize read/write schemas carefully
- **Testing**: Must test command and query paths separately

---

## When CQRS is Overkill

**Don't use CQRS if:**
- ✗ Simple CRUD service (< 5 operations)
- ✗ Reads ≈ Writes (balanced traffic)
- ✗ Team size: 1-2 developers
- ✗ Single database sufficient
- ✗ No need for event sourcing

**Instead use:** Traditional layered architecture

```csharp
Controller → Service → Repository → Database
```

---

## Troubleshooting

### Read Model Out of Sync

**Problem**: Query returns stale data  
**Solution**: 
- Check event handler is registered
- Verify event handler exception handling
- Manually rebuild read model

### Performance Issues

**Problem**: Queries still slow despite CQRS  
**Solution**:
- Check read database indexes
- Add caching (Redis)
- Consider read model pagination

### Event Handler Failures

**Problem**: Events not processed  
**Solution**:
- Add dead-letter queue
- Implement retry logic
- Log all handler exceptions

---

## Next Steps

1. **Copy template**: `cqrs-microservice-setup.template.cs`
2. **Update to your domain**: Replace Order/OrderItem with your entities
3. **Register in Program.cs**: Use extension methods
4. **Implement handlers**: Create command/query handlers for your operations
5. **Test thoroughly**: Test both command and query paths
6. **Monitor**: Watch for eventual consistency issues

---

## Related Documentation

- `/docs/architecture/microservice-patterns.md` - Microservice patterns
- `/docs/decision-tree.md` - Step 6: CQRS decision
- `/templates/shared/cqrs/` - CQRS templates

---

## Complete Example

See `cqrs-microservice-setup.template.cs` for complete working example with:
- ✅ Commands and Queries
- ✅ Handlers with validation
- ✅ Event publishing
- ✅ Read model caching
- ✅ Dependency injection setup
- ✅ Behaviors (logging, validation)
