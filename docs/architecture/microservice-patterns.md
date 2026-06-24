# Microservice Architecture Patterns

Comprehensive guide for building production-grade microservices with CQRS, event sourcing, and bootstrap patterns.

---

## When to Use This Guide

**You're building a microservice if:**
- Single responsibility (one business capability)
- Independent deployment
- Own database
- Part of larger microservices ecosystem
- Examples: User Service, Order Service, Payment Service, Notification Service

---

## Pattern Decision Matrix

| Pattern | Best For | Complexity | Team Size |
|---------|----------|-----------|-----------|
| **Simple CRUD** | Data-heavy services | Low | 1-2 devs |
| **CQRS** | Complex logic, different read/write needs | Medium | 2-4 devs |
| **Event Sourcing** | Audit trail, temporal queries | High | 4+ devs |
| **CQRS + Events** | Complex + audit + scale | High | 4+ devs |

---

## CQRS for Microservices

### When CQRS Makes Sense in Microservices

**Use CQRS if:**
```
✅ Complex business logic (many validations, rules)
✅ Read patterns differ from write patterns
✅ Multiple read models needed
✅ Scaling reads separately from writes
✅ Audit trail important
✅ Team large enough (2+ developers)
✅ Data complexity justifies it

❌ Don't use if:
   - Simple CRUD operations
   - Team size: 1 developer
   - Rapid prototyping phase
   - Low traffic service
```

### Microservice CQRS Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     MICROSERVICE BOUNDARY                   │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────────┐  ┌──────────────────────┐        │
│  │   COMMAND SIDE       │  │    QUERY SIDE        │        │
│  │   (Write/Update)     │  │   (Read-Only)        │        │
│  ├──────────────────────┤  ├──────────────────────┤        │
│  │                      │  │                      │        │
│  │ Commands             │  │ Queries              │        │
│  │  ├─ CreateOrder      │  │  ├─ GetOrders       │        │
│  │  ├─ UpdateOrder      │  │  ├─ SearchOrders    │        │
│  │  └─ CancelOrder      │  │  └─ GetOrderStats   │        │
│  │                      │  │                      │        │
│  │ Handlers             │  │ Query Handlers       │        │
│  │  ├─ Validate         │  │  ├─ Read from Cache │        │
│  │  ├─ Execute Logic    │  │  ├─ Read from DB    │        │
│  │  └─ Persist          │  │  └─ Format Response │        │
│  │                      │  │                      │        │
│  │ Write DB             │  │ Read DB (Optimized) │        │
│  │ (Normalized)         │  │ (Denormalized)      │        │
│  │                      │  │                      │        │
│  │ Event Store          │  │                      │        │
│  │ (Audit Trail)        │  │                      │        │
│  │                      │  │                      │        │
│  └──────────────────────┘  └──────────────────────┘        │
│         ↓ Events                                             │
│    [Event Bus - RabbitMQ, Azure Service Bus]                │
│                                                               │
└─────────────────────────────────────────────────────────────┘
        ↓ Publishes Events to Other Services
  [Inventory Service]
  [Payment Service]
  [Notification Service]
```

### CQRS Implementation Pattern

```csharp
// 1. COMMAND SIDE (Write Operations)
public record CreateOrderCommand(int UserId, List<OrderItem> Items);

public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, int>
{
    private readonly IOrderRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    
    public async Task<int> HandleAsync(CreateOrderCommand command)
    {
        // Validate
        ValidateOrderItems(command.Items);
        
        // Create domain object
        var order = Order.Create(command.UserId, command.Items);
        
        // Persist to write database (normalized)
        await _repository.AddAsync(order);
        
        // Publish domain events
        foreach (var domainEvent in order.DomainEvents)
        {
            await _eventPublisher.PublishAsync(domainEvent);
        }
        
        return order.Id;
    }
}

// 2. QUERY SIDE (Read Operations)
public record GetOrderSummaryQuery(int OrderId);

public class GetOrderSummaryQueryHandler : IQueryHandler<GetOrderSummaryQuery, OrderSummaryDto>
{
    private readonly IOrderReadRepository _readRepository;
    
    public async Task<OrderSummaryDto> HandleAsync(GetOrderSummaryQuery query)
    {
        // Query optimized read model
        var summary = await _readRepository.GetOrderSummaryAsync(query.OrderId);
        
        return summary;
    }
}

// 3. EVENT HANDLER (Synchronize Read Model)
public class OrderCreatedEventHandler : IEventHandler<OrderCreatedEvent>
{
    private readonly IOrderReadRepository _readRepository;
    
    public async Task HandleAsync(OrderCreatedEvent @event)
    {
        // Create optimized read model
        var orderSummary = new OrderSummary
        {
            Id = @event.OrderId,
            UserId = @event.UserId,
            Total = @event.Items.Sum(i => i.Price * i.Quantity),
            Status = "Created",
            CreatedAt = DateTime.UtcNow
        };
        
        // Write to read database (denormalized)
        await _readRepository.AddAsync(orderSummary);
    }
}
```

---

## Bootstrap Pattern for Microservices

### What is Bootstrap Pattern?

The **bootstrap pattern** is the initialization sequence when your microservice starts:

```
Start → Configure → Register Services → Seed Data → Run
 ↓        ↓         ↓                    ↓           ↓
```

### Complete Bootstrap Implementation

```csharp
// Program.cs - Bootstrap Pattern
using YourService.Infrastructure.Bootstrap;

var builder = WebApplicationBuilder.CreateBuilder(args);
var config = builder.Configuration;

// ============================================================
// PHASE 1: CONFIGURE SERVICES
// ============================================================
builder.Services
    .AddInfrastructure(config)      // Database, Repositories
    .AddApplication()                // Services, Handlers
    .AddPresentation()               // Controllers, Middleware
    .AddCrossCuttingConcerns(config) // Logging, Caching, etc.
    .AddEventBus(config)             // RabbitMQ, Service Bus
    .AddDistributedTracing(config);  // Observability

// ============================================================
// PHASE 2: BUILD APPLICATION
// ============================================================
var app = builder.Build();

// ============================================================
// PHASE 3: INITIALIZE
// ============================================================
await app.Services.InitializeMicroserviceAsync();

// ============================================================
// PHASE 4: CONFIGURE PIPELINE
// ============================================================
app.UseMiddleware<LoggingMiddleware>()
   .UseMiddleware<ErrorHandlingMiddleware>()
   .UseRouting()
   .UseAuthorization();

// ============================================================
// PHASE 5: MAP ENDPOINTS
// ============================================================
app.MapHealthChecks("/health")
   .MapSwaggerUI("/swagger")
   .MapControllers();

// ============================================================
// PHASE 6: RUN
// ============================================================
await app.RunAsync();
```

### Bootstrap Service Implementation

```csharp
public interface IBootstrapService
{
    Task InitializeAsync();
    Task StartupAsync();
    Task ShutdownAsync();
}

public class MicroserviceBootstrap : IBootstrapService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MicroserviceBootstrap> _logger;
    
    public MicroserviceBootstrap(IServiceProvider services, ILogger<MicroserviceBootstrap> logger)
    {
        _services = services;
        _logger = logger;
    }
    
    public async Task InitializeAsync()
    {
        _logger.LogInformation("🚀 Initializing microservice...");
        
        // 1. Check database connectivity
        await CheckDatabaseAsync();
        
        // 2. Run migrations
        await RunMigrationsAsync();
        
        // 3. Seed data (if needed)
        await SeedDataAsync();
        
        // 4. Verify event bus connectivity
        await VerifyEventBusAsync();
        
        // 5. Load configuration
        await LoadConfigurationAsync();
        
        // 6. Register event handlers
        await RegisterEventHandlersAsync();
        
        _logger.LogInformation("✅ Microservice initialized successfully");
    }
    
    private async Task CheckDatabaseAsync()
    {
        _logger.LogInformation("📡 Checking database connectivity...");
        
        using (var scope = _services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var canConnect = await context.Database.CanConnectAsync();
            
            if (!canConnect)
                throw new InvalidOperationException("Cannot connect to database");
            
            _logger.LogInformation("✓ Database connection verified");
        }
    }
    
    private async Task RunMigrationsAsync()
    {
        _logger.LogInformation("🔄 Running database migrations...");
        
        using (var scope = _services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.MigrateAsync();
            
            _logger.LogInformation("✓ Migrations completed");
        }
    }
    
    private async Task SeedDataAsync()
    {
        _logger.LogInformation("🌱 Seeding initial data...");
        
        using (var scope = _services.CreateScope())
        {
            var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
            await seeder.SeedAsync();
            
            _logger.LogInformation("✓ Data seeded");
        }
    }
    
    private async Task VerifyEventBusAsync()
    {
        _logger.LogInformation("📨 Verifying event bus...");
        
        using (var scope = _services.CreateScope())
        {
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();
            await eventBus.HealthCheckAsync();
            
            _logger.LogInformation("✓ Event bus verified");
        }
    }
    
    private async Task LoadConfigurationAsync()
    {
        _logger.LogInformation("⚙️  Loading configuration...");
        
        using (var scope = _services.CreateScope())
        {
            var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();
            await configService.LoadAsync();
            
            _logger.LogInformation("✓ Configuration loaded");
        }
    }
    
    private async Task RegisterEventHandlersAsync()
    {
        _logger.LogInformation("🎯 Registering event handlers...");
        
        using (var scope = _services.CreateScope())
        {
            var eventRegistry = scope.ServiceProvider.GetRequiredService<IEventHandlerRegistry>();
            await eventRegistry.RegisterAllAsync();
            
            _logger.LogInformation("✓ Event handlers registered");
        }
    }
    
    public async Task StartupAsync()
    {
        _logger.LogInformation("▶️  Starting microservice...");
        // Additional startup logic
    }
    
    public async Task ShutdownAsync()
    {
        _logger.LogInformation("⏹️  Shutting down microservice...");
        // Graceful shutdown
    }
}

// Extension method for easy registration
public static class BootstrapExtensions
{
    public static async Task InitializeMicroserviceAsync(this IServiceProvider services)
    {
        var bootstrap = services.GetRequiredService<IBootstrapService>();
        await bootstrap.InitializeAsync();
    }
}
```

### Bootstrap Checklist

```csharp
// When microservice starts, verify:
☑️ Database accessible and migrations run
☑️ Message broker (RabbitMQ, Service Bus) accessible
☑️ Configuration loaded (appsettings, Vault, etc.)
☑️ Event handlers registered and listening
☑️ Health checks responding
☑️ Dependencies healthy
☑️ Service registered in discovery (Consul, K8s, etc.)
☑️ Ready to accept requests
```

---

## Microservice Startup Flow

```
1. Program.cs starts
   ↓
2. Services registered (DI Container)
   ├─ Database context
   ├─ Repositories
   ├─ Application services
   ├─ Event bus
   └─ Middleware
   ↓
3. Build application
   ↓
4. Bootstrap initialization
   ├─ Check database → Migrate if needed
   ├─ Verify event bus connection
   ├─ Load configuration
   ├─ Register event handlers
   └─ Seed initial data (if first run)
   ↓
5. Configure HTTP pipeline
   ├─ Logging middleware
   ├─ Error handling
   ├─ CORS
   └─ Routing
   ↓
6. Map endpoints
   ├─ Health checks (/health)
   ├─ Swagger (development only)
   └─ API controllers
   ↓
7. Run application
   ↓
8. Ready to receive requests
   ↓
9. Listen for domain events from event bus
```

---

## CQRS + Event Sourcing for Microservices

### When to Add Event Sourcing

```csharp
Event Sourcing = Storing all changes as immutable events

Use when:
✅ Audit trail essential
✅ Need to replay historical state
✅ Temporal queries needed
✅ Complex business logic
✅ Long-running sagas

Example: Order Service
- CustomerOrderedEvent
- PaymentProcessedEvent
- OrderShippedEvent
- OrderDeliveredEvent
- OrderCancelledEvent
```

### Event Sourcing Pattern

```csharp
// Domain event (immutable)
public class CustomerOrderedEvent : DomainEvent
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public List<OrderItem> Items { get; set; }
    public decimal Total { get; set; }
    public DateTime OrderedAt { get; set; }
}

// Event store repository
public class EventStoreRepository : IEventStoreRepository
{
    public async Task AppendAsync<T>(T aggregateRoot) where T : AggregateRoot
    {
        var events = aggregateRoot.GetUncommittedEvents();
        
        // Store each event immutably
        foreach (var @event in events)
        {
            await _eventStore.AppendAsync(
                aggregateRoot.Id,
                @event,
                aggregateRoot.Version
            );
        }
        
        aggregateRoot.MarkEventsAsCommitted();
    }
    
    public async Task<T> GetByIdAsync<T>(int id) where T : AggregateRoot
    {
        // Replay all events to reconstruct state
        var events = await _eventStore.GetAllAsync(id);
        
        var aggregate = Activator.CreateInstance<T>();
        
        foreach (var @event in events)
        {
            aggregate.ApplyEvent(@event);
        }
        
        return aggregate;
    }
}
```

---

## Production Microservice Template

```csharp
// Template structure
src/
├── OrderService.Domain/           // Business logic, entities
│   ├── Aggregates/Order.cs
│   ├── Events/
│   │   ├── OrderCreatedEvent.cs
│   │   ├── PaymentProcessedEvent.cs
│   │   └── OrderShippedEvent.cs
│   ├── ValueObjects/OrderItem.cs
│   └── Repositories/IOrderRepository.cs
│
├── OrderService.Application/      // Use cases, commands, queries
│   ├── Commands/
│   │   ├── CreateOrderCommand.cs
│   │   └── CreateOrderCommandHandler.cs
│   ├── Queries/
│   │   ├── GetOrderQuery.cs
│   │   └── GetOrderQueryHandler.cs
│   ├── Events/
│   │   └── OrderCreatedEventHandler.cs
│   └── Services/OrderApplicationService.cs
│
├── OrderService.Infrastructure/   // Persistence, event bus
│   ├── Persistence/
│   │   ├── OrderRepository.cs
│   │   └── ApplicationDbContext.cs
│   ├── EventPublishing/
│   │   └── RabbitMqEventBus.cs
│   └── Bootstrap/
│       └── MicroserviceBootstrap.cs
│
└── OrderService.Api/              // HTTP endpoints
    ├── Controllers/OrderController.cs
    ├── Middleware/
    ├── Program.cs
    └── appsettings.json
```

---

## Decision: Simple vs CQRS vs Event Sourcing

```
Start → Service Complexity?

    ├─ Simple (CRUD)
    │  └─ Traditional Layered Architecture
    │     Controllers → Services → Repositories → DB
    │
    ├─ Medium (Complex Logic)
    │  └─ CQRS Pattern
    │     Commands ↔ Handlers ↔ Write DB
    │     Queries ↔ Handlers ↔ Read DB
    │
    └─ High (Audit + Scale)
       └─ CQRS + Event Sourcing
          Commands ↔ Events ↔ Event Store
          Queries ↔ Handlers ↔ Read Model
```

---

## Summary

For microservices:
- ✅ **Simple**: Use traditional layered architecture
- ✅ **Complex Logic**: Add CQRS
- ✅ **Audit Trail Needed**: Add Event Sourcing
- ✅ **Always**: Use bootstrap pattern for initialization
- ✅ **Always**: Consider event-driven communication with other services

See `/docs/decision-tree.md` **Step 6** for detailed CQRS guidance.
