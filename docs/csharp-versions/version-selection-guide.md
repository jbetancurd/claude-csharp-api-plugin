# C# Version Selection Guide for Claude

**Purpose**: Help Claude ask about C# versions and tailor recommendations accordingly.

---

## Question to Ask First

When a user starts a C# API project, Claude should ask:

> **"Which C# version will you be targeting for this project?"**
> 
> - **C# 12** (Latest, .NET 9+) - Modern syntax, primary constructors, collection expressions
> - **C# 11** (LTS, .NET 8) - Required members, file-scoped types, raw string literals
> - **C# 10** (.NET 6+) - Records, init-only properties, file-scoped namespaces
> - **C# 9** (.NET 5/6) - Records (basic), init properties, pattern matching
> - **C# 8 or earlier** - Older projects, consider upgrading

---

## How to Apply Version Knowledge

### When Generating Code

**Check the version and apply appropriate patterns:**

```csharp
// If C# 12+ → Use primary constructors
public class UserService(IUserRepository repository)

// If C# 11+ → Use required members
public required string Email { get; init; }

// If C# 10+ → Use records for DTOs
public record UserDto(int Id, string Email, string Name);

// If C# 9+ → Use init-only properties
public int Id { get; init; }

// If C# 8+ → Use nullable reference types
string? optionalField = null;
```

### When Reviewing Code

Ask yourself:
- Is this code using the best features for the target version?
- Could this be simplified using newer C# features?
- Are there version-specific improvements possible?

Example:
```csharp
// User said C# 12, but this looks like C# 10 code:
public class UserService
{
    private readonly IUserRepository _repository;
    
    public UserService(IUserRepository repository)
    {
        _repository = repository;
    }
}

// Suggestion: Use primary constructors (C# 12):
public class UserService(IUserRepository repository)
{
    public async Task<User> GetAsync(int id) => await repository.GetByIdAsync(id);
}
```

### When Recommending Patterns

**Dependency Injection** (varies by version):
- C# 12: Primary constructor
- C# 11-10: Constructor with field assignment
- C# 9-8: Traditional constructor with property/field

**Immutable Data** (varies by version):
- C# 10+: Records
- C# 9: Records (basic support)
- C# 8 or earlier: init-only properties or explicit constructors

**Error Handling** (varies by version):
- C# 11+: Raw string literals for error messages
- C# 10-8: Regular string literals

---

## Version Feature Matrix for Quick Reference

| Feature | C# 8 | C# 9 | C# 10 | C# 11 | C# 12 |
|---------|------|------|-------|-------|-------|
| **Primary Constructors** | ❌ | ❌ | ❌ | ❌ | ✅ |
| **Collection Expressions** | ❌ | ❌ | ❌ | ❌ | ✅ |
| **Inline Arrays** | ❌ | ❌ | ❌ | ❌ | ✅ |
| **Required Members** | ❌ | ❌ | ❌ | ✅ | ✅ |
| **File-Scoped Types** | ❌ | ❌ | ❌ | ✅ | ✅ |
| **Raw String Literals** | ❌ | ❌ | ❌ | ✅ | ✅ |
| **Records** | ❌ | ✅ | ✅ | ✅ | ✅ |
| **Init-Only Properties** | ❌ | ✅ | ✅ | ✅ | ✅ |
| **File-Scoped Namespaces** | ❌ | ❌ | ✅ | ✅ | ✅ |
| **Pattern Matching** | Basic | ⭐ | ⭐⭐ | ⭐⭐ | ⭐⭐ |
| **Nullable Reference Types** | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## Examples by Version

### ServiceRegistration Example

**C# 12** (Most Modern):
```csharp
public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        => services
            .AddScoped<IUserService, UserService>()
            .AddScoped<IOrderService, OrderService>()
            .AddScoped<IProductService, ProductService>();
}
```

**C# 11** (Enterprise Standard):
```csharp
public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductService, ProductService>();
        return services;
    }
}
```

**C# 10**:
```csharp
public class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IProductService, ProductService>();
        return services;
    }
}
```

---

### DTO Example

**C# 12 & 11 & 10**:
```csharp
public record UserDto(int Id, string Email, string Name);
public record CreateUserRequest(string Email, string Name);
```

**C# 9**:
```csharp
public record UserDto(int Id, string Email, string Name);
// Records exist but are less polished
```

**C# 8 or earlier**:
```csharp
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
}
```

---

### Validation Example

**C# 11+** (Raw String Literals):
```csharp
var errorMessage = """
    User validation failed:
    - Email is required
    - Name must be at least 3 characters
    - Phone format is invalid
""";
```

**C# 10 and earlier**:
```csharp
var errorMessage = "User validation failed:\n- Email is required\n- Name must be at least 3 characters\n- Phone format is invalid";
```

---

## Upgrade Recommendations

If user says C# 8 or earlier:
- "I noticed you're targeting C# 8. Have you considered upgrading to C# 11 (LTS) or C# 12 (latest)? This would unlock modern features like records, required members, and primary constructors that significantly improve code clarity."

If user says C# 9:
- "C# 9 is still capable, but C# 10 added important features like file-scoped namespaces and better init-only properties. C# 11+ adds required members for better safety."

If user says C# 10:
- "C# 10 is solid. C# 11 (LTS) adds required members which are excellent for API DTOs, and C# 12 adds primary constructors for cleaner DI."

---

## Storage & Reference

Store the user's chosen version and reference it when:
- Generating new classes
- Creating service/repository templates
- Suggesting architectural improvements
- Reviewing code quality
- Recommending patterns

**Example Storage in Context**:
```
User's C# Version: 12
→ Use primary constructors in all services
→ Use collection expressions in data access
→ Use records for all DTOs
→ Recommend modern async patterns
```

---

## Resources to Reference

- [Microsoft C# Whats New](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/)
- [C# Version History](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history)
- Plugin Guide: `/docs/decision-tree.md` (Step 0)
- Feature Guide: `/docs/csharp-versions/csharp-version-features.md`
