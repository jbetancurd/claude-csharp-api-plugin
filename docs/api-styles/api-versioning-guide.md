# API Versioning Strategy Guide

Manage multiple API versions for backwards compatibility and evolution.

## Why API Versioning?

```
Without Versioning:
Client v1 → Breaking Change → App Crashes ❌

With Versioning:
Client v1 → /api/v1/... (unchanged) ✅
Client v2 → /api/v2/... (new features) ✅
```

---

## Versioning Strategies

### Strategy 1: URL Path Versioning (Recommended for REST)
```
GET /api/v1/users          ← Version 1
GET /api/v2/users          ← Version 2 (different response format)
GET /api/v3/users          ← Version 3 (new fields)
```

✅ **Pros**: 
- Explicit and clear
- Easy for caching (URL-based)
- Works with all HTTP methods
- Easy to test in browser

❌ **Cons**: 
- URL duplication
- More routing configuration

### Strategy 2: Query String Versioning
```
GET /api/users?version=1
GET /api/users?version=2
GET /api/users?version=3
```

✅ **Pros**: 
- Single URL path
- Optional (default to latest)

❌ **Cons**: 
- Harder to cache
- Less explicit
- Easy to miss the parameter

### Strategy 3: Header Versioning
```
GET /api/users
Header: Api-Version: 1

GET /api/users
Header: Api-Version: 2
```

✅ **Pros**: 
- Clean URL
- Standard HTTP approach

❌ **Cons**: 
- Hidden from URL
- Harder to test in browser
- Difficult to cache

### Strategy 4: Content Negotiation (Accept Header)
```
GET /api/users
Header: Accept: application/vnd.company.v1+json

GET /api/users
Header: Accept: application/vnd.company.v2+json
```

✅ **Pros**: 
- Most RESTful
- Single URL

❌ **Cons**: 
- Complex
- Requires custom media types

---

## Recommended: URL Path Versioning

Most REST APIs use URL path versioning for clarity:

```
/api/v1/users              ← User endpoints v1
/api/v1/orders             ← Order endpoints v1
/api/v2/users              ← User endpoints v2 (breaking changes)
/api/v2/orders             ← Order endpoints v2 (breaking changes)
/api/v3/users              ← User endpoints v3 (new features)
```

---

## Installation

### Step 1: Add NuGet Package

```bash
dotnet add package Asp.Versioning.Mvc.ApiExplorer
```

### Step 2: Configure in Program.cs

```csharp
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

var builder = WebApplicationBuilder.CreateBuilder(args);

// ============ API VERSIONING ============

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;  // Include version in response headers
        options.ApiVersionReader = new UrlSegmentApiVersionReader();  // /api/v#/...
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// ============ SWAGGER WITH VERSIONING ============

builder.Services.AddSwaggerGen(options =>
{
    // Define all API versions
    var provider = builder.Services.BuildServiceProvider()
        .GetRequiredService<IApiVersionDescriptionProvider>();

    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerDoc(
            description.GroupName,
            new OpenApiInfo
            {
                Title = "My API",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated
                    ? "This API version has been deprecated."
                    : "Current API version"
            }
        );
    }
});

// ... rest of configuration
```

---

## Controller Examples

### V1 Controller

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace YourApi.Presentation.Controllers
{
    /// <summary>
    /// User management endpoints (v1)
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/users")]
    public class UsersController : ControllerBase
    {
        /// <summary>
        /// Create a new user
        /// </summary>
        /// <remarks>
        /// v1: Basic user creation with name and email only
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserDtoV1>> CreateUser(CreateUserDtoV1 dto)
        {
            // Implementation for v1
            var user = new UserDtoV1 { Id = 1, Name = dto.Name, Email = dto.Email };
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserDtoV1>> GetUser(int id)
        {
            // Implementation for v1
            var user = new UserDtoV1 { Id = id, Name = "John", Email = "john@example.com" };
            return Ok(user);
        }

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<UserDtoV1>>> GetUsers()
        {
            var users = new List<UserDtoV1>
            {
                new UserDtoV1 { Id = 1, Name = "John", Email = "john@example.com" },
                new UserDtoV1 { Id = 2, Name = "Jane", Email = "jane@example.com" }
            };
            return Ok(users);
        }

        /// <summary>
        /// Delete user
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // Implementation
            return NoContent();
        }
    }
}
```

### V1 DTOs

```csharp
namespace YourApi.Application.DTOs
{
    /// <summary>
    /// User DTO v1 - Basic user information
    /// </summary>
    public class UserDtoV1
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    /// <summary>
    /// Create user request DTO v1
    /// </summary>
    public class CreateUserDtoV1
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
```

### V2 Controller (Enhanced)

```csharp
/// <summary>
/// User management endpoints (v2)
/// Enhanced with phone and address
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/users")]
public class UsersV2Controller : ControllerBase
{
    /// <summary>
    /// Create a new user
    /// </summary>
    /// <remarks>
    /// v2: Enhanced user creation with phone and address
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDtoV2>> CreateUser(CreateUserDtoV2 dto)
    {
        // Implementation for v2 with additional fields
        var user = new UserDtoV2
        {
            Id = 1,
            Name = dto.Name,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,  // NEW
            Address = dto.Address            // NEW
        };
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDtoV2>> GetUser(int id)
    {
        var user = new UserDtoV2
        {
            Id = id,
            Name = "John",
            Email = "john@example.com",
            PhoneNumber = "555-1234",       // NEW
            Address = "123 Main St"          // NEW
        };
        return Ok(user);
    }
}
```

### V2 DTOs

```csharp
/// <summary>
/// User DTO v2 - Extended with contact information
/// </summary>
public class UserDtoV2
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }    // NEW in v2
    public string Address { get; set; }        // NEW in v2
}

/// <summary>
/// Create user request DTO v2
/// </summary>
public class CreateUserDtoV2
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }    // NEW in v2
    public string Address { get; set; }        // NEW in v2
}
```

### V3 Controller (Deprecated V1)

```csharp
/// <summary>
/// User management endpoints (v3)
/// v1 endpoints marked as deprecated
/// </summary>
[ApiController]
[ApiVersion("3.0", Deprecated = true)]  // Mark v1 as deprecated
[Route("api/v{version:apiVersion}/users")]
public class UsersV1DeprecatedController : ControllerBase
{
    [HttpGet]
    [Obsolete("Use v2 or v3 endpoints instead")]
    public async Task<ActionResult<List<UserDtoV1>>> GetUsers()
    {
        // Still support v1 for backwards compatibility
        // but direct users to upgrade
        Response.Headers.Add("Deprecation", "true");
        Response.Headers.Add("Sunset", "Wed, 31 Dec 2025 23:59:59 GMT");
        
        return Ok(new List<UserDtoV1>());
    }
}

/// <summary>
/// User management endpoints (v3)
/// Latest version with additional features
/// </summary>
[ApiController]
[ApiVersion("3.0")]
[Route("api/v{version:apiVersion}/users")]
public class UsersV3Controller : ControllerBase
{
    // ... v3 implementation
}
```

---

## Swagger Configuration with Versioning

### Program.cs Setup

```csharp
var builder = WebApplicationBuilder.CreateBuilder(args);

// Add versioning
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

// Add Swagger with multiple versions
builder.Services.AddSwaggerGen(options =>
{
    var provider = builder.Services.BuildServiceProvider()
        .GetRequiredService<IApiVersionDescriptionProvider>();

    foreach (var description in provider.ApiVersionDescriptions)
    {
        options.SwaggerDoc(
            description.GroupName,
            new OpenApiInfo
            {
                Title = "My API",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated
                    ? "⚠️ This API version is deprecated. Please upgrade to the latest version."
                    : null,
                Contact = new OpenApiContact
                {
                    Name = "Support",
                    Email = "support@example.com"
                }
            }
        );
    }

    // Include XML comments for descriptions
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Use Swagger with versioned endpoints
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var description in provider.ApiVersionDescriptions.Reverse())
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant()
            );
        }

        options.RoutePrefix = string.Empty;
    });
}

app.MapControllers();
app.Run();
```

---

## Swagger UI Display

When you visit `http://localhost:5000/`:

```
Select API Version:
┌─────────────────────┐
│ [Version Dropdown]  │
│ V3 (Latest)         │
│ V2                  │
│ V1 (Deprecated) ⚠️   │
└─────────────────────┘

===== MY API - V3 =====

Endpoints:
├── POST /api/v3/users
├── GET  /api/v3/users
├── GET  /api/v3/users/{id}
└── DELETE /api/v3/users/{id}

===== Response Example (v3) =====
{
  "id": 1,
  "name": "John",
  "email": "john@example.com",
  "phoneNumber": "555-1234",
  "address": "123 Main St"
}
```

---

## Versioning Patterns

### Pattern 1: Major Versions Only
```
/api/v1/...  ← Breaking changes only
/api/v2/...
/api/v3/...
```

### Pattern 2: Major.Minor Versions
```
/api/v1.0/...  ← Features added (backwards compatible)
/api/v1.1/...
/api/v2.0/...  ← Breaking changes
/api/v2.1/...
```

### Pattern 3: Date-based Versions
```
/api/2024.01.15/...
/api/2024.02.10/...
/api/2024.03.01/...
```

---

## Migration Path

### Step 1: Release V2 with Deprecation Warning
```csharp
[ApiVersion("1.0", Deprecated = true)]
public class UsersV1Controller : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> GetUsers()
    {
        Response.Headers.Add("Deprecation", "true");
        Response.Headers.Add("Sunset", "Wed, 31 Dec 2025 23:59:59 GMT");
        Response.Headers.Add("Warning", "299 - \"API v1 is deprecated. Use v2 instead.\"");
        
        // Still return v1 data for backwards compatibility
        return Ok();
    }
}
```

### Step 2: Support Both Versions (18 months)
```
Clients using v1 → Still works, get deprecation warnings
Clients using v2 → Get new features, no warnings
```

### Step 3: Sunset V1 (After 18+ months)
```
Version 1.0 → REMOVED
Users MUST upgrade to v2+
```

---

## Testing Multiple Versions

```csharp
public class ApiVersioningTests
{
    private readonly HttpClient _client;

    [Theory]
    [InlineData("1.0")]
    [InlineData("2.0")]
    [InlineData("3.0")]
    public async Task GetUsers_WithAnyVersion_ReturnsOK(string version)
    {
        var response = await _client.GetAsync($"/api/v{version}/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_V1_ReturnsDeprecatedWarning()
    {
        var response = await _client.GetAsync("/api/v1/users");
        
        Assert.True(response.Headers.Contains("Deprecation"));
        Assert.Equal("true", response.Headers.GetValues("Deprecation").First());
    }

    [Fact]
    public async Task CreateUser_V2_IncludesNewFields()
    {
        var payload = new
        {
            name = "John",
            email = "john@example.com",
            phoneNumber = "555-1234",
            address = "123 Main St"
        };

        var response = await _client.PostAsJsonAsync("/api/v2/users", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

---

## Best Practices

✅ **DO**

- Start with v1.0
- Use URL path versioning for REST APIs
- Support 2-3 versions simultaneously
- Provide migration guides (v1 → v2)
- Use deprecation headers
- Document breaking changes clearly
- Version your DTOs separately
- Test all versions in CI/CD

❌ **DON'T**

- Create new major versions too frequently
- Break backwards compatibility without warning
- Forget to deprecate old versions
- Mix versions in same controller
- Duplicate entire controllers (use inheritance/composition)

---

## Response Headers

Clients see versioning info:

```
HTTP/1.1 200 OK
Content-Type: application/json
api-supported-versions: 1.0,2.0,3.0
api-deprecated-versions: 1.0
api-deprecated-versions: 2.0
```

---

## Real-World Example: GitHub API

```
GET /api/v3/repos/user/repo
GET /api/v4/graphql  ← GraphQL version
```

GitHub maintains v3 (REST) and v4 (GraphQL) simultaneously.

---

## Documentation Links

- [Asp.Versioning Documentation](https://github.com/dotnet/aspnet-api-versioning)
- [API Versioning Best Practices](https://semver.org/)
- [Deprecation Headers (RFC 7231)](https://tools.ietf.org/html/rfc7231)

---

**API versioning enables sustainable long-term API evolution!** ✅
