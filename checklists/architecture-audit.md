# Architecture Audit Checklist

Use this checklist to verify your C# API follows Onion Architecture and SOLID principles.

## Domain Layer Audit

### Structure
- [ ] Domain/ folder exists and contains business logic
- [ ] Domain has no dependencies on other projects
- [ ] Domain references no frameworks (EF, ASP.NET Core, DI containers)
- [ ] Domain has no NuGet packages except testing

### Entities
- [ ] Entities encapsulate behavior (not just data holders)
- [ ] Entities validate invariants in constructors and methods
- [ ] Entities use private setters to protect state
- [ ] Entities use domain methods (Approve(), Complete(), etc.)
- [ ] No DbContext or ORM attributes on entities

### Value Objects
- [ ] Value Objects are immutable
- [ ] Value Objects have overridden Equals/GetHashCode
- [ ] Value Objects encapsulate validation logic
- [ ] Examples: Money, Address, Email, DateRange

### Domain Services
- [ ] Domain services exist only for logic not suitable for entities
- [ ] Domain services are stateless
- [ ] Domain services depend only on domain objects
- [ ] Domain services don't access repositories or external services

### Domain Events
- [ ] Domain events represent business events (OrderCreated, PaymentProcessed)
- [ ] Events are immutable DTOs
- [ ] Events capture important business facts
- [ ] Events include timestamp and context

### Exceptions
- [ ] Custom exceptions inherit from DomainException
- [ ] Exceptions convey business rules
- [ ] Exceptions are thrown for violations of invariants

---

## Application Layer Audit

### Structure
- [ ] Application/ folder exists
- [ ] Application references Domain layer only (plus interfaces)
- [ ] Application contains no HTTP/database concerns
- [ ] Application is independent of delivery mechanism

### Services
- [ ] Each service represents one use case
- [ ] Services depend on interfaces (Dependency Inversion)
- [ ] Services orchestrate domain objects
- [ ] Services don't contain business logic (that's Domain)
- [ ] Services handle validation, mapping, and coordination
- [ ] Services are async-first (Task/Task<T>)

### DTOs
- [ ] DTOs are defined in Application layer
- [ ] DTOs contain no domain logic
- [ ] DTOs map cleanly from/to domain entities
- [ ] DTOs don't expose domain entities to other layers
- [ ] Input DTOs for requests, Output DTOs for responses

### Validators
- [ ] Input validation happens in Application layer
- [ ] Validation follows Specification pattern
- [ ] Validators check structural rules
- [ ] Domain entities check business rules

### Specifications
- [ ] Specifications exist for complex queries
- [ ] Specifications build queries without exposing SQL
- [ ] Specifications are reusable across services
- [ ] Specifications encapsulate query logic

### Interfaces
- [ ] Repository interfaces defined in Application
- [ ] Service interfaces defined in Application
- [ ] Interfaces represent contracts, not implementations
- [ ] All dependencies injected via constructor

---

## Infrastructure Layer Audit

### Structure
- [ ] Infrastructure/ folder exists
- [ ] Infrastructure implements Application interfaces only
- [ ] Infrastructure contains all persistence logic
- [ ] Infrastructure contains external service clients

### Repositories
- [ ] Repository for each aggregate root
- [ ] Repositories implement Application interfaces
- [ ] Repositories use ORM (Dapper/EF) internally
- [ ] Repositories don't leak ORM objects to other layers
- [ ] Repositories are simple (CRUD + Specifications)
- [ ] Complex queries use Specification pattern

### DbContext (EF)
- [ ] DbContext in Infrastructure, not Domain
- [ ] Entity configurations in separate files
- [ ] Fluent API used for configuration
- [ ] No business logic in DbContext
- [ ] Migrations managed in Infrastructure

### Dapper Setup
- [ ] Connection strings in Infrastructure
- [ ] SQL queries in repository implementations
- [ ] Parameters properly bound (prevent SQL injection)
- [ ] Async methods used throughout

### External Services
- [ ] External API clients in Infrastructure
- [ ] Interfaces defined in Application
- [ ] Clients handle retries and timeouts
- [ ] Error handling converts external errors to domain exceptions

### Unit of Work (if used)
- [ ] UoW wraps multiple repositories
- [ ] UoW manages transaction boundaries
- [ ] UoW disposed properly
- [ ] Either UoW or SaveChanges on repository, not both

---

## Presentation Layer Audit

### Structure
- [ ] API/ or Web/ folder exists
- [ ] Presentation depends on Application + Infrastructure
- [ ] Controllers are thin (orchestration only)
- [ ] HTTP concerns isolated here

### Controllers
- [ ] Controllers inject Application services
- [ ] Controllers don't access repositories directly
- [ ] Controllers return DTOs, not domain entities
- [ ] Controllers use appropriate HTTP verbs (GET, POST, PUT, DELETE, PATCH)
- [ ] Controllers return proper status codes (201, 204, 400, 404, etc.)
- [ ] Controllers have error handling

### Routes
- [ ] Routes are consistent (/api/resource or resource/{id})
- [ ] Routes follow project convention (REST vs RESTful)
- [ ] Versioning strategy clear (v1, v2 in path)
- [ ] Query parameters for filtering/pagination

### Middleware
- [ ] Middleware in Presentation layer
- [ ] Global exception handler exists
- [ ] Logging middleware logs important events
- [ ] CORS/Auth middleware here if needed
- [ ] Pipeline order is correct

### Dependency Injection
- [ ] Services registered in Program.cs/Startup
- [ ] Scope lifetimes are correct (Transient/Scoped/Singleton)
- [ ] No direct instantiation of services (new)
- [ ] All dependencies injected via constructor

---

## SOLID Principles Audit

### Single Responsibility
- [ ] Each class has one reason to change
- [ ] Service doesn't handle multiple domains
- [ ] Repository doesn't contain business logic
- [ ] Controller doesn't do validation or calculations

### Open/Closed
- [ ] Adding new features doesn't require modifying existing classes
- [ ] New payment methods don't modify OrderService
- [ ] New repositories don't modify Application layer
- [ ] Use polymorphism and extension points

### Liskov Substitution
- [ ] All implementations honor interface contracts
- [ ] Derived classes can replace base classes
- [ ] No "fake" implementations that throw NotImplemented
- [ ] Behavior is consistent across implementations

### Interface Segregation
- [ ] Interfaces are focused, not fat
- [ ] Clients depend only on methods they use
- [ ] IOrderService not forcing all order operations
- [ ] Separated by concern (IOrderQuery vs IOrderCommand)

### Dependency Inversion
- [ ] High-level modules depend on abstractions
- [ ] Low-level modules (repositories) implement abstractions
- [ ] No direct instantiation of concrete classes
- [ ] Dependencies injected, not hardcoded

---

## Layering & Dependency Flow Audit

### Dependency Direction
- [ ] Presentation depends on Application
- [ ] Application depends on Domain only
- [ ] Infrastructure depends on Application (implements)
- [ ] No circular dependencies
- [ ] No skipping layers (Presentation → Infrastructure is wrong)

### Boundary Enforcement
- [ ] Domain layer imports only Domain
- [ ] Application layer imports Domain + Application only
- [ ] Infrastructure imports all (but implements Application)
- [ ] Presentation imports Application (services only)

### Project References
```
Domain
  ↑
Application (depends on Domain)
  ↑
Infrastructure (depends on Application + Domain, for implementations)
  ↑
Presentation (depends on Application only)
```

- [ ] References follow this pattern
- [ ] No backward references
- [ ] No sibling references (except testing)

---

## Testing Audit

### Test Organization
- [ ] Domain.Tests for entity and value object tests
- [ ] Application.Tests for service tests
- [ ] Infrastructure.Tests for repository tests
- [ ] Presentation.Tests for controller tests

### Unit Tests
- [ ] Test one thing per test
- [ ] AAA pattern (Arrange-Act-Assert)
- [ ] Meaningful test names
- [ ] Mocks for dependencies
- [ ] No test interdependencies

### Integration Tests
- [ ] Test multiple layers together
- [ ] Use real database (or in-memory)
- [ ] Test important flows end-to-end
- [ ] Clean up after tests

### Test Coverage
- [ ] Domain entities tested
- [ ] Services tested with mocks
- [ ] Repository implementations tested
- [ ] Controllers tested with mocked services
- [ ] Happy path and error cases covered

---

## Code Quality Audit

### Naming
- [ ] Classes and methods have clear, intent-revealing names
- [ ] Abbreviations avoided (except well-known acronyms)
- [ ] Names follow C# conventions (PascalCase for classes, camelCase for variables)
- [ ] Interface names start with I

### Comments
- [ ] No comments explaining what code does (code is clear)
- [ ] Comments explain WHY (non-obvious decisions)
- [ ] Comments are accurate and updated
- [ ] No commented-out code

### Code Style
- [ ] Consistent formatting
- [ ] Lines not excessively long
- [ ] Methods are reasonably sized
- [ ] Classes have single responsibility

### Error Handling
- [ ] Exceptions used for exceptional cases
- [ ] Specific exceptions, not generic Exception
- [ ] Error messages are helpful
- [ ] All paths have error handling

---

## Data Access Audit

### If Using Dapper:
- [ ] Queries use parameters (prevent SQL injection)
- [ ] No hardcoded connection strings
- [ ] Connection pooling leveraged
- [ ] Async methods used
- [ ] Specifications for complex queries

### If Using Entity Framework:
- [ ] Code-First migrations
- [ ] DbContext properly configured
- [ ] Lazy loading disabled (explicit loading instead)
- [ ] Change tracking understood
- [ ] n+1 query problems identified

### Generic:
- [ ] Repository pattern used
- [ ] Data access logic not in domain or application
- [ ] Complex queries use Specification pattern
- [ ] Database calls are async

---

## API Style Audit

### If REST (Action-Based):
- [ ] Actions are clear and intuitive
- [ ] URLs represent actions: `/orders/{id}/approve`
- [ ] POST used for actions
- [ ] Consistent URL patterns

### If RESTful (Resource-Based):
- [ ] Resources have unique URLs
- [ ] HTTP verbs used correctly (GET, POST, PUT, DELETE, PATCH)
- [ ] Standard CRUD for resources
- [ ] Proper status codes (201 Created, 204 No Content, etc.)
- [ ] Pagination for list endpoints
- [ ] Filtering with query parameters

### If GraphQL:
- [ ] Schema well-defined
- [ ] Resolvers handle queries
- [ ] Mutations for modifications
- [ ] Subscriptions for real-time (if used)

---

## Performance & Scalability Audit

### Database
- [ ] Indexes on frequently queried columns
- [ ] n+1 queries eliminated
- [ ] Pagination for large result sets
- [ ] Async queries throughout

### Caching (if needed)
- [ ] Cache invalidation strategy clear
- [ ] In-memory or distributed cache
- [ ] TTLs appropriate
- [ ] Cache busting handles updates

### Resilience (if calling external APIs)
- [ ] Polly policies configured
- [ ] Retry with exponential backoff
- [ ] Circuit breaker protects from cascading failures
- [ ] Timeouts prevent hanging

### Logging
- [ ] Informational logs at service boundaries
- [ ] Warning logs for retries/timeouts
- [ ] Error logs for exceptions
- [ ] Performance logging if needed

---

## Security Audit

- [ ] Input validation in Application layer
- [ ] SQL injection prevented (parameterized queries)
- [ ] Authentication/Authorization checked
- [ ] Sensitive data not logged
- [ ] HTTPS enforced in production
- [ ] CORS configured appropriately
- [ ] Secrets not in code (use configuration)

---

## Scoring

Count your checkmarks:

- **90-100%** ✅ Excellent - Architecture is solid
- **75-89%** ⚠️ Good - Fix identified issues
- **60-74%** 🔴 Fair - Significant refactoring needed
- **<60%** 🔴 Poor - Consider major restructuring

---

## Common Issues Found

### High-Risk Issues 🔴
- Domain references DbContext or EF
- Controllers access repositories directly
- Business logic in repositories
- No validation in services
- Circular dependencies between layers

### Medium-Risk Issues ⚠️
- Fat interfaces violating ISP
- Services doing too much (multiple concerns)
- Inconsistent error handling
- Missing Specification pattern for complex queries
- Tests don't follow AAA pattern

### Low-Risk Issues 💡
- Naming could be clearer
- Comments could explain more WHY
- Code style inconsistencies
- Missing XML documentation

---

## Next Steps After Audit

1. **Identify high-risk issues** - Fix these first
2. **Create issues** - Track medium-risk items
3. **Refactor incrementally** - Test coverage should be in place first
4. **Document decisions** - Why architecture choices were made
5. **Team alignment** - Ensure all developers understand patterns

---

**Remember**: Architecture is about dependencies and boundaries. The goal is to make the codebase easy to understand, test, and change.

