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

## Step 10: Testing & TDD Approach

**Question: How will you organize and write tests for correctness?**

### A) Unit Tests with Mocks (Isolation Testing)
```csharp
// Test single class, mock dependencies
[Fact]
public async Task CreateUser_WithValidData_ReturnsUserDto()
{
    // Arrange - Setup mocks
    var mockRepository = new Mock<IUserRepository>();
    mockRepository
        .Setup(r => r.AddAsync(It.IsAny<User>()))
        .ReturnsAsync(new User { Id = 1 });
    
    var service = new UserService(mockRepository.Object);
    
    // Act
    var result = await service.CreateUserAsync(new CreateUserDto { Name = "John" });
    
    // Assert
    Assert.NotNull(result);
    mockRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
}
```

✅ **Choose if:**
- Want **fast, isolated tests** (no database)
- Testing business logic in isolation
- Need to control external dependencies
- Quick test feedback loop important
- Each class tested independently
- No side effects or I/O operations

**Pros**: ⚡ Fast, reliable, easy to debug
**Cons**: ⚠️ Requires mocking, may miss integration bugs

**Mocking Libraries**:
- **Moq** - Most popular, fluent syntax
- **NSubstitute** - Simpler syntax
- **FakeItEasy** - Another alternative

→ **Read**: `/docs/tdd-xunit/unit-testing-mocks.md` (new)
→ **Template**: `/templates/shared/tests/unit-test-with-mocks.template.cs` (new)
→ **Example**: Unit test examples with Moq

### B) Integration Tests (Component Interactions)
```csharp
// Test multiple components working together with real database
[Collection("DatabaseCollection")]
public class UserServiceIntegrationTests
{
    private readonly ApplicationDbContext _context;
    
    [Fact]
    public async Task CreateUser_WithDatabase_PersistsCorrectly()
    {
        // Arrange - Real database
        var repository = new UserRepository(_context);
        var service = new UserService(repository);
        
        // Act
        var userId = await service.CreateUserAsync(new CreateUserDto { Name = "John" });
        
        // Assert - Verify in database
        var user = await _context.Users.FindAsync(userId);
        Assert.NotNull(user);
        Assert.Equal("John", user.Name);
    }
}
```

✅ **Choose if:**
- Testing component interactions
- Need real database operations
- Want to catch integration bugs
- Don't mind slower tests
- Verifying repository + service together
- Testing actual data persistence

**Pros**: 📦 Catches real bugs, realistic scenarios
**Cons**: ⚠️ Slower, requires DB, harder to debug

→ **Templates**: `/templates/shared/tests/integration-test.template.cs` (new)

### C) BDD with Gherkin/SpecFlow (Behavior-Driven Development)
```gherkin
# Feature: User Management
Feature: User Creation
  As a user management system
  I want to create new users
  So that I can manage user accounts

  Scenario: Create valid user
    Given I have a valid user data
      | Name  | Email              |
      | John  | john@example.com   |
    When I create a new user
    Then the user should be created successfully
    And the user ID should be returned
    And the user should be in the database
```

Translated to C# with SpecFlow:
```csharp
[Binding]
public class UserCreationSteps
{
    private UserDto _userData;
    private int _createdUserId;
    
    [Given("I have a valid user data")]
    public void GivenValidUserData(Table table)
    {
        var row = table.Rows.First();
        _userData = new UserDto 
        { 
            Name = row["Name"], 
            Email = row["Email"] 
        };
    }
    
    [When("I create a new user")]
    public async Task WhenCreateUser()
    {
        _createdUserId = await _userService.CreateUserAsync(_userData);
    }
    
    [Then("the user should be created successfully")]
    public void ThenUserCreatedSuccessfully()
    {
        Assert.True(_createdUserId > 0);
    }
}
```

✅ **Choose if:**
- Want **business-readable tests**
- Non-technical stakeholders need to understand tests
- Complex business scenarios to document
- Behavior-driven development (BDD) approach
- Tests as living documentation
- Acceptance criteria from user stories

**Pros**: 📖 Human-readable, stakeholder engagement, documentation
**Cons**: ⚠️ More setup, SpecFlow license, slower execution

**Framework**: SpecFlow (NuGet: SpecFlow)

→ **Read**: `/docs/tdd-xunit/bdd-gherkin.md` (new)
→ **Template**: `/templates/shared/tests/bdd-gherkin.template.feature` (new)
→ **Example**: Gherkin scenarios with SpecFlow

### D) API/Acceptance Tests (End-to-End)
```csharp
// Test HTTP endpoints directly
[Fact]
public async Task PostUser_WithValidPayload_Returns201Created()
{
    // Arrange
    var client = _factory.CreateClient();
    var createUserDto = new CreateUserDto { Name = "John" };
    
    // Act
    var response = await client.PostAsJsonAsync("/api/users", createUserDto);
    
    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    var user = await response.Content.ReadAsAsync<UserDto>();
    Assert.NotNull(user.Id);
}
```

✅ **Choose if:**
- Testing complete request/response cycles
- Verifying HTTP status codes and headers
- Testing middleware and filters
- Real API contract testing
- Including in CI/CD pipeline

→ **Template**: `/templates/shared/tests/api-test.template.cs`

### Testing Pyramid

```
        📄 Acceptance Tests (Few, slow)
              /\
             /  \
            /    \
          /        \
        📋 Integration Tests (Some, medium)
           /\
          /  \
         /    \
        /      \
      ⚡ Unit Tests (Many, fast)
      /\
     /  \
    /    \
```

**Best Practice**: 
- **70%** Unit tests (fast feedback)
- **20%** Integration tests (catch bugs)
- **10%** Acceptance tests (user experience)

---

## Step 11: Mocking Strategy (For Unit Tests)

**Question: How will you mock external dependencies in unit tests?**

### A) No Mocking (Only for Simple Cases)
✅ **Choose if:**
- No external dependencies (pure functions)
- Very simple, stateless logic
- Learning tests without complexity
- Prototyping quickly

**Example**:
```csharp
[Fact]
public void ValidateEmail_WithValidEmail_ReturnsTrue()
{
    var validator = new EmailValidator();
    Assert.True(validator.IsValid("john@example.com"));
}
```

### B) Moq (Most Popular)
```csharp
var mock = new Mock<IUserRepository>();
mock.Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(new User { Id = 1, Name = "John" });

var result = mock.Object.GetByIdAsync(1);
```

✅ **Choose if:**
- Most popular, great documentation
- Fluent, readable syntax
- Complete feature set
- Large community support

→ **Install**: `dotnet add package Moq`

### C) NSubstitute (Simpler Syntax)
```csharp
var substitute = Substitute.For<IUserRepository>();
substitute.GetByIdAsync(1)
    .Returns(new User { Id = 1, Name = "John" });

var result = substitute.GetByIdAsync(1);
```

✅ **Choose if:**
- Prefer simpler, more intuitive syntax
- Less ceremony in setup
- Cleaner test code
- Faster to write

→ **Install**: `dotnet add package NSubstitute`

### D) Fake Objects (Manual Mocks)
```csharp
public class FakeUserRepository : IUserRepository
{
    public async Task<User> GetByIdAsync(int id)
    {
        return new User { Id = id, Name = "Fake User" };
    }
}

// In test:
var fakeRepo = new FakeUserRepository();
var service = new UserService(fakeRepo);
```

✅ **Choose if:**
- Want complete control over behavior
- Simple repository with few methods
- Don't want external dependencies
- Learning testing concepts

**Pros**: ✅ No external library, full control
**Cons**: ⚠️ More code to maintain

### Mocking Best Practices

| Practice | ✅ Do | ❌ Don't |
|----------|--------|---------|
| **Mock external services** | HTTP calls, DB | Business logic |
| **Verify interactions** | Assert.Verify(Times.Once) | Over-verify |
| **Setup realistic data** | Valid user objects | NULL or empty |
| **Keep tests focused** | One behavior per test | Multiple assertions |
| **Use constants** | Shared test data | Magic numbers |

---

## Step 12: Test Organization & Structure

**Question: How will you organize your test projects?**

### A) Single Test Project
```
Solution/
├── MyApi/                          (Main project)
│   ├── Controllers/
│   ├── Services/
│   └── Models/
└── MyApi.Tests/                    (All tests here)
    ├── Unit/
    ├── Integration/
    └── API/
```

✅ **Choose if:**
- Small project
- Simple structure
- Quick to get started
- All tests similar complexity

### B) Multiple Test Projects (Recommended)
```
Solution/
├── MyApi.Domain/                   (Domain layer)
├── MyApi.Application/              (Application layer)
├── MyApi.Infrastructure/           (Infrastructure layer)
├── MyApi.Presentation/             (API layer)
│
├── MyApi.UnitTests/                (Unit tests only)
├── MyApi.IntegrationTests/         (Integration tests)
└── MyApi.AcceptanceTests/          (API/E2E tests)
```

✅ **Choose if:**
- Medium to large projects
- Clear layer testing responsibilities
- Different teams own different test types
- Separate test running/pipelines

**Advantages**:
- Clear ownership
- Easy to skip test types in CI/CD
- Better organization
- Can have different dependencies per project

### C) Feature-Based Structure
```
Tests/
├── UserManagement/
│   ├── CreateUserUnitTests.cs
│   ├── CreateUserIntegrationTests.cs
│   └── CreateUserAcceptanceTests.cs
├── OrderManagement/
│   ├── PlaceOrderUnitTests.cs
│   ├── PlaceOrderIntegrationTests.cs
│   └── PlaceOrderAcceptanceTests.cs
```

✅ **Choose if:**
- DDD (Domain-Driven Design) approach
- Feature-based project organization
- All tests for feature in one folder
- Clear feature boundaries

---

## Step 13: E2E Testing & API Simulation Strategy

**Question: How will you test complete user workflows and API integration?**

### A) No E2E Testing
✅ **Choose if:**
- Unit + Integration tests sufficient
- Small project, simple workflows
- No complex user journeys
- Budget constraints (Playwright has licensing)

**Impact**: Some edge cases may be missed in integration

---

### B) E2E Testing with Playwright (Browser Automation)
```csharp
// .NET Playwright tests
[PlaywrightTest]
public class UserWorkflowTests
{
    [Test]
    public async Task UserRegistrationFlow()
    {
        // Test complete user journey in browser
        var page = await Browser.NewPageAsync();
        await page.GotoAsync("https://localhost:7000");
        await page.FillAsync("input[name='email']", "user@example.com");
        await page.FillAsync("input[name='password']", "SecurePass123!");
        await page.ClickAsync("button:has-text('Register')");
        await Expect(page).ToHaveURLAsync(new Regex(".*/profile"));
    }
}
```

✅ **Choose if:**
- Web UI exists or planned
- Need to test browser interactions
- Complete user journey validation
- Visual regression testing needed
- Cross-browser compatibility important

**Pros**: 
- ⚡ Tests real browser behavior
- 📱 Cross-browser testing (Chrome, Firefox, Safari)
- 🎬 Record and playback capabilities
- 📸 Screenshots for debugging
- 🔍 Visual regression detection

**Cons**: 
- ⚠️ Slower than unit/integration tests
- ⚠️ More brittle (selectors change)
- ⚠️ Requires running browser instances
- ⚠️ Licensing costs for commercial use

→ **Read**: `/docs/e2e-testing/playwright-guide.md` (new)
→ **Template**: `/templates/shared/e2e/playwright-tests.template.cs` (new)

---

### C) API Request Simulator (Console CLI Tool)
```csharp
// Interactive CLI simulator - test all API endpoints
// Run: dotnet run --project src/YourApi.Simulator
// 
// Menu:
// 1. Test User Registration
// 2. Test User Login
// 3. Test Create Order
// 4. Test Payment Processing
// 5. View all requests/responses
// 6. Export test results
// 7. Exit

// Example workflow in console:
> 1  // Select "Test User Registration"
> Email: john@example.com
> Password: SecurePass123!
> [Request sent to: POST /api/auth/register]
> [Response: 201 Created]
> User ID: 123
> 
> 2  // Select "Test User Login"
> Email: john@example.com
> Password: SecurePass123!
> [Request sent to: POST /api/auth/login]
> [Response: 200 OK]
> JWT Token: eyJhbGciOiJIUzI1NiIs...
```

✅ **Choose if:**
- No web UI (API-only or mobile client)
- Want to replay all API workflows
- Need automated request testing
- Want to document all flows
- Simple, maintainable testing approach
- No browser automation needed

**Pros**: 
- ⚡ Fast (no browser overhead)
- 🎯 Focused on API testing
- 📝 Easy to document requests/responses
- 🔄 Simple to record new scenarios
- 💾 Export results as JSON/CSV
- 🎨 Console UI interactive
- ✅ No external dependencies (no Playwright)

**Cons**: 
- ❌ No UI/visual testing
- ❌ Manual creation of workflows
- ⚠️ More code to maintain

**Included in Simulator**:
- Interactive menu system
- HTTP request builder
- Response validation
- JSON payload support
- JWT token management
- Test result history
- Export capabilities (CSV, JSON)
- Concurrent request testing
- Performance metrics

→ **Read**: `/docs/e2e-testing/api-simulator-guide.md` (new)
→ **Template**: `/templates/shared/e2e/api-request-simulator.template.cs` (new)

---

### D) Both E2E & API Simulator
✅ **Choose if:**
- Web UI + API both important
- Want comprehensive coverage
- Browser + API testing needed
- Budget allows
- Complex system with many scenarios

**Usage**:
- **Playwright**: User-facing UI tests
- **Simulator**: Backend API workflow tests
- Together: Complete system validation

---

## Comparison: E2E vs Simulator

| Aspect | Playwright E2E | API Simulator |
|--------|---|---|
| **Speed** | Slower (browser) | ⚡⚡⚡ Fast |
| **Test UI** | ✅ Yes | ❌ No |
| **Test API** | ✅ Yes (via UI) | ✅ Yes (direct) |
| **Setup Complexity** | Medium | Low |
| **Maintenance** | Higher (selectors) | Lower |
| **Browser Automation** | ✅ Full | ❌ N/A |
| **Cross-browser** | ✅ Chrome/Firefox/Safari | ❌ N/A |
| **Visual Testing** | ✅ Yes | ❌ No |
| **Documentation** | Test code is doc | Console menu is doc |
| **For API-only** | Overkill | Perfect |
| **For Web+API** | Best | Supplementary |
| **Cost** | $ (licensing) | Free |
| **Learning curve** | Medium | Low |

**Recommendation**:
- **API-only services**: Use Simulator ✅
- **Web + API**: Use Playwright ✅
- **Complex system**: Use Both ✅✅

---

## Step 14: Swagger UI & Health Checks Configuration

**Question: How will you expose your API documentation and health status?**

### A) No Swagger (Production Only)
✅ **Choose if:**
- Production environment only
- No development/testing needed
- Security concerns about exposing API surface
- Documentation maintained separately

**Impact**: Clients need to reference separate documentation
**Startup**: White page on root path (need to document endpoints)

---

### B) Swagger UI as Default Landing Page (Recommended)
```csharp
// Swagger appears at root: http://localhost:5000/
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = string.Empty;  // Root path
});
```

✅ **Choose if:**
- Development or testing environment
- Want self-documenting API
- Interactive API testing (try-it-out feature)
- Automated client generation
- Team collaboration on endpoints

**Startup Experience**:
- App starts → Swagger UI loads immediately
- Health check available at `/health`
- Links to API documentation
- **No white page confusion!**

**Setup Requirements**:
- Swagger middleware enabled
- Health check endpoint configured
- Clear startup message showing URLs

→ **Read**: `/docs/middleware/swagger-health-checks.md`
→ **Template**: `/templates/shared/middleware/program-swagger-health.template.cs`
→ **Appsettings**: Include health check configuration

### C) Swagger + Custom Branding
```csharp
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API v1");
    options.RoutePrefix = string.Empty;
    options.DocumentTitle = "My Company API Docs";
    options.InjectStylesheet("/custom-swagger.css");
});
```

✅ **Choose if:**
- Want branded documentation
- Multiple API versions
- Custom styling to match company brand
- Need to remove Swagger branding

---

## Port Configuration

**Question: How should your API configure the listening port?**

### Default Port Options
- **.NET Default**: 5000 (HTTP), 5001 (HTTPS)
- **HTTP Standard**: 80
- **HTTPS Standard**: 443
- **Custom**: Any available port

### Configuration Priority (Highest to Lowest)
1. **Environment Variable** (recommended for Docker/cloud)
   ```bash
   export PORT=8080
   dotnet run
   ```

2. **Command Line Argument**
   ```bash
   dotnet run -- --port 8080
   ```

3. **appsettings.json**
   ```json
   { "PORT": 5000 }
   ```

4. **Code Default**
   ```csharp
   var port = configuration.GetValue<int?>("PORT") ?? 5000;
   ```

### Startup Message Example
```
╔════════════════════════════════════╗
║        API Started Successfully    ║
╚════════════════════════════════════╝

🌍 Environment:  Development
🔌 Port:         5000
🌐 Base URL:     http://localhost:5000
📄 Swagger:      http://localhost:5000/
❤️  Health:       http://localhost:5000/health

════════════════════════════════════
```

---

## Step 15: Project Documentation Setup (Optional)

**Question: Do you want to create project documentation templates?**

### A) Skip Documentation Setup
✅ **Choose if:**
- Small project (just you)
- Documentation not needed
- Adding later manually
- Team already has templates

### B) Create Documentation Folder (Recommended) ✅
✅ **Choose if:**
- Want organized documentation
- Team/open source project
- Need onboarding guides
- PR template for consistency
- Share architecture decisions

**Creates `/documents` folder with:**

1. **ARCHITECTURE.md** - Project architecture overview
   - Layer explanations
   - Design patterns used
   - Technology decisions (why Dapper vs EF, why Redis, etc.)
   - Database schema overview
   - API endpoints summary

2. **DEVELOPER_ONBOARDING.md** - New developer guide
   - Prerequisites (SDK, tools, databases)
   - Setup instructions (clone, build, run)
   - Project structure walkthrough
   - Common development tasks
   - Debugging tips
   - Testing strategy
   - Code style guidelines
   - Deployment process

3. **PULL_REQUEST_TEMPLATE.md** - GitHub PR template
   - Description section
   - Type of change (Bug, Feature, Docs)
   - Testing checklist
   - Architecture checklist
   - Breaking changes
   - Screenshots (if UI changes)

→ **Copy templates from**: `/templates/shared/documents/`

### Folder Structure Created

```
project-root/
├── documents/
│   ├── ARCHITECTURE.md          ← Design & decisions
│   ├── DEVELOPER_ONBOARDING.md  ← Getting started guide
│   ├── PULL_REQUEST_TEMPLATE.md ← PR guidelines
│   ├── API_ENDPOINTS.md         ← API reference
│   ├── DATABASE_SCHEMA.md       ← Data model
│   └── TROUBLESHOOTING.md       ← Common issues
└── .github/
    └── PULL_REQUEST_TEMPLATE.md ← GitHub PR template
```

### What Gets Documented

**ARCHITECTURE.md covers:**
- Project overview
- Layer responsibilities (Onion Architecture)
- Technology choices with rationale
- Database design
- API style (REST/RESTful/GraphQL)
- Caching strategy
- Error handling approach
- Testing strategy
- Deployment architecture

**DEVELOPER_ONBOARDING.md covers:**
- System requirements
- Installation steps
- First run instructions
- Project structure walkthrough
- IDE setup
- Running tests
- Common commands
- Debugging
- Code style
- Creating your first PR

**PULL_REQUEST_TEMPLATE.md covers:**
- Description of changes
- Type of change (check boxes)
- Related issues
- Testing checklist
- Architecture review checklist
- Breaking changes
- Screenshots/demos
- Deployment notes

---

## Step 16: Your Complete Path

### Full Project Setup Workflow

Based on your answers, follow this path:

1. **Architecture Foundations**
   - Read: `/docs/architecture/onion-architecture.md`
   - Read: API style guide (`/docs/api-styles/{your-style}/`)

2. **Project Templates**
   - Copy: Template from `/templates/{your-style}/{your-type}/`
   - Study: Example from `/examples/{your-style}/{your-type}/`

3. **Data Access Layer**
   - Setup: ORM from `/templates/shared/repositories/`
   - Read: ORM comparison guide

4. **Infrastructure & Resilience**
   - Add: Resilience if needed (`/templates/shared/resilience/polly-setup.template.cs`)
   - Add: Caching if needed (`/templates/shared/caching/`)

5. **Testing Strategy**
   - Plan: Testing approach (Unit, Integration, BDD, API)
   - Choose: Mocking library (Moq, NSubstitute, Fake objects)
   - Organize: Test projects structure
   - Setup: Test templates from `/templates/shared/tests/`

6. **E2E Testing & Simulation** (Optional)
   - Choose: Playwright (browser automation) OR API Simulator (console CLI)
   - Setup: E2E templates from `/templates/shared/e2e/`
   - Document: All user workflows and API scenarios

7. **API Configuration**
   - Setup: Swagger UI + Health Checks (`/docs/middleware/swagger-health-checks.md`)
   - Use: Program.cs template (`/templates/shared/middleware/program-swagger-health.template.cs`)

8. **Test Project Connection** (CRITICAL - Tests Won't Run Without This!)
   - Follow: `/docs/tdd-xunit/test-project-setup.md` (Complete guide)
   - Add test projects to solution: `dotnet sln add tests/YourApi.UnitTests/YourApi.UnitTests.csproj`
   - Add project references: `dotnet add reference ../../src/Domain/YourApi.Domain.csproj`
   - Verify tests discovered: `dotnet test --list-tests` (Should show all tests)
   - Verify tests run: `dotnet test` (All should pass, none skipped)

9. **Test Coverage Validation**
   - Ensure all generated code has tests
   - Follow: `/docs/tdd-xunit/test-coverage-validation.md` (Complete guide)
   - Domain layer: Aim for 85%+ coverage
   - Application layer: Aim for 75%+ coverage
   - Run with coverage: `dotnet test /p:CollectCoverage=true`
   - Generate report: `reportgenerator -reports:coverage.opencover.xml`

10. **Final Validation & Quality**
    - Verify: Architecture checklist (`/checklists/architecture-audit.md`)
    - Run: All tests (unit, integration, E2E): `dotnet test`
    - Check: Code coverage (target 70% overall)
    - Check: No test warnings or skipped tests
    - Build: `dotnet build` (no warnings)

11. **Documentation (Optional)**
    - Create: `/documents` folder
    - Copy: `ARCHITECTURE.md` template
    - Copy: `DEVELOPER_ONBOARDING.md` template
    - Copy: `PULL_REQUEST_TEMPLATE.md` template
    - Customize for your project

### Quick Commands

```bash
# Setup project structure
mkdir -p src/{Domain,Application,Infrastructure,Presentation}
mkdir -p tests/{Unit,Integration,Acceptance}
mkdir -p documents

# Create solution
dotnet new sln

# Add projects
dotnet new classlib -n YourApi.Domain
dotnet new classlib -n YourApi.Application
dotnet new classlib -n YourApi.Infrastructure
dotnet new webapi -n YourApi.Presentation
dotnet new xunit -n YourApi.UnitTests

# Install essential packages
dotnet add YourApi.Presentation package Swashbuckle.AspNetCore
dotnet add YourApi.Presentation package AspNetCore.HealthChecks.UI
dotnet add YourApi.UnitTests package Moq
dotnet add YourApi.UnitTests package FluentAssertions

# Run and test
dotnet build
dotnet test
dotnet run --project src/YourApi.Presentation
```

### Success Checklist

- ✅ Project structure follows Onion Architecture
- ✅ All 4 layers present (Domain, Application, Infrastructure, Presentation)
- ✅ Dependency injection configured in Program.cs
- ✅ Swagger UI running at root path (`http://localhost:5000/`)
- ✅ Health check endpoint at `/health`
- ✅ Unit tests with mocks (70% coverage)
- ✅ Integration tests with database
- ✅ Architecture decisions documented
- ✅ Developer onboarding guide created
- ✅ PR template configured
- ✅ All tests passing
- ✅ No warnings in build output

---

## Quick Reference: Decision Matrix

| Question | Choice A | Choice B | Choice C | Choice D |
|----------|----------|----------|----------|----------|
| **Project Type** | Microservice | Full REST API | Standalone | — |
| **API Style** | REST | RESTful | GraphQL | — |
| **Real-Time** | HTTP only | WebSockets | Both | — |
| **Database** | SQL Server | SQLite | LiteDB | Hybrid |
| **CQRS** | Traditional | CQRS Pattern | — | — |
| **ORM** | Dapper | EF Code-First | — | — |
| **Resilience** | Basic | Polly | — | — |
| **Cache** | None | In-Memory | Distributed | Hybrid |
| **Protocol** | JSON | Protobuf | — | — |
| **Testing** | Unit Tests | Integration | BDD/Gherkin | API Tests |
| **Mocking** | None | Moq | NSubstitute | Fake Objects |
| **Test Structure** | Single Project | Multiple Projects | Feature-Based | — |
| **Swagger** | None | Default Page | Custom Brand | — |

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
