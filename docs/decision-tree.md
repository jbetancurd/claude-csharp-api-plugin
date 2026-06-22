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

## Step 4: Data Persistence Strategy

**Question: How will you handle data persistence?**

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

## Step 5: Resilience & Error Handling

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

## Step 6: Caching Strategy

**Question: Do you need caching for performance?**

### A) No Caching
- Simple applications
- Real-time data only
- Low traffic
- Learning projects

### B) In-Memory Cache
```csharp
_memoryCache.Set("users:list", users, TimeSpan.FromMinutes(5));
```

✅ **Choose if:**
- Single server deployment
- Moderate data size
- Acceptable to lose cache on restart
- Lower infrastructure costs

→ **Template**: `/templates/shared/caching/in-memory-cache.template.cs`

### C) Distributed Cache (Redis)
```csharp
var cachedUsers = await _distributedCache.GetAsync("users:list");
```

✅ **Choose if:**
- Multiple server deployment
- Need cache persistence
- Microservices sharing cache
- High traffic, performance critical

→ **Template**: `/templates/shared/caching/distributed-cache.template.cs`

### D) Both (Multi-Level)
- L1: In-memory for speed
- L2: Distributed for consistency
- Complex but powerful

→ **Advanced pattern in examples**

---

## Step 7: Serialization & Protocol

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

## Step 8: Testing Strategy

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

## Step 9: Your Complete Path

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
