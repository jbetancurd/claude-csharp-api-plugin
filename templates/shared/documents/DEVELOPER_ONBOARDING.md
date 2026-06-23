# Developer Onboarding Guide

Welcome to [Project Name]! This guide will get you up and running with the project in about 30 minutes.

**Table of Contents**:
1. [Prerequisites](#prerequisites)
2. [Initial Setup](#initial-setup)
3. [Project Structure](#project-structure)
4. [Running the API](#running-the-api)
5. [First Development Task](#first-development-task)
6. [Common Commands](#common-commands)
7. [Code Style & Standards](#code-style--standards)
8. [Testing](#testing)
9. [Debugging](#debugging)
10. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software

- **[.NET 8.0 SDK](https://dotnet.microsoft.com/download)** or later
  ```bash
  dotnet --version  # Should show 8.0.0 or higher
  ```

- **Git**
  ```bash
  git --version
  ```

- **Visual Studio Code** or **Visual Studio 2022+**
  - C# extension (VS Code)
  - NuGet Package Manager

- **Database**
  - SQL Server Express (for development)
  - Or use Docker: `docker run -e 'ACCEPT_EULA=Y' mcr.microsoft.com/mssql/server`

### Optional but Recommended

- **Postman** or **REST Client** (for API testing)
- **Docker** (for containerized development)
- **Azure Data Studio** (SQL database explorer)

### System Requirements

- **RAM**: 8GB minimum (16GB recommended)
- **Disk Space**: 2GB for SDKs + dependencies
- **OS**: Windows, macOS, or Linux

---

## Initial Setup

### Step 1: Clone the Repository

```bash
git clone https://github.com/yourorg/[project-name].git
cd [project-name]
```

### Step 2: Restore Dependencies

```bash
dotnet restore
```

This downloads all NuGet packages. Takes 1-2 minutes first time.

### Step 3: Build the Solution

```bash
dotnet build
```

Should complete with no errors. If errors occur, see [Troubleshooting](#troubleshooting).

### Step 4: Create Database

```bash
# Apply migrations (creates database schema)
dotnet ef database update --project src/YourApi.Infrastructure --startup-project src/YourApi.Presentation
```

Or use the generated script:
```bash
# Windows
scripts/create-database.bat

# macOS/Linux
scripts/create-database.sh
```

### Step 5: Configure Environment

Copy `appsettings.example.json` to `appsettings.Development.json`:

```bash
cp src/YourApi.Presentation/appsettings.example.json src/YourApi.Presentation/appsettings.Development.json
```

Update connection string (if needed):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=YourApiDb;Trusted_Connection=true;"
  },
  "PORT": 5000
}
```

### Step 6: Verify Setup

```bash
dotnet run --project src/YourApi.Presentation
```

**Expected Output**:
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

**Visit in browser**:
- Swagger UI: http://localhost:5000/
- Health Check: http://localhost:5000/health

✅ **You're ready to develop!**

---

## Project Structure

```
[project-name]/
├── src/
│   ├── YourApi.Domain/                 ← Business logic (no dependencies)
│   │   ├── Entities/
│   │   ├── ValueObjects/
│   │   ├── Interfaces/
│   │   └── Services/
│   │
│   ├── YourApi.Application/            ← Use cases & orchestration
│   │   ├── Services/
│   │   ├── DTOs/
│   │   ├── Mappings/
│   │   ├── Specifications/
│   │   └── Interfaces/
│   │
│   ├── YourApi.Infrastructure/         ← Data access & external services
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Migrations/
│   │   │   └── Seeds/
│   │   ├── Repositories/
│   │   ├── Services/
│   │   └── Configuration/
│   │
│   └── YourApi.Presentation/           ← API endpoints & configuration
│       ├── Controllers/
│       ├── Middleware/
│       ├── Filters/
│       ├── Program.cs
│       ├── appsettings.json
│       └── appsettings.Development.json
│
├── tests/
│   ├── YourApi.UnitTests/              ← 70% of tests
│   │   ├── Services/
│   │   ├── Repositories/
│   │   └── Validators/
│   │
│   ├── YourApi.IntegrationTests/       ← 20% of tests
│   │   ├── API/
│   │   └── Services/
│   │
│   └── YourApi.AcceptanceTests/        ← 10% of tests (optional)
│       └── Workflows/
│
├── documents/                           ← This folder
│   ├── ARCHITECTURE.md
│   ├── DEVELOPER_ONBOARDING.md
│   └── PULL_REQUEST_TEMPLATE.md
│
├── scripts/
│   ├── create-database.sh
│   └── run-tests.sh
│
└── .github/
    ├── workflows/                       ← CI/CD pipelines
    └── PULL_REQUEST_TEMPLATE.md
```

### Understanding the Layers

**Domain Layer** (`src/YourApi.Domain/`)
- Pure business logic
- No frameworks, no dependencies
- Example: `User` entity, email validation

**Application Layer** (`src/YourApi.Application/`)
- Orchestrates use cases
- Calls repositories and services
- Example: `CreateUserService` calls `IUserRepository`

**Infrastructure Layer** (`src/YourApi.Infrastructure/`)
- Data access (repositories)
- External services (email, payment)
- Example: `UserRepository` implements `IUserRepository`

**Presentation Layer** (`src/YourApi.Presentation/`)
- API controllers
- Middleware, filters
- Example: `UsersController` calls `CreateUserService`

---

## Running the API

### Development Mode

```bash
# From project root
dotnet run --project src/YourApi.Presentation

# With watch mode (auto-restart on changes)
dotnet watch --project src/YourApi.Presentation run
```

### Using VS Code

1. **Open folder**: File → Open Folder → [project-name]
2. **Run & Debug**: Press `F5` or Ctrl+Shift+D
3. Click **Run** button
4. API starts with debugger attached

### Using Visual Studio

1. **Open solution**: File → Open → [project-name].sln
2. **Set startup project**: Right-click `YourApi.Presentation` → Set as Startup Project
3. **Press F5** to run with debugger
4. Browser opens to http://localhost:7000 (or configured port)

---

## First Development Task

### Example: Add a New Endpoint

#### Task: Create GET endpoint to list users

### Step 1: Create Domain Entity (if not exists)

**File**: `src/YourApi.Domain/Entities/User.cs`

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Step 2: Create Repository Interface

**File**: `src/YourApi.Application/Interfaces/IUserRepository.cs`

```csharp
public interface IUserRepository
{
    Task<List<User>> GetAllAsync();
    Task<User> GetByIdAsync(int id);
    Task AddAsync(User user);
}
```

### Step 3: Create Application Service

**File**: `src/YourApi.Application/Services/UserApplicationService.cs`

```csharp
public class UserApplicationService
{
    private readonly IUserRepository _repository;
    
    public UserApplicationService(IUserRepository repository)
    {
        _repository = repository;
    }
    
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await _repository.GetAllAsync();
        return users.Select(u => new UserDto 
        { 
            Id = u.Id, 
            Name = u.Name, 
            Email = u.Email 
        }).ToList();
    }
}
```

### Step 4: Implement Repository

**File**: `src/YourApi.Infrastructure/Repositories/UserRepository.cs`

```csharp
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    
    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }
    
    // Implement other methods...
}
```

### Step 5: Create Controller

**File**: `src/YourApi.Presentation/Controllers/UsersController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserApplicationService _service;
    
    public UsersController(UserApplicationService service)
    {
        _service = service;
    }
    
    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _service.GetAllUsersAsync();
        return Ok(users);
    }
}
```

### Step 6: Register in DI (Program.cs)

```csharp
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserApplicationService>();
```

### Step 7: Write Unit Test

**File**: `tests/YourApi.UnitTests/Services/UserApplicationServiceTests.cs`

```csharp
public class UserApplicationServiceTests
{
    [Fact]
    public async Task GetAllUsers_WithUsers_ReturnsUserList()
    {
        // Arrange
        var mockRepository = new Mock<IUserRepository>();
        mockRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<User> 
            { 
                new User { Id = 1, Name = "John" }
            });
        var service = new UserApplicationService(mockRepository.Object);
        
        // Act
        var result = await service.GetAllUsersAsync();
        
        // Assert
        Assert.Single(result);
        Assert.Equal("John", result[0].Name);
    }
}
```

### Step 8: Run and Test

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run API
dotnet run --project src/YourApi.Presentation

# Test in Swagger UI
# http://localhost:5000/
# Click on GET /api/users → Try it out → Execute
```

---

## Common Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test /p:CollectCoverage=true

# Create migration (EF Core)
dotnet ef migrations add MigrationName --project src/YourApi.Infrastructure

# Update database
dotnet ef database update --project src/YourApi.Infrastructure

# Run API
dotnet run --project src/YourApi.Presentation

# Run with watch (auto-reload)
dotnet watch --project src/YourApi.Presentation run

# Format code
dotnet format

# Clean build artifacts
dotnet clean

# Restore packages
dotnet restore
```

---

## Code Style & Standards

### Naming Conventions

```csharp
// Classes: PascalCase
public class UserService { }

// Methods: PascalCase
public Task<User> GetUserByIdAsync(int id) { }

// Parameters: camelCase
public void SetName(string newName) { }

// Private fields: _camelCase or camelCase
private readonly IRepository _repository;
private int _count;

// Constants: UPPER_SNAKE_CASE
private const int MAX_RETRIES = 3;

// Local variables: camelCase
var userName = "John";
```

### Async/Await

```csharp
// ✅ DO: Use async for I/O operations
public async Task<User> GetUserAsync(int id)
{
    return await _repository.GetByIdAsync(id);
}

// ❌ DON'T: Block on async
public User GetUser(int id)
{
    return _repository.GetByIdAsync(id).Result; // WRONG!
}

// ✅ DO: Async all the way
public async Task<ActionResult<UserDto>> CreateUser(CreateUserDto dto)
{
    var user = await _service.CreateUserAsync(dto);
    return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
}
```

### Null Checking

```csharp
// ✅ DO: Null-coalescing
var name = user?.Name ?? "Unknown";

// ✅ DO: Null-conditional
var email = user?.Email;

// ❌ DON'T: Verbose null checks
if (user != null && user.Email != null)
{
    var email = user.Email;
}
```

### SOLID Principles

```csharp
// ✅ Single Responsibility: One reason to change
public class UserValidator
{
    public bool IsValidEmail(string email) { }
}

// ❌ Multiple Responsibilities
public class UserService
{
    public bool IsValidEmail(string email) { } // Should be in validator
    public async Task<User> GetUserAsync(int id) { }
    public void LogError(string message) { } // Should be in logger
}

// ✅ Dependency Inversion: Depend on abstractions
public class UserService
{
    private readonly IUserRepository _repository;
    public UserService(IUserRepository repository) { }
}

// ❌ Concrete dependency
public class UserService
{
    private readonly SqlUserRepository _repository = new(); // WRONG!
}
```

### File Organization

```csharp
// File: User.cs
namespace YourApi.Domain.Entities;

using System;

/// <summary>
/// Represents a system user
/// </summary>
public class User
{
    // Properties
    public int Id { get; set; }
    public string Name { get; set; }
    
    // Constructors
    public User() { }
    
    // Methods
    public bool IsActive() { }
}
```

---

## Testing

### Unit Tests (Isolated, Fast)

```bash
# Run all unit tests
dotnet test tests/YourApi.UnitTests

# Run specific test
dotnet test --filter "FullyQualifiedName~UserServiceTests.GetUser"

# Run with verbose output
dotnet test -v detailed
```

### Integration Tests (With Database)

```bash
# Run integration tests
dotnet test tests/YourApi.IntegrationTests
```

### AAA Pattern

```csharp
[Fact]
public async Task CreateUser_WithValidData_ReturnsUserDto()
{
    // ARRANGE - Setup
    var mockRepository = new Mock<IUserRepository>();
    mockRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
        .ReturnsAsync(new User { Id = 1 });
    var service = new UserService(mockRepository.Object);
    
    // ACT - Perform action
    var result = await service.CreateUserAsync(new CreateUserDto { Name = "John" });
    
    // ASSERT - Verify result
    Assert.NotNull(result);
    Assert.Equal(1, result.Id);
}
```

---

## Debugging

### Using Breakpoints

1. Click line number to set breakpoint (red dot)
2. Press `F5` to run with debugger
3. Code stops at breakpoint
4. Inspect variables in Debug panel
5. Press `F10` to step over, `F11` to step into

### Using Debug Output

```csharp
// Log to debug output
Console.WriteLine($"User: {user.Name}");

// Or use Serilog
logger.Information("User {UserId} created", user.Id);
```

### Debugging Tests

```bash
# Run test with debugger
dotnet test --no-build -- RunConfiguration.DebuggerEnabled=true
```

### Common Issues

| Issue | Solution |
|-------|----------|
| Breakpoints not hit | Clean build: `dotnet clean && dotnet build` |
| Old code running | Rebuild solution |
| Database locked | Restart application |

---

## Troubleshooting

### Build Errors

**Error**: `The type or namespace name 'X' could not be found`

**Solution**:
```bash
# Restore packages
dotnet restore

# Rebuild
dotnet clean && dotnet build
```

### Database Errors

**Error**: `Cannot open database "YourApiDb" because it does not exist`

**Solution**:
```bash
# Create database from migrations
dotnet ef database update --project src/YourApi.Infrastructure --startup-project src/YourApi.Presentation
```

**Error**: `The connection string is not valid`

**Solution**: Check `appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=YourApiDb;Trusted_Connection=true;"
  }
}
```

### Port Already in Use

**Error**: `Address already in use: 0.0.0.0:5000`

**Solution**:
```bash
# Change port in appsettings.Development.json
{ "PORT": 5001 }

# Or kill process on port
# Windows: netstat -ano | findstr :5000
# macOS/Linux: lsof -i :5000 && kill -9 <PID>
```

### Test Failures

```bash
# Run single test with verbose output
dotnet test --filter "TestName" -v detailed

# Run with no build (faster)
dotnet test --no-build
```

### Swagger Shows "Loading API definition"

**Solution**: Ensure API is running and middleware is configured:
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = string.Empty;
    });
}
```

---

## Getting Help

### Resources

- **Architecture**: `documents/ARCHITECTURE.md`
- **Code Review**: See `PULL_REQUEST_TEMPLATE.md`
- **Testing**: `/docs/tdd-xunit/` guides
- **API Reference**: Swagger UI at http://localhost:5000/

### Questions?

1. **Check documentation** first
2. **Search closed issues** on GitHub
3. **Ask in team chat** (async communication)
4. **Create GitHub issue** with details

---

## Next Steps

1. ✅ Complete this onboarding
2. ✅ Run the API and explore Swagger
3. ✅ Run tests: `dotnet test`
4. ✅ Create your first branch: `git checkout -b feature/your-feature`
5. ✅ Implement your first feature (see [First Development Task](#first-development-task))
6. ✅ Write unit tests (70% coverage goal)
7. ✅ Submit pull request using `PULL_REQUEST_TEMPLATE.md`

---

**Welcome to the team! 🚀**

For questions or suggestions about this guide, reach out to [team-lead@example.com]
