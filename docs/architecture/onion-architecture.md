# Onion Architecture for C# APIs

Onion Architecture is a layered architectural pattern that organizes code in concentric circles, with dependencies pointing inward toward the domain core.

## Architecture Diagram

```
                    ┌─────────────────────────────┐
                    │   Presentation Layer        │
                    │  (Controllers, API, DTOs)   │
                    │  Depends on: Application    │
                    └──────────────┬──────────────┘
                                   │
                    ┌──────────────┴──────────────┐
                    │  Application Layer          │
                    │ (Services, UseCases, Specs) │
                    │ Depends on: Domain          │
                    └──────────────┬──────────────┘
                                   │
                    ┌──────────────┴──────────────┐
                    │  Infrastructure Layer       │
                    │ (Repositories, DbContext)   │
                    │ Depends on: Application     │
                    └──────────────┬──────────────┘
                                   │
                    ┌──────────────┴──────────────┐
                    │    Domain Layer (Core)      │
                    │ (Entities, Value Objects)   │
                    │ Depends on: NOTHING         │
                    └─────────────────────────────┘

    Dependency Flow: Presentation → Application → Infrastructure → Domain
                                         ↘
                                          → Domain (both direct)
```

## The Four Layers

### 1. Domain Layer (Core)
**Location**: `src/Domain/`

The innermost circle. Contains pure business logic with **zero framework dependencies**.

**What Lives Here**:
```csharp
// Entities - Domain objects with behavior
public class Order : Entity
{
    public string OrderNumber { get; private set; }
    public List<OrderItem> Items { get; private set; }
    public OrderStatus Status { get; private set; }
    
    public void Approve() // Domain behavior
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Can only approve pending orders");
        Status = OrderStatus.Approved;
    }
}

// Value Objects - Immutable objects representing concepts
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative");
        Amount = amount;
        Currency = currency;
    }
}

// Domain Services - Pure business logic (when not suitable for entities)
public interface IOrderCalculationService
{
    Money CalculateTotalPrice(List<OrderItem> items);
}

// Domain Events - Events that happened in the domain
public class OrderApprovedDomainEvent : DomainEvent
{
    public int OrderId { get; set; }
    public DateTime ApprovedAt { get; set; }
}

// Exceptions - Domain-specific exceptions
public class InsufficientInventoryException : DomainException
{
    public InsufficientInventoryException(string message) : base(message) { }
}

// Enums - Domain enums
public enum OrderStatus
{
    Pending,
    Approved,
    Shipped,
    Delivered,
    Cancelled
}
```

**Key Characteristics**:
- ✅ No dependencies on other layers
- ✅ No NuGet packages (except testing)
- ✅ Pure C# classes
- ✅ Database-agnostic (no DbContext, no ORM)
- ✅ Framework-agnostic (no ASP.NET Core, no DI)
- ✅ Rich domain objects with behavior
- ✅ Encapsulates business rules

**Rules**:
- 🚫 Never reference any `using` from other projects
- 🚫 Never use `DbContext`, `IRepository`, or interfaces
- 🚫 Never use configuration or dependency injection
- ✅ Use concrete classes for domain logic
- ✅ Protect invariants with private setters and validation

---

### 2. Application Layer
**Location**: `src/Application/`

Orchestrates domain logic and coordinates use cases. Depends on Domain layer only.

**What Lives Here**:
```csharp
// DTOs - Data Transfer Objects (API contracts)
public class CreateOrderDto
{
    public int CustomerId { get; set; }
    public List<CreateOrderItemDto> Items { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItemDto> Items { get; set; }
}

// Application Services - Use case orchestration
public class CreateOrderApplicationService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderCalculationService _calculationService;
    
    public CreateOrderApplicationService(
        IOrderRepository orderRepository,
        IOrderCalculationService calculationService)
    {
        _orderRepository = orderRepository;
        _calculationService = calculationService;
    }
    
    public async Task<OrderDto> ExecuteAsync(CreateOrderDto command)
    {
        // 1. Create domain entity
        var order = new Order(command.CustomerId);
        
        // 2. Add items to order
        foreach (var item in command.Items)
        {
            order.AddItem(item.ProductId, item.Quantity);
        }
        
        // 3. Calculate total using domain service
        var total = _calculationService.CalculateTotalPrice(order.Items);
        
        // 4. Persist using repository
        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();
        
        // 5. Return DTO
        return MapToDto(order);
    }
}

// Specifications - Complex query logic (Query Objects)
public class ActiveOrdersSpecification : Specification<Order>
{
    public ActiveOrdersSpecification(int customerId)
    {
        AddCriteria(o => o.CustomerId == customerId && 
                        o.Status != OrderStatus.Cancelled);
        AddInclude(o => o.Items);
        AddOrderByDescending(o => o.CreatedAt);
    }
}

// Interfaces - Contracts for infrastructure layer
public interface IOrderRepository
{
    Task<Order> GetByIdAsync(int id);
    Task<List<Order>> FindAsync(Specification<Order> spec);
    Task AddAsync(Order order);
    Task SaveChangesAsync();
}

// Queries/Commands - CQRS pattern (optional)
public class GetOrderByIdQuery : IQuery<OrderDto>
{
    public int OrderId { get; set; }
}

public class ApproveOrderCommand : ICommand
{
    public int OrderId { get; set; }
}

// Validators - Input validation
public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
    }
}
```

**Key Characteristics**:
- ✅ Depends only on Domain layer
- ✅ Coordinates domain objects
- ✅ Orchestrates use cases
- ✅ Contains DTOs for external communication
- ✅ Defines interfaces (which infrastructure implements)
- ✅ Independent of delivery mechanism (HTTP, GraphQL, etc.)
- ✅ Uses specifications for complex queries

**Rules**:
- 🚫 Never reference Infrastructure or Presentation directly
- 🚫 Never use `DbContext` or concrete repositories
- 🚫 Never handle HTTP concerns (HttpContext, Controllers)
- ✅ Use interfaces from this layer (Dependency Inversion)
- ✅ Map domain objects to DTOs here
- ✅ Validate input at service boundaries

---

### 3. Infrastructure Layer
**Location**: `src/Infrastructure/`

Implements persistence, external APIs, and technical details. Depends on Application and Domain.

**What Lives Here**:
```csharp
// Repository Implementations - Data access abstractions
public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;
    
    public OrderRepository(OrderDbContext context)
    {
        _context = context;
    }
    
    public async Task<Order> GetByIdAsync(int id)
    {
        return await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);
    }
    
    public async Task<List<Order>> FindAsync(Specification<Order> spec)
    {
        var query = ApplySpecification(spec);
        return await query.ToListAsync();
    }
    
    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
    }
    
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

// DbContext - EF Core configuration
public class OrderDbContext : DbContext
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Customer> Customers { get; set; }
    
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

// EF Core Configuration - Entity mappings
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
        builder.HasMany(o => o.Items).WithOne(i => i.Order);
        builder.HasOne(o => o.Customer).WithMany(c => c.Orders);
    }
}

// External Service Implementations
public class PaymentServiceClient : IPaymentService
{
    private readonly HttpClient _httpClient;
    
    public PaymentServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<PaymentResult> ProcessPaymentAsync(int orderId, Money amount)
    {
        var request = new ProcessPaymentRequest { OrderId = orderId, Amount = amount };
        var response = await _httpClient.PostAsJsonAsync("/api/payments", request);
        return response.IsSuccessStatusCode 
            ? await response.Content.ReadAsAsync<PaymentResult>()
            : throw new PaymentException("Payment failed");
    }
}

// Unit of Work Pattern (if needed)
public class UnitOfWork : IUnitOfWork
{
    private readonly OrderDbContext _context;
    
    public IOrderRepository Orders { get; }
    public ICustomerRepository Customers { get; }
    
    public UnitOfWork(OrderDbContext context, 
        IOrderRepository orders, 
        ICustomerRepository customers)
    {
        _context = context;
        Orders = orders;
        Customers = customers;
    }
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
```

**Key Characteristics**:
- ✅ Implements interfaces defined in Application layer
- ✅ Contains all database-specific code
- ✅ Handles external API integrations
- ✅ Contains DbContext configuration
- ✅ Manages persistence details
- ✅ Can be tested with real or fake databases
- ✅ Framework-specific code isolated here

**Rules**:
- 🚫 Never expose DbContext to Application layer
- 🚫 Only implement interfaces from Application layer
- 🚫 Never handle HTTP concerns
- ✅ Keep repositories simple (CRUD + Specifications)
- ✅ Use dependency injection from Application/Presentation
- ✅ Map database entities to domain entities

---

### 4. Presentation Layer
**Location**: `src/API/` (or `src/Web/`, `src/Console/`)

Handles user interaction. Depends on all inner layers.

**What Lives Here**:
```csharp
// Controllers - HTTP endpoints
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly CreateOrderApplicationService _createService;
    private readonly GetOrderApplicationService _getService;
    
    public OrdersController(
        CreateOrderApplicationService createService,
        GetOrderApplicationService getService)
    {
        _createService = createService;
        _getService = getService;
    }
    
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto dto)
    {
        try
        {
            var result = await _createService.ExecuteAsync(dto);
            return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var result = await _getService.ExecuteAsync(id);
        return result == null ? NotFound() : Ok(result);
    }
}

// Middleware - Cross-cutting concerns
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception occurred");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "Internal server error" });
        }
    }
}

// Dependency Injection Configuration
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<CreateOrderApplicationService>();
        services.AddScoped<GetOrderApplicationService>();
        return services;
    }
    
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderCalculationService, OrderCalculationService>();
        
        return services;
    }
}

// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplicationServices()
    .AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapControllers();
app.Run();
```

**Key Characteristics**:
- ✅ Depends on all inner layers
- ✅ Handles HTTP concerns (Controllers, Middleware)
- ✅ Configures dependency injection
- ✅ Framework-specific (ASP.NET Core)
- ✅ Controllers are thin (orchestrate only)
- ✅ Can have multiple implementations (REST, GraphQL, etc.)
- ✅ Handles response formatting

**Rules**:
- 🚫 Never contain business logic
- 🚫 Keep controllers thin - delegate to Application services
- 🚫 Never directly use DbContext or repositories
- ✅ Use application services only
- ✅ Map requests to DTOs
- ✅ Return appropriate HTTP status codes
- ✅ Handle cross-cutting concerns here (logging, auth, etc.)

---

## Dependency Flow

**The Golden Rule: Dependencies always point inward toward the Domain.**

```csharp
// ✅ CORRECT - Dependency on abstraction in Application
public class OrderRepository : IOrderRepository // Interface from Application
{
    public async Task<Order> GetByIdAsync(int id) // Domain entity
    {
        // Infrastructure implementation
    }
}

// ✅ CORRECT - Application orchestrates Domain
public class CreateOrderService
{
    public async Task<OrderDto> ExecuteAsync(CreateOrderDto dto) // Application DTO
    {
        var order = new Order(...); // Domain entity
        await _repository.SaveAsync(order);
        return MapToDto(order);
    }
}

// 🚫 WRONG - Presentation directly uses Domain
[HttpPost]
public ActionResult CreateOrder(Order order) // Never expose domain entities
{
    // ...
}

// 🚫 WRONG - Application references Infrastructure directly
public class OrderService
{
    private OrderRepository _repo; // Concrete, not interface
    // Should use IOrderRepository interface instead
}

// 🚫 WRONG - Domain references Infrastructure
public class Order : Entity
{
    private readonly DbContext _context; // NEVER!
}
```

---

## Project Structure

```
Solution/
├── src/
│   ├── Domain/                              # Layer 1
│   │   ├── Entities/
│   │   │   ├── Order.cs
│   │   │   ├── OrderItem.cs
│   │   │   └── Customer.cs
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs
│   │   │   └── Address.cs
│   │   ├── DomainServices/
│   │   │   └── IOrderCalculationService.cs
│   │   ├── DomainEvents/
│   │   │   └── OrderApprovedDomainEvent.cs
│   │   ├── Exceptions/
│   │   │   └── InsufficientInventoryException.cs
│   │   └── Enums/
│   │       └── OrderStatus.cs
│   │
│   ├── Application/                        # Layer 2
│   │   ├── Dtos/
│   │   │   ├── CreateOrderDto.cs
│   │   │   └── OrderDto.cs
│   │   ├── Services/
│   │   │   ├── CreateOrderService.cs
│   │   │   └── GetOrderService.cs
│   │   ├── Specifications/
│   │   │   └── ActiveOrdersSpecification.cs
│   │   ├── Interfaces/
│   │   │   ├── IOrderRepository.cs
│   │   │   ├── IOrderCalculationService.cs
│   │   │   └── IPaymentService.cs
│   │   ├── Validators/
│   │   │   └── CreateOrderDtoValidator.cs
│   │   └── Mappings/
│   │       └── OrderMappingProfile.cs
│   │
│   ├── Infrastructure/                     # Layer 3
│   │   ├── Data/
│   │   │   ├── OrderDbContext.cs
│   │   │   └── Configurations/
│   │   │       └── OrderConfiguration.cs
│   │   ├── Repositories/
│   │   │   └── OrderRepository.cs
│   │   ├── Services/
│   │   │   ├── OrderCalculationService.cs
│   │   │   └── PaymentServiceClient.cs
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   │
│   └── API/                                 # Layer 4
│       ├── Controllers/
│       │   └── OrdersController.cs
│       ├── Middleware/
│       │   ├── GlobalExceptionMiddleware.cs
│       │   └── LoggingMiddleware.cs
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── Domain.Tests/
    ├── Application.Tests/
    ├── Infrastructure.Tests/
    └── API.Tests/
```

---

## Testing Each Layer

### Domain Layer Tests (Pure Logic)
```csharp
[Fact]
public void Order_WhenApproved_ChangesStatusToApproved()
{
    // Arrange
    var order = new Order(customerId: 1);
    
    // Act
    order.Approve();
    
    // Assert
    Assert.Equal(OrderStatus.Approved, order.Status);
}
```

### Application Layer Tests (Service Logic)
```csharp
[Fact]
public async Task CreateOrder_WithValidData_ReturnsOrderDto()
{
    // Arrange
    var mockRepository = new Mock<IOrderRepository>();
    var service = new CreateOrderService(mockRepository.Object);
    var dto = new CreateOrderDto { CustomerId = 1, Items = new List<CreateOrderItemDto>() };
    
    // Act
    var result = await service.ExecuteAsync(dto);
    
    // Assert
    Assert.NotNull(result);
    mockRepository.Verify(r => r.SaveAsync(It.IsAny<Order>()), Times.Once);
}
```

### Infrastructure Tests (Data Access)
```csharp
[Fact]
public async Task OrderRepository_SaveOrder_PersistsToDatabase()
{
    // Arrange
    var options = new DbContextOptionsBuilder<OrderDbContext>()
        .UseInMemoryDatabase("test")
        .Options;
    var context = new OrderDbContext(options);
    var repository = new OrderRepository(context);
    var order = new Order(customerId: 1);
    
    // Act
    await repository.AddAsync(order);
    await repository.SaveChangesAsync();
    
    // Assert
    var saved = await context.Orders.FirstOrDefaultAsync();
    Assert.NotNull(saved);
}
```

### Presentation Tests (API Endpoints)
```csharp
[Fact]
public async Task PostOrder_WithValidData_Returns201Created()
{
    // Arrange
    var client = new HttpClient { BaseAddress = new Uri("http://localhost") };
    var dto = new CreateOrderDto { CustomerId = 1, Items = new List<CreateOrderItemDto>() };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/orders", dto);
    
    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
}
```

---

## Benefits of Onion Architecture

✅ **Clear Separation of Concerns** - Each layer has specific responsibility
✅ **Testability** - Layers can be tested in isolation
✅ **Flexibility** - Easy to swap implementations (Dapper ↔ EF)
✅ **Maintainability** - Changes localized to appropriate layer
✅ **Domain-Centric** - Business logic not scattered in framework code
✅ **Framework Independence** - Core logic works with any framework
✅ **Scalability** - Easy to extend with new features

---

## Common Mistakes to Avoid

🚫 **Adding Framework References to Domain** - Domain must be pure
🚫 **Bypassing Application Layer** - Controllers should use services
🚫 **Circular Dependencies** - Always check dependency direction
🚫 **Leaking Abstractions** - Don't expose repositories as IQueryable
🚫 **Mixing DTOs with Domain Entities** - Keep them separate
🚫 **Domain Logic in Repositories** - Use domain services
🚫 **Infrastructure Details in Application** - Apply Dependency Inversion

---

**Next**: Read about [SOLID Principles](solid-principles.md) for implementation details.
