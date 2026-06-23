# Test Project Setup & Configuration

Proper setup to ensure tests are discovered, connected, and running correctly.

## Common Issues & Solutions

### Issue 1: Tests Not Discovered (Skipped)
**Cause**: Test project not added to solution file

**Solution**:
```bash
# Add test project to solution
dotnet sln add tests/YourApi.UnitTests/YourApi.UnitTests.csproj
dotnet sln add tests/YourApi.IntegrationTests/YourApi.IntegrationTests.csproj
```

### Issue 2: Build Errors in Tests
**Cause**: Missing project references

**Solution**:
```bash
# Add references to test projects
cd tests/YourApi.UnitTests
dotnet add reference ../../src/YourApi.Domain/YourApi.Domain.csproj
dotnet add reference ../../src/YourApi.Application/YourApi.Application.csproj
dotnet add reference ../../src/YourApi.Infrastructure/YourApi.Infrastructure.csproj
```

### Issue 3: Tests Can't Find Classes/Interfaces
**Cause**: Incorrect namespace or missing using statements

**Solution**: Ensure namespaces match project structure:
```csharp
// Domain layer
namespace YourApi.Domain.Entities { }
namespace YourApi.Domain.Interfaces { }

// Application layer
namespace YourApi.Application.Services { }
namespace YourApi.Application.DTOs { }

// Infrastructure layer
namespace YourApi.Infrastructure.Repositories { }

// Test layer
namespace YourApi.UnitTests.Services { }
namespace YourApi.IntegrationTests.Repositories { }
```

---

## Step-by-Step Setup Guide

### Step 1: Create Solution Structure

```bash
# Navigate to project root
cd YourApi

# Create folders
mkdir -p src/{Domain,Application,Infrastructure,Presentation}
mkdir -p tests/{UnitTests,IntegrationTests}

# Initialize solution
dotnet new sln -n YourApi
```

### Step 2: Create Projects

```bash
# Domain layer (no dependencies)
dotnet new classlib -n YourApi.Domain -o src/Domain
cd src/Domain && dotnet user-secrets set "SkipOpenApiGeneration" "true" && cd ../..

# Application layer (depends on Domain)
dotnet new classlib -n YourApi.Application -o src/Application
cd src/Application && dotnet user-secrets set "SkipOpenApiGeneration" "true" && cd ../..

# Infrastructure layer (depends on Domain + Application)
dotnet new classlib -n YourApi.Infrastructure -o src/Infrastructure
cd src/Infrastructure && dotnet user-secrets set "SkipOpenApiGeneration" "true" && cd ../..

# Presentation layer (API - depends on all)
dotnet new webapi -n YourApi.Presentation -o src/Presentation

# Test projects
dotnet new xunit -n YourApi.UnitTests -o tests/UnitTests
dotnet new xunit -n YourApi.IntegrationTests -o tests/IntegrationTests
```

### Step 3: Add Projects to Solution

```bash
# Add all projects to solution
dotnet sln add src/Domain/YourApi.Domain.csproj
dotnet sln add src/Application/YourApi.Application.csproj
dotnet sln add src/Infrastructure/YourApi.Infrastructure.csproj
dotnet sln add src/Presentation/YourApi.Presentation.csproj
dotnet sln add tests/UnitTests/YourApi.UnitTests.csproj
dotnet sln add tests/IntegrationTests/YourApi.IntegrationTests.csproj

# Verify
dotnet sln list
```

### Step 4: Add Project References

```bash
# Application depends on Domain
cd src/Application
dotnet add reference ../Domain/YourApi.Domain.csproj
cd ../..

# Infrastructure depends on Domain + Application
cd src/Infrastructure
dotnet add reference ../Domain/YourApi.Domain.csproj
dotnet add reference ../Application/YourApi.Application.csproj
cd ../..

# Presentation depends on all
cd src/Presentation
dotnet add reference ../Domain/YourApi.Domain.csproj
dotnet add reference ../Application/YourApi.Application.csproj
dotnet add reference ../Infrastructure/YourApi.Infrastructure.csproj
cd ../..

# Test projects depend on source projects
cd tests/UnitTests
dotnet add reference ../../src/Domain/YourApi.Domain.csproj
dotnet add reference ../../src/Application/YourApi.Application.csproj
dotnet add reference ../../src/Infrastructure/YourApi.Infrastructure.csproj
cd ../..

cd tests/IntegrationTests
dotnet add reference ../../src/Domain/YourApi.Domain.csproj
dotnet add reference ../../src/Application/YourApi.Application.csproj
dotnet add reference ../../src/Infrastructure/YourApi.Infrastructure.csproj
dotnet add reference ../../src/Presentation/YourApi.Presentation.csproj
cd ../..
```

### Step 5: Add Test Dependencies

```bash
# Unit Tests - Add Moq for mocking
cd tests/UnitTests
dotnet add package Moq
dotnet add package FluentAssertions
cd ../..

# Integration Tests - Add EF Core and test database
cd tests/IntegrationTests
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package FluentAssertions
cd ../..
```

### Step 6: Verify Setup

```bash
# Build entire solution
dotnet build

# List all test classes
dotnet test --list-tests

# Run all tests
dotnet test

# Run specific test project
dotnet test tests/UnitTests/YourApi.UnitTests.csproj

# Run with verbose output
dotnet test -v detailed
```

---

## Expected Project Structure

```
YourApi/
├── YourApi.sln                                    ← SOLUTION FILE
│
├── src/
│   ├── Domain/
│   │   ├── YourApi.Domain.csproj
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   └── Services/
│   │
│   ├── Application/
│   │   ├── YourApi.Application.csproj            ← References Domain
│   │   ├── Services/
│   │   ├── DTOs/
│   │   └── Interfaces/
│   │
│   ├── Infrastructure/
│   │   ├── YourApi.Infrastructure.csproj         ← References Domain + Application
│   │   ├── Repositories/
│   │   ├── Data/
│   │   └── Services/
│   │
│   └── Presentation/
│       ├── YourApi.Presentation.csproj           ← References all
│       ├── Controllers/
│       ├── Middleware/
│       ├── Program.cs
│       └── wwwroot/
│
└── tests/
    ├── UnitTests/
    │   ├── YourApi.UnitTests.csproj              ← References src projects
    │   ├── Services/
    │   ├── Repositories/
    │   └── appsettings.json
    │
    └── IntegrationTests/
        ├── YourApi.IntegrationTests.csproj       ← References all src
        ├── API/
        ├── Repositories/
        └── appsettings.json
```

---

## Project File (.csproj) Template

### Domain Project (No Dependencies)
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsingsEnabled>true</ImplicitUsingsEnabled>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- No dependencies except .NET framework -->

</Project>
```

### Application Project (Depends on Domain)
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsingsEnabled>true</ImplicitUsingsEnabled>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="../Domain/YourApi.Domain.csproj" />
  </ItemGroup>

</Project>
```

### Test Project (References Source Projects)
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsTestProject>true</IsTestProject>
    <ImplicitUsingsEnabled>true</ImplicitUsingsEnabled>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.6.6" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageReference Include="Moq" Version="4.20.0" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Domain/YourApi.Domain.csproj" />
    <ProjectReference Include="../../src/Application/YourApi.Application.csproj" />
    <ProjectReference Include="../../src/Infrastructure/YourApi.Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

---

## Test Discovery Checklist

Before running tests, verify:

### Solution Level
- [ ] Solution file (`.sln`) exists
- [ ] All projects added to solution: `dotnet sln list`
- [ ] Solution builds without errors: `dotnet build`

### Project Level
- [ ] Test project has `IsTestProject = true` in `.csproj`
- [ ] Test project has xunit NuGet package
- [ ] Test project references source projects: `dotnet list reference`
- [ ] No circular dependencies

### Test Level
- [ ] Test class inherits from nothing (xUnit doesn't require base class)
- [ ] Test methods decorated with `[Fact]` or `[Theory]`
- [ ] Test class is `public`
- [ ] Test methods are `public`
- [ ] Test namespace is not nested in unreachable location

### Namespace & Using Statements
```csharp
// CORRECT
namespace YourApi.UnitTests.Services;

using Xunit;
using Moq;
using YourApi.Domain.Entities;
using YourApi.Application.Services;

public class UserServiceTests
{
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsUserDto()
    {
        // Test code
    }
}

// WRONG - won't be discovered
namespace YourApi.UnitTests.Services.Deep.Nested.Folder; // Too nested

internal class UserServiceTests // Must be public
{
    private void CreateUser_WithValidData_ReturnsUserDto() // Must be public
    {
        // Won't run
    }
}
```

---

## Running Tests

### List All Tests
```bash
dotnet test --list-tests

# Output:
# YourApi.UnitTests.Services.UserServiceTests.CreateUser_WithValidData_ReturnsUserDto
# YourApi.UnitTests.Services.UserServiceTests.CreateUser_WithInvalidEmail_ThrowsException
# YourApi.IntegrationTests.API.UserControllerTests.CreateUser_WithValidPayload_Returns201Created
```

### Run All Tests
```bash
dotnet test

# Output:
# Passed  YourApi.UnitTests [2.5s]
# Passed  YourApi.IntegrationTests [5.2s]
# 
# Test Run Successful.
# Total tests: 42
# Passed: 42
# Failed: 0
# Skipped: 0
```

### Run Specific Test Project
```bash
dotnet test tests/UnitTests

# Only unit tests run
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~UserServiceTests"
```

### Run Specific Test Method
```bash
dotnet test --filter "FullyQualifiedName~UserServiceTests.CreateUser_WithValidData_ReturnsUserDto"
```

### Run with Verbose Output
```bash
dotnet test -v detailed

# Shows every test as it runs
```

### Run with Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover

# Generates coverage.opencover.xml
```

---

## Troubleshooting Test Discovery

### Tests Show as "Skipped"

**Check 1**: Projects in solution?
```bash
dotnet sln list

# Should show all projects including test projects
# If test projects missing, add them:
dotnet sln add tests/UnitTests/YourApi.UnitTests.csproj
```

**Check 2**: References correct?
```bash
cd tests/UnitTests
dotnet list reference

# Should show source projects referenced
# If missing, add references:
dotnet add reference ../../src/Domain/YourApi.Domain.csproj
```

**Check 3**: Tests discoverable?
```bash
dotnet test --list-tests

# Should list all tests
# If none listed, check:
# 1. Test classes are public
# 2. Test methods are public with [Fact] or [Theory]
# 3. xunit package installed
```

### Build Errors in Tests

**Error**: `CS0246: The type or namespace name 'User' could not be found`

**Solution**:
```csharp
// Add using statement
using YourApi.Domain.Entities;

// Or fully qualify
var user = new YourApi.Domain.Entities.User();
```

**Error**: `The project does not target a valid shared runtime`

**Solution**: Ensure .csproj targets correct framework:
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>  <!-- Must match other projects -->
</PropertyGroup>
```

---

## CI/CD Test Execution

### GitHub Actions Example
```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore
        run: dotnet restore
      
      - name: Build
        run: dotnet build
      
      - name: Test
        run: dotnet test --logger "trx" --collect:"XPlat Code Coverage"
      
      - name: Upload results
        uses: actions/upload-artifact@v3
        with:
          name: test-results
          path: '**/TestResults/**'
```

---

## Test Project Best Practices

✅ **DO**

- Keep test projects separate from source projects
- Name test projects `[ProjectName].Tests` or `[ProjectName].UnitTests`
- Add test projects to solution file
- Reference only the projects you're testing
- Use consistent namespacing
- Organize tests by layer/feature

❌ **DON'T**

- Mix test and source code in same project
- Nest test classes too deep in folders
- Make test classes internal
- Forget to add projects to solution
- Create circular dependencies
- Store test data in source projects

---

## Validation Script

```bash
#!/bin/bash
# validate-tests.sh

echo "🔍 Validating test setup..."

# Check solution file exists
if [ ! -f "YourApi.sln" ]; then
    echo "❌ Solution file not found"
    exit 1
fi

echo "✅ Solution file found"

# Check test projects in solution
if dotnet sln list | grep -q "UnitTests"; then
    echo "✅ Unit test project in solution"
else
    echo "❌ Unit test project NOT in solution"
    echo "   Run: dotnet sln add tests/UnitTests/YourApi.UnitTests.csproj"
fi

# Build solution
echo ""
echo "🔨 Building solution..."
if dotnet build; then
    echo "✅ Build successful"
else
    echo "❌ Build failed"
    exit 1
fi

# List tests
echo ""
echo "📋 Discovering tests..."
TEST_COUNT=$(dotnet test --list-tests --no-build | grep "::" | wc -l)
if [ $TEST_COUNT -gt 0 ]; then
    echo "✅ Found $TEST_COUNT tests"
else
    echo "❌ No tests found"
    exit 1
fi

# Run tests
echo ""
echo "🧪 Running tests..."
if dotnet test --no-build; then
    echo "✅ All tests passed"
else
    echo "❌ Some tests failed"
    exit 1
fi

echo ""
echo "✅ Validation complete!"
```

Run with:
```bash
chmod +x validate-tests.sh
./validate-tests.sh
```

---

## Summary

| Step | Command | Verify |
|------|---------|--------|
| Create solution | `dotnet new sln -n YourApi` | `YourApi.sln` exists |
| Create projects | `dotnet new classlib/webapi/xunit` | Folders created |
| Add to solution | `dotnet sln add ...` | `dotnet sln list` |
| Add references | `dotnet add reference ...` | `dotnet list reference` |
| Build | `dotnet build` | No errors |
| Discover tests | `dotnet test --list-tests` | Tests listed |
| Run tests | `dotnet test` | All passing |

---

**Tests are now properly connected and discoverable!** ✅
