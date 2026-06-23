# CQRS Pattern Guide (Command Query Responsibility Segregation)

CQRS separates read and write operations into different models, optimized for each responsibility.

## Core Concept

Traditional architecture uses the same model for both reads and writes:
```csharp
// Same Order class for:
// - Writing (creating, updating orders)
// - Reading (displaying order lists, order details)
```

CQRS separates them:
```csharp
// Write Model (Normalized)
public class CreateOrderCommand { ... }
public class UpdateOrderCommand { ... }

// Read Model (Denormalized)
public class OrderListItemQuery { ... }
public class OrderDetailQuery { ... }
```

## Architecture Overview

```
User Request
    ↓
┌─────────────────────────────────────────┐
│        API Controllers                   │
└─────────────────────────────────────────┘
    ↓                          ↓
┌──────────────────┐    ┌────────────────────┐
│ WRITE SIDE       │    │ READ SIDE          │
├──────────────────┤    ├────────────────────┤
│ Command Handler  │    │ Query Handler      │
│                  │    │                    │
│ Validates        │    │ Denormalizes data  │
│ Executes business│    │ Optimizes for read │
│ logic            │    │ No business logic  │
│                  │    │                    │
│ Updates          │    │ Reads from         │
│ normalized DB    │    │ read model DB      │
└──────────────────┘    └────────────────────┘
    ↓                          ↑
    └──────────────────────────┘
    Event Stream (Synchronization)
```

## When to Use CQRS

### ✅ Perfect for:
- **Complex domains** with rich business logic
- **Event sourcing** (audit trail important)
- **High read/write imbalance** (many reads, few writes)
- **Multiple read models** needed
- **Independent scaling** (read DB scales differently)
- **Reporting requirements** (complex queries)
- **Domain-Driven Design** projects
- **Microservices** with event-driven communication

### ❌ Avoid for:
- **Simple CRUD** applications
- **Uniform read/write patterns**
- **No event sourcing** needed
- **Small projects**
- **Team unfamiliar** with pattern
- **Simple database** schemas

## Simple CQRS Example

### Step 1: Define Commands and Queries

```csharp
// COMMANDS (Write Operations)
public class CreateOrderCommand
{
    public int CustomerId { get; set; }
    public List<OrderItemCommand> Items { get; set; }
}

public class UpdateOrderStatusCommand
{
    public int OrderId { get; set; }
    public OrderStatus NewStatus { get; set; }
}

// QUERIES (Read Operations)
public class GetOrderListQuery
{
    public int CustomerId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GetOrderDetailQuery
{
    public int OrderId { get; set; }
}
```

### Step 2: Create Read Models

```csharp
// Denormalized read model (optimized for reading)
public class OrderListItemReadModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public string CustomerName { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderDetailReadModel
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItemReadModel> Items { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Step 3: Create Handlers

```csharp
// COMMAND HANDLERS (Write)
public class CreateOrderCommandHandler
{
    private readonly IOrderRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    public async Task<int> HandleAsync(CreateOrderCommand cmd)
    {
        // 1. Validate business rules
        if (cmd.Items.Count == 0)
            throw new DomainException("Order must have items");

        // 2. Create domain entity
        var order = new Order(cmd.CustomerId);
        foreach (var item in cmd.Items)
        {
            order.AddItem(item.ProductId, item.Quantity);
        }

        // 3. Save to normalized database
        var orderId = await _repository.AddAsync(order);

        // 4. Publish event for read model synchronization
        await _eventPublisher.PublishAsync(
            new OrderCreatedEvent { OrderId = orderId, Order = order });

        return orderId;
    }
}

// QUERY HANDLERS (Read)
public class GetOrderListQueryHandler
{
    private readonly IOrderReadModelRepository _readRepo;

    public async Task<List<OrderListItemReadModel>> HandleAsync(GetOrderListQuery query)
    {
        // Directly query denormalized read model (fast!)
        return await _readRepo.GetOrdersByCustomerAsync(
            query.CustomerId,
            query.PageNumber,
            query.PageSize);
    }
}

public class GetOrderDetailQueryHandler
{
    private readonly IOrderReadModelRepository _readRepo;

    public async Task<OrderDetailReadModel> HandleAsync(GetOrderDetailQuery query)
    {
        return await _readRepo.GetOrderDetailAsync(query.OrderId)
            ?? throw new NotFoundException("Order not found");
    }
}
```

### Step 4: Controller Usage

```csharp
[ApiController]
[Route("api/orders")]
public class OrdersController
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly IQueryDispatcher _queryDispatcher;

    [HttpPost]
    public async Task<ActionResult> CreateOrder([FromBody] CreateOrderCommand cmd)
    {
        var orderId = await _commandDispatcher.DispatchAsync(cmd);
        return CreatedAtAction(nameof(GetOrder), new { id = orderId });
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderListItemReadModel>>> ListOrders(
        [FromQuery] int customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetOrderListQuery 
        { 
            CustomerId = customerId, 
            PageNumber = page, 
            PageSize = pageSize 
        };
        var result = await _queryDispatcher.DispatchAsync(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDetailReadModel>> GetOrder(int id)
    {
        var result = await _queryDispatcher.DispatchAsync(
            new GetOrderDetailQuery { OrderId = id });
        return Ok(result);
    }
}
```

## Event-Driven Synchronization

Keep read models in sync with write operations:

```csharp
// When order is created:
1. Write model saves to Order table (normalized)
2. Event published: OrderCreatedEvent
3. Event handler updates OrderReadModel table (denormalized)

// When order status changes:
1. Write model updates Order.Status
2. Event published: OrderStatusChangedEvent
3. Event handler updates OrderReadModel.Status
```

## CQRS with Event Sourcing

```csharp
// Store all events
public class OrderEventStore
{
    // Instead of storing final Order state,
    // store all events:
    // - OrderCreatedEvent
    // - ItemAddedEvent
    // - OrderApprovedEvent
    // - OrderShippedEvent

    // Rebuild Order state by replaying events
    public Order RebuildFromEvents(int orderId)
    {
        var events = GetEventsForOrder(orderId);
        var order = new Order();
        
        foreach (var @event in events)
        {
            order.Apply(@event);
        }
        
        return order;
    }
}
```

## Dispatcher Pattern (MediatR Example)

```csharp
// Using MediatR library
public interface ICommand { }
public interface IQuery<T> { }

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command);
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query);
}

// Usage
var orderId = await mediator.Send(new CreateOrderCommand { ... });
var orders = await mediator.Send(new GetOrderListQuery { ... });
```

## DI Setup

```csharp
// Program.cs
builder.Services
    // Repositories
    .AddScoped<IOrderRepository, OrderRepository>()
    .AddScoped<IOrderReadModelRepository, OrderReadModelRepository>()
    
    // Handlers
    .AddScoped<CreateOrderCommandHandler>()
    .AddScoped<GetOrderListQueryHandler>()
    .AddScoped<GetOrderDetailQueryHandler>()
    
    // Dispatchers
    .AddScoped<ICommandDispatcher, CommandDispatcher>()
    .AddScoped<IQueryDispatcher, QueryDispatcher>()
    
    // Event handlers
    .AddScoped<OrderCreatedEventHandler>()
    .AddScoped<OrderStatusChangedEventHandler>();
```

## CQRS Benefits & Tradeoffs

### ✅ Benefits:
- **Optimized reads** - Denormalized for fast queries
- **Optimized writes** - Normalized for consistency
- **Scalability** - Scale read DB independently
- **Clear separation** - Read logic separate from write
- **Event audit** - Full history of changes
- **Complex reporting** - Easy to build specialized read models

### ❌ Challenges:
- **Complexity** - More code, more patterns
- **Eventual consistency** - Read model lags behind write
- **Learning curve** - Team needs to understand CQRS
- **Synchronization** - Events must sync read models
- **Duplication** - Data stored in both models
- **Testing** - More complex test scenarios

## Simplified CQRS vs Full CQRS

### Simple CQRS
```
Same database, separated models:
- Commands: Normalized schema
- Queries: Denormalized views/tables
- Synchronization: Triggers or manual sync
```

### Full CQRS + Event Sourcing
```
Separate databases:
- Write DB: Event store (append-only)
- Read DB: Denormalized (optimized)
- Synchronization: Event handlers
- Audit: Complete event history
```

## Decision: CQRS or Traditional?

| Aspect | Traditional | Simple CQRS | Full CQRS |
|--------|-----------|-----------|----------|
| **Complexity** | Low | Medium | High |
| **Reads/Writes** | Mixed | Separated | Separated |
| **Database** | One | One | Two |
| **Events** | No | Optional | Yes |
| **Audit Trail** | Limited | Good | Full |
| **Scalability** | Limited | Better | Best |
| **Learning Curve** | Easy | Medium | Hard |

## When to Migrate to CQRS

Start traditional, migrate to CQRS when:
- ✅ Read complexity grows significantly
- ✅ Read/write patterns diverge
- ✅ Need complete audit trail
- ✅ Different scaling needs for reads vs writes
- ✅ Team understands patterns

---

**See Also**:
- [Decision Tree](../decision-tree.md)
- [Onion Architecture](onion-architecture.md)
- [DDD and Events](ddd-guide.md)
