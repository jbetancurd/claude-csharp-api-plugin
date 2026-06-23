# Architecture Documentation

**Project Name**: [Your API Name]  
**Last Updated**: [Date]  
**Authors**: [Team Members]

## Overview

[Brief description of what this API does and its purpose]

**Example**:
> This is a microservice that handles user management within our e-commerce platform. It provides REST endpoints for user CRUD operations, authentication, and profile management.

## Technology Stack

| Layer | Technology | Version | Why |
|-------|-----------|---------|-----|
| **API Framework** | ASP.NET Core | 8.0 | Modern, performant, cross-platform |
| **ORM** | Entity Framework Core / Dapper | Latest | [Reason for choice] |
| **Database** | SQL Server / PostgreSQL / SQLite | [Version] | [Reason for choice] |
| **Caching** | Redis / In-Memory | [Version] | [Reason for choice] |
| **Testing** | xUnit + Moq | Latest | Industry standard for C# |
| **Logging** | Serilog | Latest | Structured logging with file output |
| **Validation** | FluentValidation | Latest | Fluent API for validation rules |

## Architecture Pattern: Onion Architecture

This project follows **Onion Architecture** with 4 layers:

### Layer 1: Domain (Core)
**Location**: `src/YourApi.Domain/`

**Responsibility**: Pure business logic, no dependencies on frameworks

**Contains**:
- Entities (User, Order, Product, etc.)
- Value Objects (Money, Email, Address, etc.)
- Domain Services (business logic that involves multiple entities)
- Interfaces (contracts for repositories, services)

**Example**:
```csharp
// User entity with business rules
public class User
{
    public int Id { get; set; }
    public Email Email { get; set; }
    public string Name { get; set; }
    public UserStatus Status { get; set; }
    
    // Business logic
    public void Deactivate() => Status = UserStatus.Inactive;
    public bool IsActive => Status == UserStatus.Active;
}
```

**Dependencies**: ✅ None (only .NET)

---

### Layer 2: Application (Use Cases)
**Location**: `src/YourApi.Application/`

**Responsibility**: Orchestrate domain logic, implement use cases

**Contains**:
- Application Services (use case orchestrators)
- DTOs (Data Transfer Objects)
- Specifications (query filters)
- Mapping profiles (entity ↔ DTO)
- Interfaces (repository contracts)
- Exceptions (application-level errors)

**Example**:
```csharp
// Application service orchestrates domain + infrastructure
public class UserApplicationService
{
    private readonly IUserRepository _repository;
    private readonly IEmailService _emailService;
    
    public async Task<UserDto> CreateUserAsync(CreateUserDto dto)
    {
        // Validate
        ValidateEmail(dto.Email);
        
        // Create domain entity
        var user = new User(dto.Name, dto.Email);
        
        // Persist
        await _repository.AddAsync(user);
        
        // Notify
        await _emailService.SendWelcomeEmailAsync(user.Email);
        
        // Return DTO
        return MapToDto(user);
    }
}
```

**Dependencies**: ✅ Domain layer only

---

### Layer 3: Infrastructure (Data & External Services)
**Location**: `src/YourApi.Infrastructure/`

**Responsibility**: Data access, external integrations

**Contains**:
- DbContext (EF Core database configuration)
- Repositories (data access patterns)
- External service implementations (email, payment, etc.)
- Configuration (connection strings, settings)
- Migrations (database schema changes)

**Example**:
```csharp
// Repository implements domain interface
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    
    public async Task AddAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
    }
}
```

**Dependencies**: ✅ Domain + Application

---

### Layer 4: Presentation (API)
**Location**: `src/YourApi.Presentation/`

**Responsibility**: HTTP endpoints, middleware, configuration

**Contains**:
- Controllers (API endpoints)
- Middleware (request/response handling)
- Filters (validation, error handling)
- Program.cs (DI setup, middleware pipeline)
- appsettings.json (configuration)

**Example**:
```csharp
// Controller delegates to application service
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserApplicationService _service;
    
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
    {
        var result = await _service.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
    }
}
```

**Dependencies**: ✅ All layers (depends on everyone)

---

## Dependency Flow

```
Presentation (API Layer)
    ↓
Application (Use Cases)
    ↓
Domain (Business Logic)
    ↓
Infrastructure (Data Access)

Rule: Inner layers never depend on outer layers
```

**Valid**: Presentation → Application → Domain  
**Invalid**: Domain → Application (WRONG!)

---

## API Style: [REST / RESTful / GraphQL]

### REST (Action-Based)
```
POST /api/users/123/approve
POST /api/users/123/deactivate
GET /api/reports/generate?startDate=2024-01-01
```
- Intuitive, action-focused
- Operations don't map to standard CRUD

### RESTful (Resource-Based)
```
GET    /api/users              # List
POST   /api/users              # Create
GET    /api/users/123          # Get
PUT    /api/users/123          # Update
DELETE /api/users/123          # Delete
PATCH  /api/users/123/email    # Partial update
```
- Proper HTTP semantics
- Standard, widely understood
- **[Chosen if applicable]**

### GraphQL (Query Language)
```graphql
query GetUser($id: ID!) {
  user(id: $id) {
    id
    name
    email
    orders { id total }
  }
}
```
- Flexible queries
- Client-driven data shapes
- **[Chosen if applicable]**

---

## Data Persistence

### ORM Choice: [Dapper / Entity Framework / LiteDB]

**[Explain why this ORM was chosen]**

#### Dapper
- ✅ Maximum control over SQL
- ✅ High performance
- ✅ Full control over queries
- ❌ More boilerplate
- **Used for**: Complex queries, performance-critical paths

#### Entity Framework Code-First
- ✅ Less boilerplate
- ✅ Change tracking
- ✅ Migrations
- ❌ Less control, slightly slower
- **Used for**: Standard CRUD, complex relationships

#### LiteDB
- ✅ Embedded, no server
- ✅ Document-based (flexible)
- ✅ JSON-like structure
- ❌ Not relational
- **Used for**: Local storage, NoSQL scenarios

### Database Schema

[Include ER diagram or description]

**Main Entities**:
- Users
- Orders
- Products
- [Other key entities]

**Key Relationships**:
- One User → Many Orders
- One Order → Many OrderItems
- [Other relationships]

---

## Resilience Strategy

### Polly Patterns Used

```csharp
// Retry with exponential backoff
.Retry(retryCount: 3, sleepDurationProvider: ...)

// Circuit breaker (fail fast after threshold)
.CircuitBreaker(handledEventsAllowedBeforeBreaking: 5)

// Timeout (prevent hanging)
.Timeout(TimeSpan.FromSeconds(10))

// Fallback (graceful degradation)
.Fallback(fallbackValue: DefaultUser)

// Bulkhead (isolate failure domains)
.BulkheadPolicy(maxParallelization: 10)
```

**When applied to**:
- External API calls
- Database operations (sometimes)
- Cache operations
- Payment processing

---

## Caching Strategy

### Cache Type: [None / In-Memory / Redis / Hybrid]

**[Explain why this strategy was chosen]**

#### In-Memory Cache
```csharp
// Single-server, fast, lost on restart
IMemoryCache for commonly accessed data
TTL: 5 minutes default
```

#### Redis (Distributed)
```csharp
// Multi-server, persistent, shared
IDistributedCache with Redis backend
TTL: 10 minutes for session data
```

#### Hybrid (In-Memory + Redis)
```csharp
// L1: In-Memory (fastest)
// L2: Redis (persistent, shared)
// L3: Database (source of truth)
```

**Cached Data**:
- User profiles (5 min TTL)
- Product catalogs (1 hour TTL)
- Settings/configuration (1 hour TTL)

**Cache Invalidation**:
- Time-based (TTL expires)
- Event-based (on data change)
- Manual (force clear when needed)

---

## Error Handling

### Global Exception Filter
```csharp
// All exceptions caught and converted to HTTP responses
ExceptionFilterAttribute handles:
- Validation exceptions → 400 Bad Request
- Not found exceptions → 404 Not Found
- Unauthorized → 401 Unauthorized
- Generic exceptions → 500 Internal Server Error
```

### Response Format (RFC 7807 Problem Details)
```json
{
  "type": "https://api.example.com/errors/validation",
  "title": "Validation Failed",
  "status": 400,
  "detail": "Email is required",
  "instance": "/api/users",
  "errors": {
    "email": ["Email is required"],
    "name": ["Name must be 2-50 characters"]
  }
}
```

---

## Testing Strategy

### Test Types & Coverage

```
Unit Tests: 70% (fast, isolated)
├─ Service layer logic
├─ Validator rules
└─ Repository queries

Integration Tests: 20% (medium, with DB)
├─ Service + Repository
├─ Middleware + Controllers
└─ End-to-end workflows

Acceptance Tests: 10% (slow, full API)
├─ API endpoints
└─ User scenarios
```

### Testing Approach

**Unit Testing with Mocks**:
```csharp
// Mock repositories, test service logic in isolation
var mockRepository = new Mock<IUserRepository>();
mockRepository.Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(new User { Id = 1 });
var service = new UserService(mockRepository.Object);
```

**BDD with Gherkin** (if chosen):
```gherkin
Feature: User Management
  Scenario: Create user with valid data
    Given I have valid user data
    When I submit the creation form
    Then the user is created successfully
```

---

## Logging & Monitoring

### Logging Strategy: Serilog

**Log Levels**:
- Debug: Detailed information (development)
- Information: General flow (application start/stop)
- Warning: Something unexpected (missing cache, retry)
- Error: Recoverable error (validation failed)
- Fatal: System crash (database unavailable)

**Structured Logging**:
```csharp
// Context is automatically captured
logger.Information("User {UserId} created by {AdminId}", 
    user.Id, adminUser.Id);

// Output to file as JSON
{
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "Information",
  "messageTemplate": "User {UserId} created by {AdminId}",
  "userId": 123,
  "adminId": 456,
  "source": "UserService"
}
```

**Log Sinks**:
- File (rolling daily)
- Console (development)
- External service (Seq, Datadog, Azure)

---

## Swagger UI & Health Checks

### Swagger
- **Available at**: `/` (root path, development only)
- **Displays**: All endpoints with schema
- **Features**: Try-it-out, authentication bearer token

### Health Check
- **Endpoint**: `/health`
- **Returns**: JSON with all system checks
- **Checks**: Self, Database, Cache, External services

---

## Deployment Architecture

### Development Environment
```
Developer Machine
    ↓
Local API (http://localhost:5000)
    ↓
Local SQLite / SQL Server Express
```

### Production Environment
```
Docker Container
    ↓
Cloud (Azure App Service / AWS / GCP)
    ↓
Cloud Database (SQL Server / PostgreSQL)
    ↓
CDN / Load Balancer
```

### CI/CD Pipeline
```
Push to main
    ↓
Build & Test
    ↓
Code Coverage Check (>70%)
    ↓
Create Docker Image
    ↓
Push to Registry
    ↓
Deploy to Staging
    ↓
Run Integration Tests
    ↓
Deploy to Production
```

---

## Key Design Decisions

| Decision | Chosen | Reason |
|----------|--------|--------|
| **Architecture Pattern** | Onion | Clear separation of concerns, testable |
| **API Style** | [REST/RESTful/GraphQL] | [Reason] |
| **ORM** | [Dapper/EF/LiteDB] | [Reason] |
| **Database** | [SQL Server/PostgreSQL/SQLite] | [Reason] |
| **Caching** | [None/In-Memory/Redis] | [Reason] |
| **Testing Framework** | xUnit + Moq | Industry standard, powerful |
| **Logging** | Serilog | Structured, flexible, file output |
| **Validation** | FluentValidation | Fluent API, reusable rules |

---

## Common Patterns Used

### Repository Pattern
- Abstracts data access
- One repository per aggregate root
- Implements specification pattern for queries

### Service Pattern
- Orchestrates use cases
- Calls repositories and external services
- Handles DTOs and mapping

### Dependency Injection
- Constructor injection (explicit dependencies)
- Interface segregation (small, focused interfaces)
- Registered in Program.cs

### Async/Await
- All I/O operations are async
- Database queries return Task
- Controllers use async endpoints

---

## Performance Considerations

### Optimization Strategies
- **Database**: Indexed queries, eager loading, pagination
- **Caching**: Cache-aside pattern, appropriate TTLs
- **API**: Compression, JSON size reduction, pagination
- **Code**: Avoid N+1 queries, use batch operations

### Monitoring
- Response time tracking
- Database query performance
- Cache hit rates
- Error rates and types

---

## Security Considerations

### Authentication
- [JWT / OAuth / Azure AD]
- Tokens in Authorization header

### Authorization
- [Role-based / Claim-based / Resource-based]
- Policy-based in endpoints

### Data Protection
- HTTPS enforced
- Sensitive data encrypted at rest
- No credentials in logs

---

## Future Enhancements

- [ ] Feature 1: [Description]
- [ ] Feature 2: [Description]
- [ ] Performance improvement: [Description]
- [ ] Infrastructure: [Description]

---

## References

- [Onion Architecture](https://jeffreypalermo.com/blog/the-onion-architecture-ussuri/)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)
- [Entity Framework Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Swagger/OpenAPI](https://swagger.io/)
- [Serilog Documentation](https://serilog.net/)

---

**Last Updated**: [Date]  
**Next Review**: [Date + 3 months]
