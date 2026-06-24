# C# Version Features Guide

This guide explains how to leverage version-specific features in your API. All code examples follow Microsoft's C# Coding Conventions.

**🔗 Reference**: [Microsoft C# What's New & Version History](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history)

---

## C# 12 Feature Highlights

### 1. Primary Constructors (Game Changer for DI)

**Before (C# 11)**:
```csharp
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;
    
    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<User> GetUserAsync(int id)
        => await _repository.GetByIdAsync(id);
}
```

**After (C# 12)**:
```csharp
public class UserService(IUserRepository repository, ILogger<UserService> logger)
{
    public async Task<User> GetUserAsync(int id)
        => await repository.GetByIdAsync(id);
}
```

**Benefits**: Less boilerplate, parameters automatically become fields, cleaner code

---

### 2. Collection Expressions

**Before (C# 11)**:
```csharp
List<User> users = new() { user1, user2, user3 };
int[] numbers = new[] { 1, 2, 3, 4, 5 };
var merged = users.Concat(newUsers).ToList();
```

**After (C# 12)**:
```csharp
List<User> users = [user1, user2, user3];
int[] numbers = [1, 2, 3, 4, 5];
var merged = [..users, ..newUsers];
```

**Benefits**: More concise, spreads collections easily, works with any collection type

---

### 3. Inline Arrays (Performance)

```csharp
// Stack-allocated, no heap allocation
Span<int> buffer = [1, 2, 3, 4, 5];

// Perfect for high-performance APIs
public class DataProcessor
{
    public void ProcessBatch(ReadOnlySpan<int> data)
    {
        Span<int> temp = [..data];  // Inline array
        // Process without allocations
    }
}
```

---

## C# 11 Feature Highlights

### 1. Required Members

**Before (C# 10)**:
```csharp
public class User
{
    public int Id { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    
    // No validation - can create invalid users!
    var user = new User(); // Missing email and name!
}
```

**After (C# 11)**:
```csharp
public class User
{
    public required int Id { get; init; }
    public required string Email { get; init; }
    public required string Name { get; init; }
}

// Compiler enforces initialization:
// var user = new User() { }; // ERROR: Missing required properties
var user = new User { Id = 1, Email = "...", Name = "..." }; // ✅ Valid
```

**Benefits**: Compile-time validation, prevents invalid objects, self-documenting

---

### 2. File-Scoped Types

```csharp
// UserService.cs
public class UserService { }

// UserServiceHelper.cs
file class UserServiceHelper  // Only visible in this file
{
    public static void HelperMethod() { }
}

// Program.cs - Cannot access UserServiceHelper!
var service = new UserService();
// UserServiceHelper helper = new(); // ERROR: Not visible
```

**Benefits**: Better encapsulation, avoids namespace pollution

---

### 3. Raw String Literals

**Perfect for SQL, JSON, XML in code**:

```csharp
// SQL without escaping quotes
string query = """
    SELECT * FROM Users 
    WHERE Email = 'john@example.com' 
    AND CreatedDate > '2024-01-01'
""";

// JSON payload without escaping
string json = """
{
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com",
    "roles": ["admin", "user"]
}
""";

// XML configuration
string xml = """
<?xml version="1.0"?>
<configuration>
    <appSettings>
        <add key="api:timeout" value="30000" />
    </appSettings>
</configuration>
""";
```

**Benefits**: Readability, no escape character headaches, multiline support

---

## C# 10 Feature Highlights

### 1. Records (Perfect for DTOs & Data Transfer)

```csharp
// Immutable by default, value equality
public record UserDto(int Id, string Email, string Name);
public record CreateUserRequest(string Email, string Name);
public record UserResponse(int Id, string Email, string Name, DateTime CreatedAt);

// Usage in services
public class UserApplicationService
{
    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request)
    {
        // Create domain object from request
        var user = new User(request.Email, request.Name);
        
        // Return as record (immutable)
        return new UserResponse(
            user.Id, 
            user.Email, 
            user.Name, 
            user.CreatedAt
        );
    }
}
```

**Benefits**: No boilerplate, automatic Equals/GetHashCode, immutable by default

---

### 2. Init-Only Properties

```csharp
public class Order
{
    public int Id { get; init; }
    public string OrderNumber { get; init; }
    public DateTime CreatedAt { get; init; }
    public decimal Total { get; set; }  // Can be modified after creation
}

// Usage
var order = new Order 
{ 
    Id = 1, 
    OrderNumber = "ORD-001", 
    CreatedAt = DateTime.Now 
};

// Can modify mutable properties
order.Total = 99.99m;

// Cannot modify init-only properties
// order.Id = 2; // ERROR: Cannot assign to init-only property
```

**Benefits**: Immutable core data, mutable state, prevents accidental changes

---

### 3. File-Scoped Namespaces

**Before (C# 9)**:
```csharp
namespace MyApp.Services.Users
{
    public class UserService { }
    
    public class UserValidator { }
    
    public class UserRepository { }
}
// Deeply nested, adds indentation to entire file
```

**After (C# 10)**:
```csharp
file namespace MyApp.Services.Users;

public class UserService { }
public class UserValidator { }
public class UserRepository { }
```

**Benefits**: Less indentation, cleaner file structure, one statement at top

---

## Pattern Matching (All Modern Versions)

### C# 10+ Advanced Patterns

```csharp
public class OrderValidator
{
    public OrderStatus ValidateOrder(Order order) => order switch
    {
        // Property pattern
        { Total: <= 0 } => OrderStatus.Invalid,
        
        // Multiple conditions
        { Items.Count: 0, Status: OrderStatus.Pending } => OrderStatus.Invalid,
        
        // Type patterns with guards
        Order { CreatedDate: var date } when date < DateTime.Now.AddDays(-30) 
            => OrderStatus.Expired,
        
        // Negation
        { Email: not null } => OrderStatus.Valid,
        
        // Relational patterns
        { Total: > 1000 } => OrderStatus.RequiresApproval,
        
        _ => OrderStatus.Valid
    };
}
```

---

## Version-Specific Best Practices

### C# 12 API
```csharp
public class CreateUserService(
    IUserRepository userRepository,
    IValidator<CreateUserRequest> validator,
    ILogger<CreateUserService> logger)
{
    public async Task<UserResponse> ExecuteAsync(CreateUserRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);
        
        var user = new User(request.Email, request.Name);
        await userRepository.AddAsync(user);
        
        return new UserResponse(
            user.Id,
            user.Email,
            user.Name,
            user.CreatedAt
        );
    }
}
```

### C# 11 API
```csharp
public class CreateUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IValidator<CreateUserRequest> _validator;
    private readonly ILogger<CreateUserService> _logger;
    
    public CreateUserService(
        IUserRepository userRepository,
        IValidator<CreateUserRequest> validator,
        ILogger<CreateUserService> logger)
    {
        _userRepository = userRepository;
        _validator = validator;
        _logger = logger;
    }
    
    public async Task<UserResponse> ExecuteAsync(CreateUserRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);
        
        var user = new User { Email = request.Email, Name = request.Name };
        await _userRepository.AddAsync(user);
        
        return new UserResponse(
            user.Id,
            user.Email,
            user.Name,
            user.CreatedAt
        );
    }
}
```

---

## Performance Considerations by Version

| Aspect | C# 10 | C# 11 | C# 12 |
|--------|-------|-------|-------|
| **Struct Performance** | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ (inline arrays) |
| **Async Overhead** | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Allocations** | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Compiler Optimizations** | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |

---

## Migration Path

**From C# 9 → C# 10**:
- Add `required` to critical properties
- Convert DTO classes to records
- Use file-scoped namespaces

**From C# 10 → C# 11**:
- Add `required` keyword to enforce initialization
- Use file-scoped types for internal helpers
- Leverage raw string literals for JSON/SQL

**From C# 11 → C# 12**:
- Adopt primary constructors
- Use collection expressions
- Leverage inline arrays for performance

---

## Claude Code Behavior by Version

When you tell Claude your C# version, it will:

✅ **C# 12**: Use primary constructors, collection expressions, latest patterns  
✅ **C# 11**: Use required members, file-scoped types, raw string literals  
✅ **C# 10**: Use records, init-only properties, file-scoped namespaces  
✅ **C# 9**: Use records (basic), init properties, pattern matching  
✅ **C# 8**: Use nullable reference types, switch expressions only  

Always specify your version in the first step of the decision tree for optimal recommendations!
