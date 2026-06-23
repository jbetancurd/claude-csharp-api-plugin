# Test Coverage Validation & Ensuring Tests Cover Application

Ensure your tests properly cover the generated application code.

---

## Coverage Goals

```
Domain Layer:      85%+ coverage (most critical, pure logic)
Application Layer: 75%+ coverage (services, DTOs)
Infrastructure:    60%+ coverage (repositories, harder to test)
Presentation:      40%+ coverage (controllers, integration tested)

Overall Target: 70% code coverage
```

---

## What "Coverage" Means

### Line Coverage
```csharp
public class UserService
{
    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))  // Line 1 - Covered
            return false;                   // Line 2 - Covered
        
        return email.Contains("@");         // Line 3 - Covered
    }
}

[Fact]
public void IsValidEmail_WithValidEmail_ReturnsTrue()
{
    // Tests line 3 only
    // Coverage: 2/3 lines = 66%
}
```

### Branch Coverage
```csharp
public class OrderService
{
    public bool CanShip(Order order)
    {
        if (order.Status == OrderStatus.Confirmed)      // Branch 1
        {
            return order.Items.Count > 0;               // Branch 2A - True
        }                                               // Branch 2B - False
        return false;
    }
}

// Test 1: Confirmed order with items → 2 branches
[Fact]
public void CanShip_ConfirmedWithItems_ReturnsTrue() { }

// Test 2: Confirmed order without items → 1 branch
[Fact]
public void CanShip_ConfirmedNoItems_ReturnsFalse() { }

// Test 3: Not confirmed → 1 branch
[Fact]
public void CanShip_NotConfirmed_ReturnsFalse() { }

// Coverage: 4/4 branches = 100%
```

---

## Step 1: Identify What to Test

### Map Application Structure to Tests

```
src/YourApi.Domain/
├── Entities/User.cs                    → tests/UnitTests/Domain/Entities/UserTests.cs
├── Entities/Order.cs                   → tests/UnitTests/Domain/Entities/OrderTests.cs
├── Services/OrderDomainService.cs      → tests/UnitTests/Domain/Services/OrderDomainServiceTests.cs
└── ValueObjects/Email.cs               → tests/UnitTests/Domain/ValueObjects/EmailTests.cs

src/YourApi.Application/
├── Services/UserApplicationService.cs  → tests/UnitTests/Application/UserApplicationServiceTests.cs
├── Services/OrderApplicationService.cs → tests/UnitTests/Application/OrderApplicationServiceTests.cs
├── DTOs/UserDto.cs                     → (Usually not tested, just containers)
└── Validators/CreateUserValidator.cs   → tests/UnitTests/Application/Validators/CreateUserValidatorTests.cs

src/YourApi.Infrastructure/
├── Repositories/UserRepository.cs      → tests/IntegrationTests/Repositories/UserRepositoryTests.cs
├── Repositories/OrderRepository.cs     → tests/IntegrationTests/Repositories/OrderRepositoryTests.cs
└── Services/EmailService.cs            → tests/UnitTests/Infrastructure/EmailServiceTests.cs (mock SMTP)

src/YourApi.Presentation/
├── Controllers/UsersController.cs      → tests/IntegrationTests/API/UsersControllerTests.cs
└── Controllers/OrdersController.cs     → tests/IntegrationTests/API/OrdersControllerTests.cs
```

---

## Step 2: Write Tests for Each Layer

### Domain Layer Tests (Unit)

```csharp
namespace YourApi.UnitTests.Domain.Entities;

using Xunit;
using YourApi.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Create_WithValidName_SetProperties()
    {
        // Test constructor
        var user = new User { Name = "John", Email = "john@example.com" };
        
        Assert.Equal("John", user.Name);
        Assert.Equal("john@example.com", user.Email);
    }

    [Fact]
    public void IsActive_WhenStatusActive_ReturnsTrue()
    {
        // Test property
        var user = new User { Status = UserStatus.Active };
        
        Assert.True(user.IsActive);
    }

    [Fact]
    public void Deactivate_SetsStatusToInactive()
    {
        // Test method
        var user = new User { Status = UserStatus.Active };
        
        user.Deactivate();
        
        Assert.Equal(UserStatus.Inactive, user.Status);
    }
}
```

### Application Layer Tests (Unit with Mocks)

```csharp
namespace YourApi.UnitTests.Application.Services;

using Xunit;
using Moq;
using YourApi.Application.Services;
using YourApi.Domain.Entities;
using YourApi.Domain.Repositories; // Mock this

public class UserApplicationServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly UserApplicationService _service;

    public UserApplicationServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _service = new UserApplicationService(_mockRepository.Object);
    }

    [Fact]
    public async Task CreateUser_WithValidData_CallsRepository()
    {
        // Arrange
        var dto = new CreateUserDto { Name = "John", Email = "john@example.com" };

        // Act
        await _service.CreateUserAsync(dto);

        // Assert - Verify interaction
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task GetUser_WithValidId_ReturnsUserDto()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John" };
        _mockRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(user);

        // Act
        var result = await _service.GetUserAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.Name);
    }
}
```

### Infrastructure Layer Tests (Integration)

```csharp
namespace YourApi.IntegrationTests.Repositories;

using Xunit;
using Microsoft.EntityFrameworkCore;
using YourApi.Infrastructure.Data;
using YourApi.Infrastructure.Repositories;
using YourApi.Domain.Entities;

public class UserRepositoryTests : IAsyncLifetime
{
    private readonly ApplicationDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        // Use in-memory database for tests
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new UserRepository(_context);
    }

    public async Task InitializeAsync()
    {
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
    }

    [Fact]
    public async Task AddUser_WithValidData_PersistsToDatabase()
    {
        // Arrange
        var user = new User { Name = "John", Email = "john@example.com" };

        // Act
        await _repository.AddAsync(user);

        // Assert - Verify in database
        var storedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "john@example.com");
        Assert.NotNull(storedUser);
        Assert.Equal("John", storedUser.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsUser()
    {
        // Arrange
        var user = new User { Name = "John", Email = "john@example.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.Name);
    }
}
```

### Presentation Layer Tests (Integration/API)

```csharp
namespace YourApi.IntegrationTests.API;

using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using YourApi.Presentation;

public class UsersControllerTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public UsersControllerTests()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Setup test database
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CreateUser_WithValidPayload_Returns201Created()
    {
        // Arrange
        var payload = new { name = "John", email = "john@example.com" };
        var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/users", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task GetUser_WithValidId_Returns200OK()
    {
        // Act
        var response = await _client.GetAsync("/api/users/1");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

---

## Step 3: Measure Coverage

### Install Coverage Tools

```bash
dotnet add package coverlet.collector
```

### Generate Coverage Report

```bash
# Generate coverage for all tests
dotnet test /p:CollectCoverage=true \
  /p:CoverageFormat=opencover \
  /p:CoverageDirectory="./coverage"

# Output: ./coverage/coverage.opencover.xml
```

### View Coverage Report

```bash
# Install ReportGenerator
dotnet tool install -g dotnet-reportgenerator-globaltool

# Generate HTML report
reportgenerator -reports:"./coverage/coverage.opencover.xml" \
  -targetdir:"./coverage-report" \
  -reporttypes:Html

# Open report
open coverage-report/index.html  # macOS
start coverage-report/index.html # Windows
```

---

## Step 4: Coverage Analysis

### Example Coverage Report

```
Project               Line Coverage    Branch Coverage    Method Coverage
────────────────────────────────────────────────────────────────────────
Domain                      92%               89%               95%
Application                 78%               72%               80%
Infrastructure              65%               60%               70%
Presentation               42%               35%               45%

OVERALL                     70%               65%               72%
```

### By File

```
UserService.cs             95%  ████████████████████ (19/20 lines)
OrderService.cs            82%  ██████████████████░░ (18/22 lines)
UserRepository.cs          60%  ████████████░░░░░░░░ (12/20 lines)
UsersController.cs         45%  █████████░░░░░░░░░░░ (9/20 lines)
```

---

## Step 5: Coverage Checklist

### Critical Path Testing

Ensure these are 100% covered:

- [ ] Entity constructors & properties
- [ ] Validation logic (all branches)
- [ ] Business rules (all conditions)
- [ ] Error handling (success + failures)
- [ ] Service methods (happy path + exceptions)
- [ ] Repository CRUD operations

### Nice-to-Have Testing

Aim for 80%+:

- [ ] Complex queries
- [ ] Edge cases
- [ ] Data transformations
- [ ] Integration points

### Optional (Lower Priority)

- [ ] Boilerplate code
- [ ] Simple getters/setters
- [ ] Configuration
- [ ] Controller routing (structural)

---

## Coverage Goals by Layer

### Domain Layer (Target: 85%+)

```csharp
// MUST TEST:
✅ Entity creation
✅ Property validation
✅ Business logic methods
✅ Value objects
✅ Domain services

// MAY SKIP:
❌ Auto-properties only
❌ Configuration constructors
❌ ToString() methods
```

Test count: 1-2 tests per method

### Application Layer (Target: 75%+)

```csharp
// MUST TEST:
✅ Service methods (happy path)
✅ Service methods (error cases)
✅ DTO mapping
✅ Validation rules
✅ Repository interactions

// MAY SKIP:
❌ Simple DTO classes
❌ AutoMapper profiles (if simple)
```

Test count: 2-3 tests per method

### Infrastructure Layer (Target: 60%+)

```csharp
// MUST TEST:
✅ Repository methods (with in-memory DB)
✅ Entity configurations
✅ Query logic
✅ Data access patterns

// MAY SKIP:
❌ Database migrations
❌ Connection strings
❌ External service calls (unless critical)
```

Test count: 2-3 tests per method

### Presentation Layer (Target: 40%+)

```csharp
// MUST TEST:
✅ API endpoints (happy path)
✅ API endpoints (error cases)
✅ Input validation
✅ Status codes

// MAY SKIP:
❌ Routing configuration
❌ Swagger generation
❌ Static middleware
```

Test count: 2 tests per endpoint

---

## Automated Coverage Validation

### GitHub Actions

```yaml
name: Coverage Check

on: [pull_request]

jobs:
  coverage:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Test with Coverage
        run: |
          dotnet test /p:CollectCoverage=true \
            /p:CoverageFormat=opencover \
            /p:Threshold=70 \
            /p:ThresholdType=line
      
      - name: Upload Coverage
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage/coverage.opencover.xml
          flags: unittests
          fail_ci_if_error: true
```

### Local Script

```bash
#!/bin/bash
# check-coverage.sh

echo "🧪 Running tests with coverage..."

dotnet test \
  /p:CollectCoverage=true \
  /p:CoverageFormat=opencover \
  /p:CoverageDirectory="./coverage"

# Check coverage percentage
COVERAGE=$(grep -oP '(?<=line-rate=")[^"]*' coverage/coverage.opencover.xml | head -1)

echo ""
echo "📊 Coverage: ${COVERAGE}%"

THRESHOLD=70

if (( $(echo "$COVERAGE >= $THRESHOLD" | bc -l) )); then
    echo "✅ Coverage meets threshold ($THRESHOLD%)"
    exit 0
else
    echo "❌ Coverage below threshold ($THRESHOLD%)"
    exit 1
fi
```

Run:
```bash
chmod +x check-coverage.sh
./check-coverage.sh
```

---

## Common Coverage Gaps

### Gap 1: Missing Happy Path Tests

```csharp
// PROBLEM: Only negative tests
[Fact]
public void CreateUser_WithEmptyName_ThrowsException() { }

[Fact]
public void CreateUser_WithEmptyEmail_ThrowsException() { }

// SOLUTION: Add happy path
[Fact]
public void CreateUser_WithValidData_ReturnsUserDto() { }  // ← ADD THIS!
```

### Gap 2: Missing Error Cases

```csharp
// PROBLEM: Only happy path
[Fact]
public void GetUser_WithValidId_ReturnsUser() { }

// SOLUTION: Add error cases
[Fact]
public void GetUser_WithInvalidId_ReturnsNull() { }        // ← ADD THIS!

[Fact]
public void GetUser_WithException_ThrowsRepositoryException() { }  // ← ADD THIS!
```

### Gap 3: Missing Branch Coverage

```csharp
public bool IsEligible(User user)
{
    if (user.Age >= 18)           // Branch 1
        return user.Status == UserStatus.Active;  // Branch 2A/2B
    return false;                 // Branch 1 false path
}

// PROBLEM: Only tests Age >= 18
[Fact]
public void IsEligible_Over18Active_ReturnsTrue() { }

// SOLUTION: Test all branches
[Fact]
public void IsEligible_Over18Active_ReturnsTrue() { }
[Fact]
public void IsEligible_Over18Inactive_ReturnsFalse() { }
[Fact]
public void IsEligible_Under18_ReturnsFalse() { }
```

---

## Test Documentation

Create a coverage map document:

```markdown
# Test Coverage Map

## Domain Layer

### User Entity
- [x] Constructor (1 test)
- [x] IsActive property (2 tests: true/false)
- [x] Deactivate() method (1 test)
- [x] Validate() method (4 tests: valid/invalid email/name)
**Coverage: 8 tests → 100%**

### Order Entity
- [x] Create order (2 tests: with/without items)
- [x] Calculate total (3 tests: no items/single/multiple)
**Coverage: 5 tests → 95%**

## Application Layer

### UserApplicationService
- [x] CreateUserAsync (3 tests: success/validation/repository error)
- [x] GetUserAsync (2 tests: found/not found)
- [x] UpdateUserAsync (3 tests: success/not found/validation)
**Coverage: 8 tests → 85%**

## Infrastructure Layer

### UserRepository
- [x] AddAsync (1 test)
- [x] GetByIdAsync (2 tests: found/not found)
- [x] UpdateAsync (1 test)
**Coverage: 4 tests → 70%**

## Presentation Layer

### UsersController
- [x] CreateUser endpoint (2 tests: 201/400)
- [x] GetUser endpoint (2 tests: 200/404)
**Coverage: 4 tests → 50%**

---

**Total: 29 tests across all layers**
**Overall Coverage: ~75%**
```

---

## Summary

| Layer | Target | Method | Priority |
|-------|--------|--------|----------|
| Domain | 85%+ | Unit tests (Moq) | CRITICAL |
| Application | 75%+ | Unit tests (Moq) | CRITICAL |
| Infrastructure | 60%+ | Integration tests (In-Memory DB) | HIGH |
| Presentation | 40%+ | Integration tests (WebApplicationFactory) | MEDIUM |

**Ensure every generated class has at least one test!** ✅
