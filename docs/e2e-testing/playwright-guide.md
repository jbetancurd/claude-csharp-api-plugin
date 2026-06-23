# E2E Testing with Playwright

Browser automation testing for your web UI and API integration.

## What is Playwright?

Playwright is a framework for automating web browsers (Chrome, Firefox, Safari, Edge) for testing and automation.

**Perfect for**:
- ✅ Testing complete user workflows
- ✅ Cross-browser compatibility
- ✅ Visual regression testing
- ✅ API testing through UI
- ✅ Mobile browser testing

**Not ideal for**:
- ❌ API-only services (use API Simulator instead)
- ❌ Very simple apps (unit tests sufficient)
- ❌ Cost-sensitive projects (licensing)

---

## Installation

### Step 1: Install NuGet Package

```bash
dotnet add package Microsoft.Playwright
dotnet add package xunit
```

### Step 2: Install Browsers

```bash
pwsh bin/Debug/net8.0/playwright.ps1 install

# Or
playwright install
```

### Step 3: Create Test Project

```bash
dotnet new xunit -n YourApi.E2ETests
cd YourApi.E2ETests
dotnet add package Microsoft.Playwright
```

---

## Project Structure

```
tests/
├── YourApi.E2ETests/
│   ├── Pages/
│   │   ├── LoginPage.cs          ← Page Object Model
│   │   ├── DashboardPage.cs
│   │   └── OrderPage.cs
│   ├── Fixtures/
│   │   └── PlaywrightFixture.cs  ← Browser setup
│   ├── Workflows/
│   │   ├── UserRegistrationTests.cs
│   │   ├── OrderProcessingTests.cs
│   │   └── PaymentTests.cs
│   ├── appsettings.json
│   └── YourApi.E2ETests.csproj
```

---

## Basic Setup

### Playwright Fixture

```csharp
// Fixtures/PlaywrightFixture.cs
using Microsoft.Playwright;
using Xunit;

public class PlaywrightFixture : IAsyncLifetime
{
    private IBrowser _browser;
    public IPage Page { get; private set; }
    
    public string BaseUrl { get; } = "https://localhost:7000";

    public async Task InitializeAsync()
    {
        var playwright = await Playwright.CreateAsync();
        _browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,  // Set to false to see browser
        });
        
        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            BaseURL = BaseUrl,
        });
        
        Page = await context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await Page?.CloseAsync();
        await _browser?.CloseAsync();
    }
}
```

### Page Object Model (POM)

```csharp
// Pages/LoginPage.cs
using Microsoft.Playwright;

public class LoginPage
{
    private readonly IPage _page;

    public LoginPage(IPage page)
    {
        _page = page;
    }

    // Locators
    private ILocator EmailInput => _page.Locator("input[name='email']");
    private ILocator PasswordInput => _page.Locator("input[name='password']");
    private ILocator LoginButton => _page.Locator("button:has-text('Login')");
    private ILocator ErrorMessage => _page.Locator("[data-testid='error-message']");

    // Actions
    public async Task GotoAsync()
    {
        await _page.GotoAsync("/login");
    }

    public async Task LoginAsync(string email, string password)
    {
        await EmailInput.FillAsync(email);
        await PasswordInput.FillAsync(password);
        await LoginButton.ClickAsync();
    }

    // Assertions
    public async Task<bool> IsErrorMessageVisibleAsync()
    {
        return await ErrorMessage.IsVisibleAsync();
    }

    public async Task<string> GetErrorMessageAsync()
    {
        return await ErrorMessage.TextContentAsync();
    }

    public async Task WaitForNavigationAsync()
    {
        await _page.WaitForURLAsync("**/dashboard");
    }
}
```

---

## Test Examples

### Simple Login Test

```csharp
public class LoginTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;
    private readonly LoginPage _loginPage;

    public LoginTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
        _loginPage = new LoginPage(fixture.Page);
    }

    [Fact]
    public async Task Login_WithValidCredentials_SuccessfullyLogsIn()
    {
        // Arrange
        await _loginPage.GotoAsync();

        // Act
        await _loginPage.LoginAsync("user@example.com", "Password123!");

        // Assert
        await _fixture.Page.WaitForURLAsync("**/dashboard");
        await Expect(_fixture.Page).ToHaveTitleAsync(new Regex("Dashboard"));
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShowsError()
    {
        // Arrange
        await _loginPage.GotoAsync();

        // Act
        await _loginPage.LoginAsync("user@example.com", "WrongPassword");

        // Assert
        Assert.True(await _loginPage.IsErrorMessageVisibleAsync());
        var error = await _loginPage.GetErrorMessageAsync();
        Assert.Contains("Invalid credentials", error);
    }
}
```

### Complete User Workflow

```csharp
public class UserWorkflowTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;
    private readonly RegistrationPage _registrationPage;
    private readonly LoginPage _loginPage;
    private readonly DashboardPage _dashboardPage;

    [Fact]
    public async Task UserRegistrationAndLogin_CompleteFlow()
    {
        // Step 1: Register new user
        await _registrationPage.GotoAsync();
        await _registrationPage.RegisterAsync("newuser@example.com", "SecurePass123!");
        await _registrationPage.WaitForSuccessMessageAsync();

        // Step 2: Login with new account
        await _loginPage.GotoAsync();
        await _loginPage.LoginAsync("newuser@example.com", "SecurePass123!");
        await _loginPage.WaitForNavigationAsync();

        // Step 3: Verify dashboard
        Assert.True(await _dashboardPage.IsUserGreetingVisibleAsync());
        var greeting = await _dashboardPage.GetUserGreetingAsync();
        Assert.Contains("newuser@example.com", greeting);

        // Step 4: Create order (bonus workflow)
        await _dashboardPage.CreateOrderAsync("Product123", 2);
        Assert.True(await _dashboardPage.IsOrderCreatedMessageVisibleAsync());
    }
}
```

---

## Advanced Features

### Intercepting Network Requests

```csharp
[Fact]
public async Task Order_API_CallValidation()
{
    var apiRequests = new List<string>();
    
    // Listen to API calls
    _page.Request += (_, request) =>
    {
        if (request.ResourceType == "xhr" || request.ResourceType == "fetch")
        {
            apiRequests.Add(request.Url);
        }
    };

    // User creates order via UI
    await _dashboardPage.CreateOrderAsync("Product123", 1);

    // Verify correct API was called
    Assert.Contains(apiRequests, url => url.Contains("/api/orders"));
    
    // Verify request had correct payload
    // (This requires additional setup to capture request body)
}
```

### Mocking Network Responses

```csharp
[Fact]
public async Task Order_WithMockedAPI_ShowsError()
{
    // Mock API error response
    await _page.RouteAsync("**/api/orders", route =>
    {
        route.AbortAsync("failed");
    });

    // User tries to create order
    await _dashboardPage.GotoAsync();
    await _dashboardPage.ClickCreateOrderAsync();

    // Verify error message shown
    Assert.True(await _dashboardPage.IsErrorMessageVisibleAsync());
}
```

### Visual Regression Testing

```csharp
[Fact]
public async Task Dashboard_Screenshot_MatchesBaseline()
{
    await _dashboardPage.GotoAsync();
    
    // Take screenshot
    await _fixture.Page.ScreenshotAsync(new PageScreenshotOptions 
    { 
        Path = "screenshots/dashboard.png" 
    });

    // Compare with baseline
    // (Requires additional screenshot comparison library)
}
```

### Mobile Browser Testing

```csharp
public class MobileTests : IAsyncLifetime
{
    private IBrowser _browser;
    public IPage MobilePage { get; private set; }

    public async Task InitializeAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var iphone = playwright.Devices["iPhone 12"];
        
        _browser = await playwright.Chromium.LaunchAsync();
        var context = await _browser.NewContextAsync(iphone);
        MobilePage = await context.NewPageAsync();
    }

    [Fact]
    public async Task ResponsiveUI_LooksGoodOnMobile()
    {
        await MobilePage.GotoAsync("https://localhost:7000");
        // Test mobile-specific flows
    }

    public async Task DisposeAsync()
    {
        await MobilePage?.CloseAsync();
        await _browser?.CloseAsync();
    }
}
```

---

## Locator Strategies

```csharp
// By role
var button = page.Locator("role=button[name='Click me']");

// By test ID
var input = page.Locator("[data-testid='email-input']");

// By text
var link = page.Locator("text=Forgot password?");

// By CSS selector
var field = page.Locator(".form-input");

// By XPath
var element = page.Locator("xpath=//div[@id='myid']");

// By label
var input = page.Locator("label:has-text('Email') ~ input");

// Combining
var button = page.Locator("button:has-text('Login'):visible");
```

---

## Waits & Synchronization

```csharp
// Wait for element to be visible
await page.Locator("#load-button").WaitForAsync();

// Wait for navigation
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

// Wait for specific URL
await page.WaitForURLAsync("**/orders");

// Wait for function
await page.WaitForFunctionAsync("() => window.orderCount > 0");

// Explicit timeout
var element = await page.Locator(".hidden-element").WaitForAsync(
    new LocatorWaitForOptions { Timeout = 5000 }
);
```

---

## Debugging

### Run in Headed Mode (See Browser)

```csharp
_browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
{
    Headless = false,  // Shows browser
});
```

### Use Inspector

```bash
# Start inspector for interactive debugging
PWDEBUG=1 dotnet test

# Then use step-through in Inspector
```

### Screenshot on Failure

```csharp
[Fact]
public async Task Test_WithScreenshotOnFailure()
{
    try
    {
        // Test code
    }
    catch (Exception)
    {
        await _page.ScreenshotAsync(new PageScreenshotOptions 
        { 
            Path = $"screenshots/failure-{DateTime.Now:yyyyMMdd-HHmmss}.png" 
        });
        throw;
    }
}
```

### Trace Recording

```csharp
var traceFile = "trace.zip";

await context.Tracing.StartAsync(new TracingStartOptions
{
    Screenshots = true,
    Snapshots = true
});

// Run tests...

await context.Tracing.StopAsync(new TracingStopOptions 
{ 
    Path = traceFile 
});

// View trace: npx playwright show-trace trace.zip
```

---

## Best Practices

### ✅ DO

- **Use Page Object Model** - Encapsulate page logic
  ```csharp
  // Good
  await loginPage.LoginAsync(email, password);
  
  // Bad
  await page.FillAsync("input[name='email']", email);
  ```

- **Use data-testid attributes** - More stable than CSS selectors
  ```html
  <input data-testid="email-input" />
  ```

- **Wait for elements properly** - Use `.WaitForAsync()`, not Thread.Sleep
  ```csharp
  // Good
  await page.Locator(".modal").WaitForAsync();
  
  // Bad
  Thread.Sleep(2000);
  ```

- **Handle dynamic content** - Use appropriate waits
  ```csharp
  await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
  ```

### ❌ DON'T

- **Don't use Thread.Sleep**
  ```csharp
  // Bad - flaky and slow
  Thread.Sleep(5000);
  
  // Good - smart waiting
  await page.WaitForURLAsync("**/dashboard");
  ```

- **Don't hardcode waits**
  ```csharp
  // Bad
  await Task.Delay(3000);
  
  // Good
  await page.Locator(".loading").WaitForAsync(
      new LocatorWaitForOptions { State = WaitForSelectorState.Hidden }
  );
  ```

- **Don't test implementation details**
  ```csharp
  // Bad - tests HTML structure
  await page.Locator("div > div > span:nth-child(3)").ClickAsync();
  
  // Good - tests user visible behavior
  await page.Locator("role=button[name='Submit']").ClickAsync();
  ```

---

## Running Tests

### Run All E2E Tests

```bash
dotnet test YourApi.E2ETests

# Run specific test
dotnet test --filter "LoginTests"

# Run with verbose output
dotnet test -v detailed

# Run in headed mode (see browser)
HEADED=1 dotnet test
```

### CI/CD Integration

```bash
# Headless on CI
dotnet test --configuration Release

# Parallel execution
dotnet test -v detailed -parallel
```

---

## CI/CD Pipeline Example

```yaml
# .github/workflows/e2e-tests.yml
name: E2E Tests

on: [push, pull_request]

jobs:
  e2e:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Install Playwright browsers
        run: |
          dotnet tool install --global Microsoft.Playwright.CLI
          playwright install

      - name: Start API
        run: |
          dotnet run --project src/YourApi.Presentation &
          sleep 10  # Wait for API to start
      
      - name: Run E2E tests
        run: dotnet test YourApi.E2ETests
      
      - name: Upload screenshots on failure
        if: failure()
        uses: actions/upload-artifact@v3
        with:
          name: screenshots
          path: tests/YourApi.E2ETests/screenshots/
```

---

## Comparison: Playwright vs Others

| Tool | Language | Browsers | Mobile | Licensing | Best For |
|------|----------|----------|--------|-----------|----------|
| **Playwright** | C#, JS, Python | Chrome, Firefox, Safari, Edge | Yes | Free | Complex workflows, cross-browser |
| **Selenium** | Multiple | Chrome, Firefox, Safari, Edge | Yes | Free | Legacy support, wide compatibility |
| **Cypress** | JavaScript | Chrome, Firefox, Edge | Limited | Free & Paid | JavaScript heavy, single browser |
| **API Simulator** | C# | N/A | N/A | Free | API-only testing, simple scenarios |

---

## Resources

- [Playwright Documentation](https://playwright.dev/dotnet/)
- [Page Object Model Pattern](https://playwright.dev/dotnet/docs/pom)
- [Best Practices](https://playwright.dev/dotnet/docs/best-practices)
- [Debugging](https://playwright.dev/dotnet/docs/debug)
- [CI/CD Integration](https://playwright.dev/dotnet/docs/ci)

---

**See Also**:
- [API Request Simulator Guide](api-simulator-guide.md) - For API-only E2E testing
- [Integration Testing Guide](../tdd-xunit/integration-testing.md)
