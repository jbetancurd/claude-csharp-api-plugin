# Claude C# API Architecture Plugin - Documentation

This is a Claude Code plugin designed to guide C# API implementations with production-quality architecture, patterns, and best practices.

## Plugin Purpose

When working on C# API projects, Claude Code will:
1. **Guide architectural decisions** - Help choose between microservices, full APIs, or standalone services
2. **Recommend API styles** - Advise REST, RESTful, or GraphQL based on requirements
3. **Suggest patterns** - Provide SOLID, Onion, DRY, and component reuse examples
4. **Review code** - Audit against architecture, resilience, and performance guidelines
5. **Generate scaffolding** - Create project structure and test templates
6. **Teach TDD** - Guide xUnit test-first development

## How Claude Uses This Plugin

### When You're Starting a C# API Project
Claude will:
1. Suggest the decision-tree questionnaire
2. Based on answers, recommend project structure
3. Provide relevant templates and examples
4. Explain architectural choices

### When You're Implementing Code
Claude will:
1. Suggest patterns from the appropriate example
2. Explain why layers are separated that way
3. Generate test-first implementations with xUnit
4. Highlight DRY violations and component reuse opportunities

### When You're Reviewing Code
Claude will:
1. Check against architecture checklist
2. Verify Onion layer boundaries
3. Validate SOLID principle adherence
4. Check resilience (Polly) patterns
5. Audit caching and performance

## Plugin Structure Reference

The plugin is organized by **API Style** (REST, RESTful, GraphQL), with each style showing:
- **Microservice** template and example
- **Full REST API** template and example  
- **Standalone Service** template and example

### Decision Tree Flow

```
START
  ↓
What is your project type?
├─ Microservice → See microservice templates/examples
├─ Full REST API → See full-api templates/examples
└─ Standalone Service → See standalone templates/examples
  ↓
What API style do you prefer?
├─ REST (action-based) → /rest-api/
├─ RESTful (resource-based) → /restful-api/
└─ GraphQL (query language) → /graphql-api/
  ↓
Do you need WebSockets or just HTTP?
├─ HTTP only → Standard HTTP setup
├─ WebSockets → Add WebSocket middleware
└─ Both → Hybrid setup (REST + WebSocket)
  ↓
Which ORM fits your needs?
├─ Dapper → Lightweight, full control
└─ Entity Framework Code-First → Feature-rich, less boilerplate
  ↓
Do you need resilience patterns?
├─ Yes → Include Polly setup
└─ No → Basic error handling
  ↓
Do you need caching?
├─ In-memory → Simple caching
├─ Distributed → Redis or similar
└─ Both → Multi-level caching
  ↓
APPLY TEMPLATE & EXAMPLES
```

## Key Architecture Concepts

### Onion Architecture

The plugin enforces **Onion Architecture** with four layers:

1. **Domain Layer** (Core)
   - Entities, Value Objects, Domain Services
   - Zero dependencies - no frameworks
   - Pure domain logic

2. **Application Layer** (Use Cases)
   - Application Services, DTOs, Specs
   - Depends only on Domain
   - Orchestrates domain logic

3. **Infrastructure Layer** (Data/External)
   - Repositories, ORM DbContext, External services
   - Depends on Application & Domain
   - Handles persistence and integration

4. **Presentation Layer** (API)
   - Controllers, Middleware, API DTOs
   - Depends on all inner layers
   - Handles HTTP/WebSocket/GraphQL communication

**Direction of Dependency**: Always inward toward Domain.

### SOLID Principles Applied

- **S**ingle Responsibility: Each class/interface has one reason to change
- **O**pen/Closed: Open for extension, closed for modification
- **L**iskov Substitution: Derived classes substitute base classes
- **I**nterface Segregation: Clients depend on focused interfaces
- **D**ependency Inversion: Depend on abstractions, not concretions

### DRY - Don't Repeat Yourself

The plugin shows patterns for:
- Generic repositories with specifications
- Reusable service base classes
- Shared validation and error handling
- Common middleware and filters
- Aspect-oriented concerns (logging, caching)

## API Styles Explained

### REST (Action-Based)
```
POST /api/users/123/approve
POST /api/users/123/disable
GET /api/reports/generate?startDate=...
```
✅ Simple, intuitive
❌ Ignores HTTP semantics, action-focused

### RESTful (Resource-Based)
```
GET /api/users                    # List
POST /api/users                   # Create
GET /api/users/123               # Read
PUT /api/users/123               # Update
DELETE /api/users/123            # Delete
```
✅ Proper HTTP semantics, standard, composable
❌ Some actions don't map to CRUD

### GraphQL (Query Language)
```
query GetUser($id: ID!) {
  user(id: $id) {
    id
    name
    posts { title }
  }
}
```
✅ Flexible, client-driven queries, efficient
❌ More complex, requires learning

## Communication Patterns

### HTTP Requests
- Standard REST/RESTful APIs
- Stateless, cacheable
- Examples in `/templates/shared/`

### WebSockets
- Real-time bidirectional communication
- Perfect for notifications, live updates
- Templates show SignalR integration

### Protocol Buffers
- Efficient binary serialization
- Great for microservice-to-microservice
- Guide in `/docs/communication/protobuf-guide.md`

## Resilience with Polly

The plugin includes Polly setup for:
- **Retry Policy** - Exponential backoff
- **Circuit Breaker** - Fail fast after threshold
- **Timeout Policy** - Prevent hanging requests
- **Fallback Policy** - Graceful degradation
- **Bulkhead Policy** - Isolate failure domains

Example setup provided in `/templates/shared/resilience/`

## Testing & TDD

The plugin emphasizes **Test-Driven Development** with xUnit:

1. **Write test first** - Define expected behavior
2. **Red phase** - Test fails
3. **Green phase** - Make minimal code to pass
4. **Refactor** - Improve while keeping tests green

### Test Organization

- **Unit Tests** - Test single class in isolation
- **Integration Tests** - Test component interactions
- **API Tests** - Test HTTP endpoints

### xUnit Patterns

- **AAA Pattern** (Arrange-Act-Assert)
- **Fixtures** - Shared test data and setup
- **Theories** - Parameterized tests
- **Custom Attributes** - Test categorization

Examples in `/templates/shared/tests/`

## Caching Strategies

The plugin covers:
- **In-Memory Cache** - Fast, single-server
- **Distributed Cache** - Redis, multi-server
- **Cache Invalidation** - TTL, events, tags
- **Cache Patterns** - Cache-aside, write-through

## ORM Selection Guide

### Use Dapper When
- You need maximum control over SQL
- Performance is critical (low-level optimization)
- Queries are complex and hand-tuned
- You prefer explicit data access code
- Working with legacy databases

### Use Entity Framework Code-First When
- Building domain-first applications
- You want LINQ query interface
- Change tracking and lazy loading help
- Entity relationships are complex
- Migration management is valuable

The plugin includes decision criteria and examples for both.

## Performance & Memory Considerations

The plugin addresses:
- Async/await patterns (never blocking)
- Memory pooling for buffers
- String allocation reduction
- Database query optimization
- Connection pooling
- Caching strategy selection

## Project Type Differences

### Microservice
- **Goal**: Single responsibility, deployable independently
- **Size**: Small-to-medium
- **Database**: Own database
- **Communication**: HTTP, gRPC, events
- **Example**: Order Service, User Service

### Full REST API
- **Goal**: Complete business functionality
- **Size**: Large
- **Database**: May have multiple DB contexts
- **Features**: Full suite (auth, validation, caching)
- **Example**: E-commerce API, CMS

### Standalone Service
- **Goal**: Single function, often background work
- **Size**: Small-medium
- **Pattern**: Often timer-based or event-driven
- **Example**: Notification Service, Report Generator

## Using the Examples

Each example includes:
- ✅ Complete project structure
- ✅ All four Onion layers
- ✅ Proper separation of concerns
- ✅ Repository and Service patterns
- ✅ xUnit tests (AAA pattern)
- ✅ Async/await best practices
- ✅ Error handling and validation
- ✅ Dependency injection setup
- ✅ Configuration management

### How to Study Examples

1. **Understand the domain** - What does it do?
2. **Trace the layers** - Domain → Application → Infrastructure → Presentation
3. **Find patterns** - Repository, Service, Specification patterns
4. **Read tests** - Understand expected behavior from tests
5. **Apply to your code** - Use as template, adapt to your needs

## Integration Points

Claude will reference this plugin when:
1. **Starting new C# API project** - Suggests decision-tree and templates
2. **Implementing repositories** - Shows ORM examples and patterns
3. **Creating services** - Demonstrates application layer organization
4. **Writing controllers** - Shows API endpoint patterns
5. **Writing tests** - Provides xUnit templates and practices
6. **Reviewing code** - Checks architecture and checklist compliance

## Maintenance Notes

This plugin is designed to evolve:
- Add new examples as you create projects
- Update templates based on lessons learned
- Keep examples runnable and tested
- Update documentation as practices change
- Add new communication patterns as needed

## Key Files to Reference

When Claude mentions architecture decisions, it will likely reference:
- `docs/decision-tree.md` - Initial project setup
- `docs/architecture/onion-architecture.md` - Layer definitions
- `docs/api-styles/` - API pattern guidance
- `templates/shared/` - Reusable patterns
- `examples/` - Complete implementations
- `checklists/` - Compliance verification

## Success Metrics

You'll know the plugin is working well when:
✅ New projects follow consistent architecture
✅ Layer boundaries are clear and enforced
✅ Tests drive development (TDD)
✅ Code is DRY with reusable components
✅ SOLID principles are visible in code
✅ Error handling is consistent
✅ Performance is measurable and optimized

---

**Plugin Status**: Ready for use
**Last Updated**: June 2026
