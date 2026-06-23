# Unit Testing with Mocks

Isolate classes and test business logic without external dependencies using mocks.

## Why Unit Testing with Mocks?

✅ **Fast** - No database, no network, no I/O  
✅ **Isolated** - Test one class in isolation  
✅ **Reliable** - No external dependencies = no flaky tests  
✅ **Quick feedback** - Run 1000s of tests in seconds  
✅ **Easy to debug** - Clear failure reasons  

## AAA Pattern (Arrange-Act-Assert)

All unit tests follow this structure:

```csharp
[Fact]
public async Task CreateUser_WithValidData_ReturnsUserDto()
{
    // ARRANGE - Setup test data and mocks
    var mockRepository = new Mock<IUserRepository>();
    mockRepository
        .Setup(r => r.AddAsync(It.IsAny<User>()))
        .ReturnsAsync(new User { Id = 1, Name = "John" });
    
    var service = new UserService(mockRepository.Object);
    
    // ACT - Perform the action being tested
    var result = await service.CreateUserAsync(
        new CreateUserDto { Name = "John", Email = "john@example.com" }
    );
    
    // ASSERT - Verify the result
    Assert.NotNull(result);
    Assert.Equal(1, result.Id);
    Assert.Equal("John", result.Name);
}
```

## Mocking Libraries Comparison

### Moq (Most Popular)

```csharp
// Setup
var mock = new Mock<IUserRepository>();

// Returns specific value
mock.Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(new User { Id = 1, Name = "John" });

// Returns based on argument
mock.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
    .ReturnsAsync((int id) => new User { Id = id });

// Returns async
mock.Setup(r => r.SaveAsync())
    .Returns(Task.CompletedTask);

// Throws exception
mock.Setup(r => r.GetByIdAsync(0))
    .ThrowsAsync(new InvalidOperationException("ID not found"));

// Verify called
mockRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
```

**Install**: `dotnet add package Moq`

### NSubstitute (Simpler Syntax)

```csharp
// Setup
var substitute = Substitute.For<IUserRepository>();

// Returns value
substitute.GetByIdAsync(1)
    .Returns(new User { Id = 1, Name = "John" });

// Returns async
substitute.SaveAsync()
    .Returns(Task.CompletedTask);

// Throws exception
substitute.GetByIdAsync(0)
    .Throws(new InvalidOperationException("ID not found"));

// Verify called
substitute.Received(1).AddAsync(Arg.Any<User>());
```

**Install**: `dotnet add package NSubstitute`

### Comparison Table

| Feature | Moq | NSubstitute |
|---------|-----|------------|
| **Popularity** | ⭐⭐⭐⭐⭐ Most | ⭐⭐⭐⭐ Popular |
| **Syntax** | Fluent, verbose | Simpler, cleaner |
| **Learning** | Steeper curve | Easier to learn |
| **Setup** | `Setup()` method | Direct calls |
| **Verification** | `Verify()` method | `Received()` |
| **Documentation** | Extensive | Good |
| **Community** | Large | Smaller |

## Moq Examples

### Basic Setup

```csharp
var mockRepository = new Mock<IUserRepository>();

// Configure mock behavior
mockRepository
    .Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(new User { Id = 1, Name = "John" });

// Use in test
var service = new UserService(mockRepository.Object);
var result = await service.GetUserAsync(1);

// Verify was called
mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
```

### Matching Arguments

```csharp
// Any int
.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
    .ReturnsAsync(new User());

// Specific int
.Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(new User { Id = 1 });

// Matching condition
.Setup(r => r.GetAsync(It.Is<Expression<Func<User, bool>>>(
    e => e.Compile()(new User { Id = 1 })
)))
    .ReturnsAsync(new List<User>());

// Argument capture
var capturedUser = new User();
mockRepository
    .Setup(r => r.AddAsync(Capture.In(capturedUser)))
    .ReturnsAsync(1);

service.CreateUserAsync(newUser);
Assert.Equal(newUser.Name, capturedUser.Name);
```

### Return Values

```csharp
// Return fixed value
.Setup(r => r.GetByIdAsync(1))
    .ReturnsAsync(new User { Id = 1 });

// Return based on invocation
.Setup(r => r.GetByIdAsync(It.IsAny<int>()))
    .Returns((int id) => Task.FromResult(
        new User { Id = id, Name = $"User{id}" }
    ));

// Return different values on consecutive calls
.SetupSequence(r => r.GetByIdAsync(It.IsAny<int>()))
    .ReturnsAsync(new User { Id = 1 })
    .ReturnsAsync(new User { Id = 2 });

// Throw exception
.Setup(r => r.GetByIdAsync(0))
    .ThrowsAsync(new NotFoundException("User not found"));
```

### Verification

```csharp
// Verify called exactly once
mockRepository.Verify(
    r => r.AddAsync(It.IsAny<User>()), 
    Times.Once
);

// Verify called exactly 3 times
mockRepository.Verify(
    r => r.UpdateAsync(It.IsAny<User>()), 
    Times.Exactly(3)
);

// Verify never called
mockRepository.Verify(
    r => r.DeleteAsync(It.IsAny<int>()), 
    Times.Never
);

// Verify called at least once
mockRepository.Verify(
    r => r.GetByIdAsync(It.IsAny<int>()), 
    Times.AtLeastOnce
);

// Verify call order
var seq = new MockSequence();
mockRepository.InSequence(seq)
    .Setup(r => r.BeginTransaction())
    .Returns(Task.CompletedTask);
mockRepository.InSequence(seq)
    .Setup(r => r.AddAsync(It.IsAny<User>()))
    .ReturnsAsync(1);
mockRepository.InSequence(seq)
    .Setup(r => r.CommitAsync())
    .Returns(Task.CompletedTask);
```

## Complete Example: User Service Tests

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly UserService _service;
    
    public UserServiceTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockEmailService = new Mock<IEmailService>();
        _service = new UserService(_mockRepository.Object, _mockEmailService.Object);
    }
    
    // ============ CREATE USER TESTS ============
    
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsUserDto()
    {
        // Arrange
        var createDto = new CreateUserDto { Name = "John", Email = "john@example.com" };
        var createdUser = new User { Id = 1, Name = "John", Email = "john@example.com" };
        
        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<User>()))
            .ReturnsAsync(createdUser);
        
        // Act
        var result = await _service.CreateUserAsync(createDto);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.Name);
        Assert.Equal("john@example.com", result.Email);
        
        // Verify email was sent
        _mockEmailService.Verify(
            e => e.SendWelcomeEmailAsync("john@example.com"),
            Times.Once
        );
    }
    
    [Fact]
    public async Task CreateUser_WithEmptyName_ThrowsValidationException()
    {
        // Arrange
        var createDto = new CreateUserDto { Name = "", Email = "john@example.com" };
        
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateUserAsync(createDto)
        );
        
        // Verify repository was never called
        _mockRepository.Verify(
            r => r.AddAsync(It.IsAny<User>()),
            Times.Never
        );
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    public async Task CreateUser_WithInvalidEmail_ThrowsValidationException(string email)
    {
        // Arrange
        var createDto = new CreateUserDto { Name = "John", Email = email };
        
        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateUserAsync(createDto)
        );
    }
    
    // ============ UPDATE USER TESTS ============
    
    [Fact]
    public async Task UpdateUser_WithValidData_UpdatesSuccessfully()
    {
        // Arrange
        var userId = 1;
        var updateDto = new UpdateUserDto { Name = "Jane", Email = "jane@example.com" };
        var existingUser = new User { Id = userId, Name = "John", Email = "john@example.com" };
        var updatedUser = new User { Id = userId, Name = "Jane", Email = "jane@example.com" };
        
        _mockRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(existingUser);
        
        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(updatedUser);
        
        // Act
        var result = await _service.UpdateUserAsync(userId, updateDto);
        
        // Assert
        Assert.Equal("Jane", result.Name);
        Assert.Equal("jane@example.com", result.Email);
        
        // Verify repository methods called
        _mockRepository.Verify(r => r.GetByIdAsync(userId), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
    }
    
    [Fact]
    public async Task UpdateUser_WithNonExistentId_ThrowsNotFoundException()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((User)null);
        
        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _service.UpdateUserAsync(999, new UpdateUserDto())
        );
        
        // Verify update was never called
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
    }
    
    // ============ DELETE USER TESTS ============
    
    [Fact]
    public async Task DeleteUser_WithValidId_DeletesSuccessfully()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Name = "John" };
        
        _mockRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);
        
        _mockRepository
            .Setup(r => r.DeleteAsync(userId))
            .Returns(Task.CompletedTask);
        
        // Act
        await _service.DeleteUserAsync(userId);
        
        // Assert - Verify was called
        _mockRepository.Verify(r => r.DeleteAsync(userId), Times.Once);
    }
    
    // ============ GET USER TESTS ============
    
    [Fact]
    public async Task GetUser_WithValidId_ReturnsUserDto()
    {
        // Arrange
        var userId = 1;
        var user = new User { Id = userId, Name = "John", Email = "john@example.com" };
        
        _mockRepository
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(user);
        
        // Act
        var result = await _service.GetUserAsync(userId);
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.Name);
    }
}
```

## Best Practices

### ✅ DO

- **Test behavior, not implementation**
  ```csharp
  // Good - Test what the method does
  Assert.Equal("John", result.Name);
  
  // Bad - Testing implementation detail
  mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
  ```

- **Use meaningful test names**
  ```csharp
  // Good
  [Fact]
  public async Task CreateUser_WithValidData_ReturnsUserDto()
  
  // Bad
  [Fact]
  public async Task Test1()
  ```

- **Keep tests simple and focused**
  ```csharp
  // Good - One assertion per test
  [Fact]
  public void IsValidEmail_WithValidEmail_ReturnsTrue()
  {
      Assert.True(_validator.IsValidEmail("john@example.com"));
  }
  
  // Bad - Multiple unrelated assertions
  [Fact]
  public void ValidateUser_DoesEverything()
  {
      Assert.True(IsValidEmail(...));
      Assert.True(IsValidName(...));
      Assert.True(IsValidAge(...));
  }
  ```

- **Mock only external dependencies**
  ```csharp
  // Good - Mock repository (external)
  var mockRepository = new Mock<IUserRepository>();
  var service = new UserService(mockRepository.Object);
  
  // Bad - Mock internal logic
  var mockValidator = new Mock<EmailValidator>(); // Internal class!
  ```

- **Use Arrange-Act-Assert pattern**
  ```csharp
  // Good - Clear structure
  // ARRANGE
  var input = ...;
  
  // ACT
  var result = method(input);
  
  // ASSERT
  Assert.Equal(..., result);
  ```

### ❌ DON'T

- **Don't test getters and setters**
  ```csharp
  // Bad - Too simple, no value
  [Fact]
  public void Name_SetAndGet_ReturnsValue()
  {
      var user = new User();
      user.Name = "John";
      Assert.Equal("John", user.Name);
  }
  ```

- **Don't have shared state between tests**
  ```csharp
  // Bad - Test depends on another test's state
  private List<User> _users = new();
  
  [Fact]
  public void CreateUser_Runs() => _users.Add(new User());
  
  [Fact]
  public void GetUser_DependsOnCreate() => Assert.Single(_users); // WRONG!
  
  // Good - Each test is independent
  [Fact]
  public void CreateUser_Runs()
  {
      var users = new List<User>();
      users.Add(new User());
      Assert.Single(users);
  }
  ```

- **Don't over-verify**
  ```csharp
  // Bad - Verifying too many implementation details
  mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
  mockRepository.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once);
  mockRepository.Verify(r => r.SaveAsync(), Times.Once);
  
  // Good - Only verify critical behavior
  var result = await service.UpdateUserAsync(1, updateDto);
  Assert.NotNull(result);
  ```

- **Don't use `Thread.Sleep` or delays**
  ```csharp
  // Bad - Makes tests slow
  [Fact]
  public void EventuallyDoesWork()
  {
      Thread.Sleep(5000); // Why wait?
      Assert.True(...);
  }
  ```

## Test Coverage

### Good Coverage Distribution

```
🟢 Unit Tests: 70%
   ├─ Service layer
   ├─ Repository layer
   └─ Validators
   
🟡 Integration Tests: 20%
   ├─ Repository + Database
   ├─ Service + Repository
   └─ Middleware integration
   
🔴 End-to-End Tests: 10%
   ├─ API endpoints
   ├─ Full workflows
   └─ User scenarios
```

### Calculate Coverage

```bash
# Install OpenCover
dotnet add package OpenCover

# Generate coverage report
dotnet test /p:CollectCoverage=true
```

## Fixtures & Reusable Setup

### Test Fixtures

```csharp
public class UserServiceTestFixture : IDisposable
{
    public Mock<IUserRepository> MockRepository { get; }
    public Mock<IEmailService> MockEmailService { get; }
    public UserService Service { get; }
    
    public UserServiceTestFixture()
    {
        MockRepository = new Mock<IUserRepository>();
        MockEmailService = new Mock<IEmailService>();
        Service = new UserService(MockRepository.Object, MockEmailService.Object);
    }
    
    public void Dispose()
    {
        // Cleanup
    }
}

// Usage with IClassFixture
public class UserServiceTests : IClassFixture<UserServiceTestFixture>
{
    private readonly UserServiceTestFixture _fixture;
    
    public UserServiceTests(UserServiceTestFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsUserDto()
    {
        // Use _fixture.Service, _fixture.MockRepository
    }
}
```

### Custom Builders

```csharp
public class CreateUserDtoBuilder
{
    private string _name = "John";
    private string _email = "john@example.com";
    
    public CreateUserDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }
    
    public CreateUserDtoBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }
    
    public CreateUserDto Build()
    {
        return new CreateUserDto { Name = _name, Email = _email };
    }
}

// Usage
var dto = new CreateUserDtoBuilder()
    .WithName("Jane")
    .WithEmail("jane@example.com")
    .Build();
```

---

**See Also**:
- [BDD with Gherkin](bdd-gherkin.md)
- [Integration Testing](integration-testing.md)
- [Testing Patterns](testing-patterns.md)
