# Dapper vs Entity Framework Core: Comparison Guide

Choose between Dapper and EF Core based on your project needs.

## Quick Comparison

| Factor | Dapper | EF Core |
|--------|--------|---------|
| **Learning Curve** | ⭐⭐⭐ (Easier) | ⭐⭐⭐⭐ (Steeper) |
| **Performance** | ⭐⭐⭐⭐⭐ (Best) | ⭐⭐⭐⭐ (Very good) |
| **Control** | ⭐⭐⭐⭐⭐ (Full) | ⭐⭐⭐ (Limited) |
| **Boilerplate** | ⭐⭐⭐ (Moderate) | ⭐⭐⭐⭐⭐ (Minimal) |
| **Relationships** | ⭐⭐⭐ (Manual) | ⭐⭐⭐⭐⭐ (Automatic) |
| **Change Tracking** | ❌ (None) | ✅ (Built-in) |
| **Migrations** | ❌ (Manual) | ✅ (Automatic) |
| **LINQ Support** | ❌ (No) | ✅ (Full) |
| **Complex Queries** | ⭐⭐⭐⭐⭐ (Excel) | ⭐⭐⭐⭐ (Good) |

## Dapper

### What It Is
A micro-ORM that provides lightweight data mapping. Write SQL, get strongly-typed results.

### Code Example

```csharp
// Simple query
var order = await connection.QuerySingleAsync<Order>(
    "SELECT * FROM Orders WHERE Id = @Id",
    new { Id = orderId });

// Complex query with parameters
var orders = await connection.QueryAsync<Order>(
    @"SELECT o.*, oi.Id, oi.ProductId, oi.Quantity
      FROM Orders o
      LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
      WHERE o.CustomerId = @CustomerId
      ORDER BY o.CreatedAt DESC",
    new { CustomerId = customerId });

// Insert with transaction
using (var transaction = connection.BeginTransaction())
{
    var insertedId = await connection.ExecuteScalarAsync<int>(
        "INSERT INTO Orders (OrderNumber, CustomerId, Total) VALUES (@OrderNumber, @CustomerId, @Total); SELECT SCOPE_IDENTITY();",
        new { OrderNumber = "ORD-001", CustomerId = 1, Total = 100m },
        transaction);
    
    transaction.Commit();
    return insertedId;
}
```

### Pros ✅
- **Blazingly fast** - Minimal overhead, direct SQL mapping
- **Full SQL control** - Use any SQL feature (CTEs, window functions, etc.)
- **Lightweight** - Small package, minimal dependencies
- **Explicit** - What you see is what you get
- **Legacy database friendly** - Works with existing schemas
- **Easy debugging** - SQL is visible, easy to profile
- **No magic** - Predictable performance characteristics

### Cons ❌
- **No LINQ** - Write raw SQL strings (no IntelliSense)
- **No migrations** - Manage schema changes manually
- **No change tracking** - Track updates yourself
- **More boilerplate** - Write more code for common operations
- **Manual relationships** - Handle JOINs and mapping yourself
- **SQL injection risk** - Must use parameters (but Dapper helps)
- **Harder to refactor** - Moving columns requires SQL updates

## Entity Framework Core

### What It Is
Full-featured ORM that maps C# objects to database tables. Use LINQ, get automatic migrations.

### Code Example

```csharp
// Simple query
var order = await context.Orders
    .AsNoTracking()
    .FirstOrDefaultAsync(o => o.Id == orderId);

// Complex query with relationships
var orders = await context.Orders
    .AsNoTracking()
    .Where(o => o.CustomerId == customerId)
    .Include(o => o.Items)
    .OrderByDescending(o => o.CreatedAt)
    .ToListAsync();

// Using LINQ
var highValueOrders = await context.Orders
    .AsNoTracking()
    .Where(o => o.Total > 1000 && o.Status == OrderStatus.Approved)
    .Select(o => new OrderDto
    {
        Id = o.Id,
        Number = o.OrderNumber,
        Items = o.Items.Count
    })
    .ToListAsync();

// Insert with auto-save
var order = new Order { CustomerId = 1, OrderNumber = "ORD-001" };
context.Orders.Add(order);
await context.SaveChangesAsync();  // Auto commit
```

### Pros ✅
- **LINQ support** - Write queries in C#, not SQL strings
- **Automatic migrations** - Schema version controlled in code
- **Change tracking** - Automatic dirty-checking, batched updates
- **Less boilerplate** - Configurations over code
- **Navigation properties** - Automatic relationship loading
- **Lazy loading** - Load related data on demand
- **Built-in validation** - Model validation integrated
- **RAD** - Rapid application development
- **Entity relationships** - Define and use relationships easily

### Cons ❌
- **Slower** - More overhead than Dapper
- **Less control** - Generates SQL you don't see
- **Steeper learning curve** - More concepts (DbContext, tracking, lazy loading)
- **Performance tuning harder** - Opacity makes profiling difficult
- **Eager loading complexity** - Must explicitly Include() related data
- **N+1 query problem** - Easy to accidentally trigger multiple queries
- **Migration complexity** - Large migrations can be slow on big tables
- **Legacy database challenges** - Schema mapping can be complex

## Decision Matrix

### Choose Dapper If:

✅ Performance is critical (microseconds matter)
✅ Working with legacy/complex databases
✅ Team prefers explicit SQL control
✅ API has complex custom queries
✅ Database operations are mostly read-heavy
✅ Batch processing with millions of records
✅ Need to use advanced SQL features
✅ Simple CRUD without relationships
✅ Microservice with minimal data access

### Choose EF Core If:

✅ Domain-driven design (entities with behavior)
✅ Complex entity relationships
✅ Building from scratch (green-field)
✅ Need database migrations in code
✅ Rapid development important
✅ Team comfortable with ORM abstractions
✅ Multiple related aggregates
✅ Application has rich domain logic
✅ Code-first development preferred
✅ Entity change tracking valuable

## Real-World Scenarios

### Scenario 1: Simple REST API (Dapper)
```
Project: Todo microservice
Complexity: Low (CRUD only)
Relationships: Minimal (optional, user)
Performance: Not critical
Choice: Dapper
Reason: Lightweight, fast, simple queries
```

### Scenario 2: Complex Business App (EF Core)
```
Project: E-commerce platform
Complexity: High (orders, items, customers, inventory)
Relationships: Complex (multiple aggregates)
Performance: Moderate requirements
Choice: EF Core
Reason: Relationships, migrations, rapid development
```

### Scenario 3: Real-time Analytics (Dapper)
```
Project: Reports and dashboards
Complexity: High queries, low writes
Relationships: Mostly read operations
Performance: Critical (1000+ queries/min)
Choice: Dapper
Reason: Raw speed, control over queries
```

### Scenario 4: Domain-driven Design (EF Core)
```
Project: Large business system
Complexity: Rich domain logic
Relationships: Complex entity graphs
Performance: Acceptable with optimization
Choice: EF Core
Reason: Supports DDD, relationships, migrations
```

## Hybrid Approach

Use both in the same project:

```csharp
public interface IOrderRepository
{
    // Complex read queries using Dapper
    Task<List<OrderSummaryDto>> GetOrdersByCustomerAsync(int customerId);
    Task<List<SalesReportDto>> GetSalesReportAsync(DateTime startDate, DateTime endDate);
    
    // Write operations using EF Core
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task DeleteAsync(int id);
}

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDbConnection _connection;

    public async Task<List<OrderSummaryDto>> GetOrdersByCustomerAsync(int customerId)
    {
        // Complex query: use Dapper for speed
        const string query = @"
            SELECT o.Id, o.OrderNumber, COUNT(oi.Id) as ItemCount, SUM(oi.UnitPrice * oi.Quantity) as Total
            FROM Orders o
            LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
            WHERE o.CustomerId = @CustomerId
            GROUP BY o.Id, o.OrderNumber
            ORDER BY o.CreatedAt DESC";

        return (await _connection.QueryAsync<OrderSummaryDto>(query, new { CustomerId = customerId }))
            .ToList();
    }

    public async Task AddAsync(Order order)
    {
        // Write operation: use EF Core for simplicity and change tracking
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
    }
}
```

## Migration Path

### From Dapper to EF Core
If your app grows more complex:

1. Create entities matching Dapper models
2. Create DbContext with entity configurations
3. Create initial migration
4. Gradually move queries from Dapper to EF LINQ
5. Remove Dapper repository once migrated

### From EF Core to Dapper
If performance becomes critical:

1. Profile to find slow queries
2. Create Dapper repository for problematic queries
3. Use hybrid approach for hot paths
4. Gradually migrate entire repository if needed

## Performance Comparison (Rough Benchmarks)

```
1000 simple SELECT queries:
├─ Dapper:   ~50ms
├─ EF Core:  ~150ms (with change tracking)
└─ EF Core (AsNoTracking): ~80ms

Complex JOIN with 1M rows:
├─ Dapper (optimized SQL): ~200ms
├─ EF Core (Include): ~500ms
└─ EF Core (manual Include): ~300ms

Single INSERT with relationships:
├─ Dapper:   ~5ms
└─ EF Core:  ~8ms
```

**Real truth**: For most applications, EF Core performance is sufficient. Only optimize if profiling shows it's the bottleneck.

## Recommendation by Project Size

### Startup/MVP (Small)
**Choose**: Dapper
- Fast development
- Minimal complexity
- Performance not critical yet

### Small-Medium (Growing)
**Choose**: EF Core
- Growing complexity
- Multiple developers
- Migrations valuable
- Performance still acceptable

### Large Enterprise
**Choose**: Hybrid (Both)
- Critical performance paths: Dapper
- Complex business logic: EF Core
- Best of both worlds

### Monolith with Many Services
**Choose**: Dapper per service
- Each service small scope
- Performance critical at scale
- Independent deployment

## Conclusion

| **EF Core** wins for: Domain-driven design, complexity, migrations, team velocity
| **Dapper** wins for: Performance, control, simple CRUD, legacy databases
| **Both** wins for: Complex projects needing both abstraction and control

**Most projects should start with EF Core** and optimize to Dapper only if profiling shows it's necessary.

---

**See Also**:
- [Dapper Repository Template](../templates/shared/repositories/dapper-repository.template.cs)
- [EF Core Repository Template](../templates/shared/repositories/ef-repository.template.cs)
- [EF Core Migrations Guide](ef-migrations.md)
