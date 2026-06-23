# Behavior-Driven Development (BDD) with Gherkin & SpecFlow

Write tests in plain English that non-technical stakeholders can understand.

## What is BDD?

**BDD bridges the gap between:**
- ✅ Developers (write code)
- ✅ QA (test features)
- ✅ Product/Business (define requirements)

Everyone writes tests in **Gherkin language** - human-readable, not code!

## Gherkin Syntax

### Basic Structure

```gherkin
Feature: User Management
  As a user management system
  I want to create and manage users
  So that I can maintain user accounts

  Scenario: Create a valid user
    Given I have a valid user data
    When I create a new user
    Then the user should be created successfully
    And the user ID should be returned
```

### Gherkin Keywords

| Keyword | Purpose | Example |
|---------|---------|---------|
| `Feature:` | High-level feature description | Feature: User Management |
| `Scenario:` | Individual test case | Scenario: Create valid user |
| `Given` | Setup (precondition) | Given I have valid user data |
| `When` | Action (what happens) | When I create a new user |
| `Then` | Expected outcome | Then the user is created |
| `And` | Additional condition | And the user ID is returned |
| `But` | Negative condition | But the user is not deleted |
| `Scenario Outline:` | Parameterized scenarios | Multiple test cases with different data |
| `Examples:` | Test data for Scenario Outline | Table of inputs/outputs |

## Feature Files

### Simple Feature Example

```gherkin
# File: Features/UserCreation.feature

Feature: User Creation
  Scenario: Create user with valid data
    Given I have the following user data:
      | Property | Value           |
      | Name     | John Doe        |
      | Email    | john@example.com|
      | Age      | 30              |
    When I submit the user creation form
    Then the user should be created successfully
    And I should receive the user ID
    And an email should be sent to john@example.com

  Scenario: Create user with invalid email
    Given I have a user with email "invalid-email"
    When I submit the user creation form
    Then an error should be displayed
    And the message should say "Invalid email format"

  Scenario: Create duplicate user
    Given a user with email "john@example.com" already exists
    When I create a user with email "john@example.com"
    Then an error should be displayed
    And the message should say "User already exists"
```

### Scenario Outline (Parameterized)

```gherkin
Feature: Email Validation

  Scenario Outline: Validate email addresses
    Given I have an email address "<email>"
    When I validate the email
    Then the result should be <isValid>

    Examples:
      | email                 | isValid |
      | john@example.com      | true    |
      | jane.doe@example.co.uk| true    |
      | invalid.email         | false   |
      | @example.com          | false   |
      | user@.com             | false   |
```

### Data Tables

```gherkin
Feature: Order Management

  Scenario: Add multiple items to order
    Given I have an empty shopping cart
    When I add the following items:
      | Product | Quantity | Price |
      | Book    | 2        | 15.99 |
      | Pen     | 5        | 2.50  |
      | Paper   | 1        | 8.99  |
    Then the cart should have 3 items
    And the total should be 60.47
```

## SpecFlow Implementation

### Step 1: Install SpecFlow

```bash
dotnet add package SpecFlow
dotnet add package SpecFlow.NUnit  # or xUnit
dotnet add package SpecFlow.Tools.MsBuild.Generation
```

### Step 2: Create Feature File

**File: Features/UserCreation.feature**

```gherkin
Feature: User Management - Creation
  As a user management system
  I want to create new users
  So that I can manage user accounts

  Background:
    Given the user service is initialized

  Scenario: Create user with valid data
    Given I have valid user data:
      | Name  | Email              |
      | John  | john@example.com   |
    When I create a new user
    Then the user should be created successfully
    And the created user has the correct data

  Scenario: Create user with invalid email
    Given I have a user with invalid email "not-an-email"
    When I create a new user
    Then an error should be raised
    And the error message should contain "Invalid email"
```

### Step 3: Implement Step Definitions

**File: Steps/UserCreationSteps.cs**

```csharp
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;
using Xunit;

namespace MyApi.Specifications.Steps
{
    [Binding]
    public class UserCreationSteps
    {
        private readonly IUserService _userService;
        private CreateUserDto _userDto;
        private UserDto _createdUser;
        private Exception _exception;

        public UserCreationSteps(IUserService userService)
        {
            _userService = userService;
        }

        [Given("the user service is initialized")]
        public void GivenUserServiceInitialized()
        {
            // Service is already injected
            Assert.NotNull(_userService);
        }

        [Given("I have valid user data:")]
        public void GivenValidUserData(Table table)
        {
            // Extract data from table
            var row = table.Rows.First();
            _userDto = new CreateUserDto
            {
                Name = row["Name"],
                Email = row["Email"]
            };
        }

        [Given("I have a user with invalid email \"(.*)\"")]
        public void GivenUserWithInvalidEmail(string email)
        {
            _userDto = new CreateUserDto
            {
                Name = "Test User",
                Email = email
            };
        }

        [When("I create a new user")]
        public async Task WhenCreateNewUser()
        {
            try
            {
                _createdUser = await _userService.CreateUserAsync(_userDto);
            }
            catch (Exception ex)
            {
                _exception = ex;
            }
        }

        [Then("the user should be created successfully")]
        public void ThenUserCreatedSuccessfully()
        {
            Assert.Null(_exception);
            Assert.NotNull(_createdUser);
            Assert.True(_createdUser.Id > 0);
        }

        [Then("the created user has the correct data")]
        public void ThenUserHasCorrectData()
        {
            Assert.Equal(_userDto.Name, _createdUser.Name);
            Assert.Equal(_userDto.Email, _createdUser.Email);
        }

        [Then("an error should be raised")]
        public void ThenErrorRaised()
        {
            Assert.NotNull(_exception);
        }

        [Then("the error message should contain \"(.*)\"")]
        public void ThenErrorMessageContains(string expectedMessage)
        {
            Assert.Contains(expectedMessage, _exception.Message);
        }
    }
}
```

### Step 4: Register Bindings

**File: Hooks/ServiceHooks.cs**

```csharp
using TechTalk.SpecFlow;
using Microsoft.Extensions.DependencyInjection;

namespace MyApi.Specifications.Hooks
{
    [Binding]
    public class ServiceHooks
    {
        private static IServiceProvider _serviceProvider;

        [BeforeTestRun]
        public static void SetupDependencyInjection()
        {
            var services = new ServiceCollection();
            
            // Register services
            services.AddScoped<IUserRepository, InMemoryUserRepository>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IEmailService, MockEmailService>();
            
            _serviceProvider = services.BuildServiceProvider();
        }

        [BeforeScenario]
        public void BeforeScenario(ScenarioContext context)
        {
            // Make services available in steps
            foreach (var service in _serviceProvider.GetServices<object>())
            {
                context.Add(service);
            }
        }

        [AfterScenario]
        public void AfterScenario(ScenarioContext context)
        {
            // Cleanup if needed
            var repository = context.Get<IUserRepository>();
            if (repository is InMemoryUserRepository inMemory)
            {
                inMemory.Clear();
            }
        }
    }
}
```

## Advanced Gherkin Features

### Background (Setup Common Steps)

```gherkin
Feature: User Orders

  Background:
    Given the system is initialized
    And a user "john@example.com" exists
    And the user has a shopping cart

  Scenario: Place order with valid items
    When the user adds item "Book" with quantity 2
    And the user submits the order
    Then the order should be confirmed
    And the user should receive confirmation email
```

### Tags (Categorize Tests)

```gherkin
Feature: User Management

  @regression @critical
  Scenario: Create user with valid data
    # This test runs in regression and critical suites
    ...

  @smoke
  Scenario: User login
    # This is a smoke test
    ...

  @pending @wip
  Scenario: Advanced user search
    # Work in progress - don't run yet
    ...
```

**Run specific tests:**
```bash
dotnet test --filter "Category=regression"
dotnet test --filter "Category=smoke"
```

### Scenario Context (Share Data Between Steps)

```csharp
[Binding]
public class OrderSteps
{
    private readonly ScenarioContext _context;

    public OrderSteps(ScenarioContext context)
    {
        _context = context;
    }

    [When("I add item to cart")]
    public void AddItem()
    {
        var item = new OrderItem { Name = "Book" };
        _context["current_item"] = item;  // Store in context
    }

    [Then("the item should be in cart")]
    public void CheckItem()
    {
        var item = _context["current_item"] as OrderItem;
        Assert.NotNull(item);
    }
}
```

## BDD Best Practices

### ✅ DO

**Write from user perspective:**
```gherkin
✅ GOOD
Scenario: User receives order confirmation
  When I submit my order
  Then I should receive an email confirmation

❌ BAD
Scenario: Order confirmation logic
  When order service is called
  Then database is updated
```

**Use realistic data:**
```gherkin
✅ GOOD
Given I have a user with email "john@example.com"

❌ BAD
Given I have a user with email "test@test.com"
```

**Keep scenarios simple:**
```gherkin
✅ GOOD - One behavior per scenario
Scenario: Create user with valid data
  When I create a user
  Then the user is created

❌ BAD - Multiple behaviors mixed
Scenario: User lifecycle
  When I create a user
  And I update the user
  And I delete the user
  Then everything works
```

### Step Definition Reusability

```csharp
// Reusable steps
[Given("I have a user with email \"(.*)\"")]
public void UserWithEmail(string email)
{
    _userDto.Email = email;
}

[When("I (create|update|delete) the user")]
public void UserAction(string action)
{
    switch (action)
    {
        case "create":
            _result = _service.Create(_userDto);
            break;
        case "update":
            _result = _service.Update(_userDto);
            break;
        case "delete":
            _service.Delete(_userDto.Id);
            break;
    }
}
```

## Running BDD Tests

### Command Line

```bash
# Run all BDD tests
dotnet test

# Run specific feature
dotnet test --filter "FullyQualifiedName~UserCreation"

# Run tests with tag
dotnet test --filter "Category=smoke"

# Generate report
dotnet test --logger:html
```

### Test Explorer (Visual Studio)

- Tests appear in Test Explorer
- Can run individual scenarios
- Shows pass/fail status
- Can filter by tag

## Reporting

### Generate BDD Reports

```bash
# Install reporter
dotnet add package SpecFlow.Plus.LivingDocumentation

# Generate HTML report
dotnet test
# Report generated in: bin/reports/
```

## BDD vs Unit Testing

| Aspect | BDD (Gherkin) | Unit Tests (xUnit) |
|--------|---------------|--------------------|
| **Audience** | Business + Developers | Developers |
| **Language** | English (non-technical) | C# (technical) |
| **Scope** | User behavior | Code units |
| **Readability** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Stakeholder engagement** | High | Low |
| **Execution speed** | Slower | Fast |
| **Setup complexity** | Higher | Lower |
| **Debugging** | Harder | Easier |

## When to Use BDD

✅ **Use BDD when:**
- Complex business scenarios
- Non-technical stakeholders involved
- Behavior documentation needed
- Acceptance criteria from user stories
- Team benefits from shared language

❌ **Don't use BDD for:**
- Simple utilities (use unit tests)
- Internal implementation details
- Performance-critical tests
- Testing low-level algorithms

## Complete Example: Order Processing

**Features/OrderProcessing.feature:**

```gherkin
Feature: Order Processing
  As an e-commerce system
  I want to process customer orders
  So that customers can purchase items

  Scenario: Process valid order
    Given the user has items in cart:
      | Product | Quantity | Price |
      | Book    | 2        | 15.99 |
      | Pen     | 3        | 2.50  |
    When the user submits the order
    Then the order should be confirmed
    And the total should be 39.47
    And an order confirmation email should be sent

  Scenario Outline: Apply discount codes
    Given the user has a cart total of <total>
    When the user applies code <code>
    Then the discount should be <discount>
    And the final total should be <final>

    Examples:
      | total | code    | discount | final |
      | 50.00 | SAVE10  | 5.00     | 45.00 |
      | 100.00| SAVE20  | 20.00    | 80.00 |
      | 25.00 | SAVE10  | 2.50     | 22.50 |
```

**Steps/OrderProcessingSteps.cs:**

```csharp
[Binding]
public class OrderProcessingSteps
{
    private readonly IOrderService _orderService;
    private readonly ScenarioContext _context;

    public OrderProcessingSteps(IOrderService orderService, ScenarioContext context)
    {
        _orderService = orderService;
        _context = context;
    }

    [Given("the user has items in cart:")]
    public void AddItemsToCart(Table table)
    {
        var order = new Order();
        foreach (var row in table.Rows)
        {
            order.AddItem(
                row["Product"],
                int.Parse(row["Quantity"]),
                decimal.Parse(row["Price"])
            );
        }
        _context["order"] = order;
    }

    [When("the user submits the order")]
    public async Task SubmitOrder()
    {
        var order = _context["order"] as Order;
        var confirmation = await _orderService.ProcessOrderAsync(order);
        _context["confirmation"] = confirmation;
    }

    [Then("the order should be confirmed")]
    public void VerifyOrderConfirmed()
    {
        var confirmation = _context["confirmation"] as OrderConfirmation;
        Assert.NotNull(confirmation);
        Assert.True(confirmation.IsConfirmed);
    }

    [Then("the total should be (.*)")]
    public void VerifyTotal(decimal expectedTotal)
    {
        var order = _context["order"] as Order;
        Assert.Equal(expectedTotal, order.Total);
    }
}
```

---

**See Also**:
- [Unit Testing with Mocks](unit-testing-with-mocks.md)
- [Integration Testing](integration-testing.md)
- [SpecFlow Documentation](https://specflow.org/)
- [Gherkin Guide](https://cucumber.io/docs/gherkin/)
