# C# API Project Decision Tree

Use this interactive guide to determine the best architecture and API style for your project.

## Step 1: Project Type & Scope

**Question: What is the primary purpose and scope of this project?**

### A) Microservice
✅ **Choose if:**
- Building a single, focused business capability
- Part of a larger microservices ecosystem
- Will have its own database
- Needs independent deployment
- Example: Order Service, User Service, Payment Service

→ **Templates**: `/templates/*/microservice/`
→ **Examples**: `examples/*/[service]-microservice/`

### B) Full REST API
✅ **Choose if:**
- Complete, standalone business application
- Handles multiple interconnected domains
- May have complex relationships between entities
- All in one deployable unit
- Example: E-commerce API, CMS, Project Management System

→ **Templates**: `/templates/*/full-api/`
→ **Examples**: `examples/*/[business]-full-api/`

### C) Standalone Service
✅ **Choose if:**
- Single responsibility, limited scope
- Often background/timer-based work
- Handles notifications, reports, batch processing
- May be event-driven
- Example: Email Service, Report Generator, Cleanup Service

→ **Templates**: `/templates/*/standalone/`
→ **Examples**: `examples/*/[service]-service/`

---

## Step 2: API Communication Style

**Question: How will clients interact with your API?**

### A) REST (Action-Based)
```csharp
POST /api/orders/123/approve
POST /api/orders/123/cancel
GET /api/reports/generate?startDate=2024-01-01
DELETE /api/cache/clear
```

✅ **Choose if:**
- Intuitive, action-oriented API
- Non-standard operations (approve, generate, etc.)
- Operations don't map to standard CRUD
- Rapid development prioritized over standards
- **Drawback**: Not idempotent, ignores HTTP semantics

→ **Read**: `/docs/api-styles/rest-guide.md`
→ **Templates**: `/templates/rest-api/`

### B) RESTful (Resource-Based, HTTP Semantics)
```csharp
GET    /api/orders          // List orders
POST   /api/orders          // Create order
GET    /api/orders/123      // Get specific order
PUT    /api/orders/123      // Update order
DELETE /api/orders/123      // Delete order
PATCH  /api/orders/123      // Partial update
```

✅ **Choose if:**
- Standard CRUD operations
- Want proper HTTP method usage
- Need caching and browser compatibility
- RESTful API best practices preferred
- **Advantage**: Standard, composable, widely understood

→ **Read**: `/docs/api-styles/restful-guide.md`
→ **Templates**: `/templates/restful-api/`

### C) GraphQL (Query Language)
```graphql
query GetOrderWithItems($id: ID!) {
  order(id: $id) {
    id
    total
    customer { name email }
    items { product quantity }
  }
}
```

✅ **Choose if:**
- Clients need flexible data shapes
- Multiple client types with different data needs
- Want to reduce over-fetching
- Frontend-driven API development
- **Advantage**: Flexible, efficient, great developer experience

→ **Read**: `/docs/api-styles/graphql-guide.md`
→ **Templates**: `/templates/graphql-api/`

---

## Step 3: Real-Time Communication

**Question: Do clients need real-time, bidirectional communication?**

### A) HTTP Only
- Standard request/response model
- Perfect for REST/RESTful APIs
- Clients poll for updates if needed
- Simpler infrastructure

→ **Standard HTTP setup in templates**

### B) WebSockets (Real-Time)
- Server can push updates to clients
- Persistent connection
- Perfect for:
  - Live notifications
  - Real-time dashboards
  - Collaborative features
  - Chat applications

→ **Add WebSocket middleware** (SignalR recommended)
→ **Include in**: `/templates/shared/middleware/websocket/`

### C) Both HTTP and WebSockets
- REST/RESTful endpoints + WebSocket connections
- Hybrid approach: request/response + streaming
- Most flexible

→ **Combine both patterns**
→ **See**: `/templates/shared/middleware/`

---

## Step 4: Database Type

**Question: What type of database will you use?**

### A) Server-Based SQL Database (SQL Server, PostgreSQL, MySQL)
✅ **Choose if:**
- Relational data with complex relationships
- ACID transactions required
- Multi-user concurrent access (10+ concurrent users)
- Need migrations and schema management
- Traditional enterprise application
- Scalability to millions of records
- Separate database server infrastructure

**Supports**: EF Core or Dapper ORM
**Requires**: DbContext configuration, Entity mappings, Migrations
**Best for**: Enterprise apps, complex business logic, multi-user systems

→ **Read**: `/docs/orm-guide/dapper-vs-ef-comparison.md`
→ **Templates**: `/templates/shared/repositories/`
→ **Next**: Step 5 (CQRS choice)

### B) SQLite (Embedded SQL Database)
✅ **Choose if:**
- Single-file SQL database (like LiteDB but with SQL)
- Embedded/no separate server needed
- Relational schema with SQL support
- Limited concurrent users (1-5)
- Desktop, mobile, or small microservice
- Local development or offline-first app
- Want SQL features with embedded simplicity

**Supports**: EF Core (best) or Dapper
**Uses**: SQLite connection string
**Migrations**: Full EF Core migrations support
**Best for**: Desktop apps, mobile, embedded scenarios, small services with SQL needs

→ **Read**: `/docs/orm-guide/sqlite-guide.md`
→ **Templates**: `/templates/shared/repositories/`
→ **Next**: Step 5 (CQRS choice)

### C) LiteDB (Embedded NoSQL/Document Database)
✅ **Choose if:**
- Single-file embedded database
- Document-oriented data model (not relational)
- Simple to moderate data needs
- Desktop or small service application
- No separate database server needed
- Quick prototyping or local development
- Flexible schema (schema-less)

**Does NOT use**: DbContext or traditional ORM
**Uses**: ILiteDatabase interface
**No migrations**: Schema-less (flexible)
**Best for**: Rapid development, NoSQL scenarios, embedded, small projects

→ **Read**: `/docs/orm-guide/litedb-guide.md`
→ **Templates**: `/templates/shared/repositories/litedb-repository.template.cs`
→ **Next**: Skip Step 5 (CQRS), go to Step 6

### D) Hybrid (Multiple Databases)
✅ **Choose if:**
- Main app in server SQL (PostgreSQL)
- Local cache in SQLite or LiteDB
- Event store separate
- Different DBs for different services (microservices)

**Example**: EF Core + PostgreSQL for cloud + SQLite for local sync + LiteDB for cache

---

## Step 5: CQRS Pattern (For SQL Databases Only)

**Question: Will you use CQRS (Command Query Responsibility Segregation)?**

### A) Traditional Architecture (No CQRS)
✅ **Choose if:**
- Simple CRUD operations (Create, Read, Update, Delete)
- Same models for reading and writing
- Monolithic service
- Quick prototyping
- Simple to moderate complexity
- Team unfamiliar with CQRS

**Structure:**
```
Controllers → Services → Repositories → Single Database
(One model for read and write)
```

**Best for**: CRUD APIs, simple microservices, learning projects

→ **Continue with Step 6**

### B) CQRS Pattern (Separated Read/Write)
✅ **Choose if:**
- Complex business logic (many validations, rules)
- Heavy read operations vs writes (different optimization)
- Need event sourcing
- Multiple read models needed
- Different schemas for read/write beneficial
- Scaling reads independently

**Structure:**
```
Write Side (Commands):              Read Side (Queries):
Controllers → Commands             Controllers → Queries
    ↓                                   ↓
Services → Repositories             Services → Read Models
    ↓                                   ↓
Write Database ←→ Event Stream ←→ Read Database
(Normalized)          (Audit)       (Denormalized)
```

**Example:**
```csharp
// Command: Create order (normalized write model)
public class CreateOrderCommand { ... }

// Query: Get order summary (denormalized read model)
public class GetOrderSummaryQuery { ... }

// Separate handlers
public class CreateOrderCommandHandler { ... }
public class GetOrderSummaryQueryHandler { ... }
```

**Best for**: Complex domains, DDD (Domain-Driven Design), event sourcing, complex reporting

→ **Read**: `/docs/architecture/cqrs-guide.md`
→ **Templates**: `/templates/shared/cqrs/`

---

## Step 6: Data Persistence Strategy (For SQL Databases)

**Question: How will you handle data persistence with SQL?**

### A) Dapper (Lightweight, Full Control)
```csharp
var user = await _connection.QuerySingleAsync<User>(
    "SELECT * FROM Users WHERE Id = @Id", 
    new { Id = userId }
);
```

✅ **Choose Dapper if:**
- Need maximum control over SQL
- Performance is critical (micro-optimizations)
- Complex queries with specific requirements
- Prefer explicit data access
- Working with legacy databases
- Team prefers SQL

→ **Read**: `/docs/orm-guide/dapper-comparison.md`
→ **Template**: `/templates/shared/repositories/dapper-repository.template.cs`
→ **Example**: Study repository implementations

### B) Entity Framework Code-First
```csharp
var user = await _context.Users
    .Include(u => u.Orders)
    .FirstOrDefaultAsync(u => u.Id == userId);
```

✅ **Choose EF if:**
- Domain-first approach preferred
- Complex entity relationships
- Want LINQ query syntax
- Change tracking and lazy loading valuable
- Entity migrations important
- Team prefers ORM abstraction

→ **Read**: `/docs/orm-guide/ef-comparison.md`
→ **Template**: `/templates/shared/repositories/ef-repository.template.cs`
→ **Example**: Study DbContext patterns

### Decision Matrix

| Criteria | Dapper | EF Code-First |
|----------|--------|---------------|
| **Performance** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Control** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Boilerplate** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Learning Curve** | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Complex Queries** | ⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Relationships** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Maintainability** | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |

---

## Step 7: Resilience & Error Handling

**Question: Does your API need resilience patterns for distributed scenarios?**

### A) Basic Error Handling
- Try-catch, validation
- Global exception middleware
- No external dependency handling
- Single-server, no external calls

→ **Standard error handling in templates**

### B) Polly for Resilience
```csharp
var policy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .Retry(3, onRetry: LogRetry)
    .Wrap(circuitBreaker);

await policy.ExecuteAsync(() => _httpClient.GetAsync(url));
```

✅ **Choose Polly if:**
- Calling external APIs/microservices
- Network reliability concerns
- Need retry logic with backoff
- Microservices architecture
- Want circuit breaker pattern
- Graceful degradation needed

→ **Read**: `/docs/resilience/polly-patterns.md`
→ **Template**: `/templates/shared/resilience/polly-setup.template.cs`
→ **Patterns**: Retry, Circuit Breaker, Timeout, Fallback, Bulkhead

---

## Step 8: Caching Strategy

**Question: Do you need caching for performance and quick access?**

### A) No Caching
✅ **Choose if:**
- Simple applications (CRUD only)
- Real-time data always required
- Low traffic (< 100 req/min)
- Learning/prototype projects
- Small dataset
- Database is already fast enough

**Impact**: Slower response times, more DB load

### B) In-Memory Cache (IMemoryCache)
```csharp
// Store in process memory
_cache.Set("users:list", users, TimeSpan.FromMinutes(5));
var cached = _cache.Get("users:list");
```

✅ **Choose if:**
- **Single server** deployment only
- Moderate data size (< 500MB)
- Acceptable to lose cache on app restart
- Cache invalidation is simple
- Lower infrastructure costs
- Fast access within same process

**Pros**: ⚡ Fastest (in-process), no network latency, no additional infrastructure
**Cons**: ⚠️ Lost on restart, not shared across servers, high memory usage

**Best for**: Desktop apps, single-server microservices, development

→ **Read**: `/docs/performance/caching-strategies.md`
→ **Template**: `/templates/shared/caching/in-memory-cache.template.cs`

**Setup**:
```bash
dotnet add package Microsoft.Extensions.Caching.Memory
```

### C) Redis (Distributed Cache)
```csharp
// Shared cache across servers
var cached = await _distributedCache.GetAsync("users:list");
await _distributedCache.SetAsync("users:list", data, 
    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
```

✅ **Choose if:**
- **Multiple servers** (load balanced)
- **Microservices** sharing cache
- Cache must **survive app restart**
- **High traffic** (> 1000 req/min)
- Performance critical (read-heavy)
- Different services need same cache

**Pros**: 📦 Persistent, shared across servers/services, handles high load, survives restarts
**Cons**: ⚠️ Extra infrastructure (Redis server), network latency, complexity

**Best for**: Cloud apps, microservices, high-traffic APIs, multi-server deployments

→ **Read**: `/docs/performance/caching-strategies.md`
→ **Template**: `/templates/shared/caching/redis-cache.template.cs`

**Setup**:
```bash
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

### D) Hybrid (Both In-Memory + Redis)
```
Request
  ↓
Check In-Memory Cache (fast!)
  ├─ Hit: Return immediately ⚡
  └─ Miss: Check Redis (persistent shared cache)
       ├─ Hit: Load into memory, return ⚡
       └─ Miss: Query database, cache in both levels
```

✅ **Choose if:**
- **Maximum performance** needed
- **Multiple servers** with high traffic
- Want **best of both worlds**: speed + consistency
- Complex caching strategy (different TTLs for different data)

**Pros**: ⚡ Fastest (memory), 📦 Persistent (Redis), 🎯 Scalable
**Cons**: ⚠️ Most complex, needs cache invalidation strategy

**Best for**: High-performance cloud APIs, critical systems, large-scale microservices

→ **Advanced pattern in examples**

---

## Caching Comparison Matrix

| Factor | None | In-Memory | Redis | Hybrid |
|--------|------|-----------|-------|--------|
| **Speed** | Slow | ⚡⚡⚡ Fastest | ⚡⚡ Fast | ⚡⚡⚡ Fastest |
| **Persistence** | N/A | ❌ Lost on restart | ✅ Persistent | ✅ Persistent |
| **Multi-Server** | ❌ | ❌ Separate caches | ✅ Shared | ✅ Shared |
| **Infrastructure** | DB only | App memory | Redis server | App + Redis |
| **Complexity** | 🟢 Simple | 🟡 Medium | 🟡 Medium | 🔴 Complex |
| **Memory Usage** | None | 🔴 High (in-process) | 🟡 On Redis server | 🔴 High |
| **Best for** | Simple apps | Single-server | Multi-server | High-performance |
| **Cost** | Low | Low | Medium | Medium |

---

## When to Add Caching

**Start with NO caching**, then add if:
1. ✅ Database queries are slow (profile first!)
2. ✅ Same data requested repeatedly
3. ✅ Data changes infrequently
4. ✅ Traffic exceeds database capacity
5. ✅ Response time targets not met

**Don't cache if:**
- ❌ Data changes frequently (cache invalidation nightmare)
- ❌ Database is already fast enough
- ❌ Real-time accuracy critical
- ❌ Adding complexity not worth the gain

---

## Step 9: Serialization & Protocol

**Question: How will data be serialized over the wire?**

### A) JSON (Default)
```json
{ "id": 1, "name": "John", "email": "john@example.com" }
```

✅ **Default for:**
- REST/RESTful APIs
- GraphQL
- Web clients
- Human-readable
- Browser compatibility

→ **Included in all templates**

### B) Protocol Buffers (Protobuf)
```protobuf
message User {
  int32 id = 1;
  string name = 2;
  string email = 3;
}
```

✅ **Choose if:**
- Microservice-to-microservice communication
- Bandwidth optimization important
- High-frequency data transfer
- Performance critical
- Smaller payload size needed

→ **Read**: `/docs/communication/protobuf-guide.md`
→ **Setup**: Separate from REST/GraphQL

---

## Step 10: Testing Strategy

**Question: How will you verify correctness?**

All templates include **xUnit with TDD** (Test-Driven Development).

### Test Types Included

```csharp
// Unit Test - Single class in isolation
[Fact]
public async Task CreateUser_WithValidData_ReturnsUserDto()

// Integration Test - Component interactions
[Fact]
public async Task CreateUser_WithDatabase_PersistsCorrectly()

// API Test - HTTP endpoint
[Fact]
public async Task PostUser_WithValidPayload_Returns201Created()

// Theory - Parameterized test
[Theory]
[InlineData("")]
[InlineData(null)]
public void CreateUser_WithInvalidEmail_ThrowsException(string email)
```

→ **Read**: `/docs/tdd-xunit/testing-patterns.md`
→ **Templates**: `/templates/shared/tests/`
→ **Examples**: Study test files in examples

---

## Step 11: Your Complete Path

### Path Summary

Based on your answers, follow this path:

1. **Read**: `/docs/architecture/onion-architecture.md` (all projects)
2. **Read**: API style guide (`/docs/api-styles/{your-style}/`)
3. **Copy**: Template from `/templates/{your-style}/{your-type}/`
4. **Study**: Example from `/examples/{your-style}/{your-type}/`
5. **Setup**: ORM from `/templates/shared/repositories/`
6. **Add**: Resilience if needed (`/templates/shared/resilience/`)
7. **Add**: Caching if needed (`/templates/shared/caching/`)
8. **Setup**: Tests with `/templates/shared/tests/`
9. **Verify**: Run checklist from `/checklists/architecture-audit.md`

---

## Quick Reference: Decision Matrix

| Question | Choice A | Choice B | Choice C |
|----------|----------|----------|----------|
| **Project Type** | Microservice | Full REST API | Standalone |
| **API Style** | REST | RESTful | GraphQL |
| **Real-Time** | HTTP only | WebSockets | Both |
| **ORM** | Dapper | EF Code-First | — |
| **Resilience** | Basic | Polly | — |
| **Cache** | None | In-Memory | Distributed |
| **Protocol** | JSON | Protobuf | — |

---

## Common Project Setups

### Setup 1: Microservice REST API with Dapper
```
Template: /templates/rest-api/microservice/
ORM: Dapper
Resilience: Polly
Cache: In-Memory (optional)
Testing: xUnit
→ Example: examples/rest-api/todo-microservice/
```

### Setup 2: Full E-Commerce RESTful API with EF
```
Template: /templates/restful-api/full-api/
ORM: EF Code-First
Resilience: Polly (external services)
Cache: Distributed (Redis)
WebSockets: For order updates
Testing: xUnit
→ Example: examples/restful-api/ecommerce-full-api/
```

### Setup 3: GraphQL API with Dapper
```
Template: /templates/graphql-api/microservice/
ORM: Dapper (for precision)
Resilience: Polly
Cache: In-Memory
Testing: xUnit
→ Example: examples/graphql-api/user-microservice/
```

### Setup 4: Notification Microservice with WebSockets
```
Template: /templates/rest-api/standalone/
Communication: WebSocket (SignalR)
ORM: EF (simpler for notifications)
Resilience: Polly
Testing: xUnit
→ Example: examples/rest-api/notification-service/
```

---

## Next Steps

1. **Answer all 9 questions** above
2. **Follow your unique path** through templates and examples
3. **Copy the template** for your project type and API style
4. **Study the example** that matches your setup
5. **Implement following TDD** - tests first
6. **Use checklists** to verify architecture
7. **Refer back** as you encounter patterns you're unsure about

---

**Ready to start?** → Pick your project type in Step 1 above and follow the path!
