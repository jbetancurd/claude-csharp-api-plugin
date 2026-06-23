# API Request Simulator - Console CLI Tool

Interactive console application to test all API workflows and endpoints.

## What is an API Simulator?

A standalone console application that provides:
- 📋 Interactive menu system for testing API flows
- 🔄 HTTP request builder with templates
- 💾 Request/response history and storage
- 📊 Performance metrics and testing results
- 📤 Export capabilities (JSON, CSV, Postman)
- 🔐 JWT token management
- ⚡ Concurrent request testing
- 🎯 Validation of responses

**Perfect for**:
- ✅ API-only services (no UI)
- ✅ Microservices testing
- ✅ Mobile app backend validation
- ✅ Documenting API workflows
- ✅ Load/stress testing
- ✅ Regression testing
- ✅ Simple, maintainable E2E testing

**Not ideal for**:
- ❌ Testing UI/browser interactions
- ❌ Visual regression testing
- ❌ Complex CSS selector testing

---

## Project Structure

```
src/
├── YourApi.Presentation/  ← Main API
└── YourApi.Simulator/     ← New simulator project
    ├── Menu/
    │   ├── MainMenu.cs
    │   ├── WorkflowMenu.cs
    │   └── AdminMenu.cs
    ├── Workflows/
    │   ├── UserWorkflow.cs
    │   ├── OrderWorkflow.cs
    │   ├── PaymentWorkflow.cs
    │   └── IWorkflow.cs
    ├── HttpClient/
    │   ├── ApiClient.cs
    │   ├── RequestBuilder.cs
    │   └── ResponseValidator.cs
    ├── Storage/
    │   ├── RequestHistory.cs
    │   └── ResultExporter.cs
    ├── Models/
    │   ├── ApiResponse.cs
    │   └── TestResult.cs
    ├── appsettings.json
    └── Program.cs
```

---

## Quick Start

### Step 1: Create Simulator Project

```bash
dotnet new console -n YourApi.Simulator
cd YourApi.Simulator
```

### Step 2: Add NuGet Packages

```bash
dotnet add package HttpClientFactory
dotnet add package Newtonsoft.Json
dotnet add package Spectre.Console  # Pretty console output
```

### Step 3: Create Basic Structure

**Program.cs**:
```csharp
using YourApi.Simulator;

var apiClient = new ApiClient("https://localhost:7000");
var simulator = new ApiSimulator(apiClient);

await simulator.RunAsync();
```

### Step 4: Run

```bash
dotnet run --project src/YourApi.Simulator
```

---

## Complete Example: User Simulator

### Main Menu

```csharp
// Menu/MainMenu.cs
using Spectre.Console;

public class MainMenu
{
    private readonly ApiSimulator _simulator;

    public MainMenu(ApiSimulator simulator)
    {
        _simulator = simulator;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("API Simulator")
                .Centered()
                .Color(Color.Cyan));

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold cyan]Select Workflow[/]")
                    .AddChoices(
                        "👤 User Management",
                        "📦 Order Processing",
                        "💳 Payment Processing",
                        "📊 View Results",
                        "📤 Export Results",
                        "❌ Exit"
                    )
            );

            switch (choice)
            {
                case "👤 User Management":
                    await RunUserWorkflowAsync();
                    break;
                case "📦 Order Processing":
                    await RunOrderWorkflowAsync();
                    break;
                // ... other cases
                case "❌ Exit":
                    return;
            }

            AnsiConsole.Press("[yellow]Press any key to continue...[/]");
        }
    }

    private async Task RunUserWorkflowAsync()
    {
        var workflow = new UserWorkflow(_simulator.ApiClient);
        
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]User Actions[/]")
                .AddChoices(
                    "Register New User",
                    "Login User",
                    "Get User Profile",
                    "Update User",
                    "Delete User",
                    "Back"
                )
        );

        switch (action)
        {
            case "Register New User":
                await workflow.RegisterAsync();
                break;
            case "Login User":
                await workflow.LoginAsync();
                break;
            // ... other cases
        }
    }
}
```

### User Workflow

```csharp
// Workflows/UserWorkflow.cs
using Spectre.Console;
using System.Net.Http.Json;

public class UserWorkflow : IWorkflow
{
    private readonly ApiClient _apiClient;

    public UserWorkflow(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task RegisterAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Heading("User Registration"));

        var email = AnsiConsole.Ask<string>("Email: ");
        var password = AnsiConsole.Ask<string>("Password: ", null);
        var name = AnsiConsole.Ask<string>("Name: ");

        var dto = new RegisterUserDto 
        { 
            Email = email, 
            Password = password, 
            Name = name 
        };

        var result = await _apiClient.PostAsync<UserResponseDto>(
            "/api/auth/register", 
            dto
        );

        DisplayResult(result);
    }

    public async Task LoginAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Heading("User Login"));

        var email = AnsiConsole.Ask<string>("Email: ");
        var password = AnsiConsole.Ask<string>("Password: ", null);

        var dto = new LoginDto { Email = email, Password = password };

        var result = await _apiClient.PostAsync<LoginResponseDto>(
            "/api/auth/login", 
            dto
        );

        if (result.IsSuccess && result.Data?.Token != null)
        {
            // Store JWT for subsequent requests
            _apiClient.SetBearerToken(result.Data.Token);
            
            AnsiConsole.MarkupLine("[green]✓ Login successful![/]");
            AnsiConsole.MarkupLine($"[cyan]Token saved for authenticated requests[/]");
        }

        DisplayResult(result);
    }

    public async Task GetProfileAsync()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Heading("Get User Profile"));

        var result = await _apiClient.GetAsync<UserProfileDto>("/api/users/me");
        DisplayResult(result);
    }

    private void DisplayResult<T>(ApiResponse<T> result)
    {
        AnsiConsole.WriteLine();
        
        if (result.IsSuccess)
        {
            AnsiConsole.MarkupLine("[green]✓ Request successful![/]");
            AnsiConsole.MarkupLine($"[cyan]Status Code:[/] {result.StatusCode}");
            AnsiConsole.MarkupLine($"[cyan]Response Time:[/] {result.ResponseTime}ms");
            
            if (result.Data != null)
            {
                AnsiConsole.MarkupLine("[cyan]Response:[/]");
                var json = JsonConvert.SerializeObject(result.Data, Formatting.Indented);
                AnsiConsole.Write(new Panel(json)
                    .Border(BoxBorder.Rounded)
                    .Padding(1, 1));
            }
        }
        else
        {
            AnsiConsole.MarkupLine("[red]✗ Request failed![/]");
            AnsiConsole.MarkupLine($"[red]Status Code:[/] {result.StatusCode}");
            AnsiConsole.MarkupLine($"[red]Error:[/] {result.ErrorMessage}");
        }
    }
}
```

### API Client

```csharp
// HttpClient/ApiClient.cs
using System.Net.Http.Json;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private string _bearerToken;

    public ApiClient(string baseUrl)
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = 
            (message, cert, chain, errors) => true; // Allow self-signed for dev

        _httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };
    }

    public void SetBearerToken(string token)
    {
        _bearerToken = token;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            stopwatch.Stop();

            var content = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<T>(content);

            return new ApiResponse<T>
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                Data = data,
                ResponseTime = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ApiResponse<T>
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ResponseTime = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object payload)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, payload);
            stopwatch.Stop();

            var content = await response.Content.ReadAsStringAsync();
            var data = response.IsSuccessStatusCode 
                ? JsonConvert.DeserializeObject<T>(content)
                : default;

            return new ApiResponse<T>
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                Data = data,
                ResponseTime = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ApiResponse<T>
            {
                IsSuccess = false,
                ErrorMessage = ex.Message,
                ResponseTime = stopwatch.ElapsedMilliseconds
            };
        }
    }
}
```

### Response Model

```csharp
// Models/ApiResponse.cs
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public T Data { get; set; }
    public string ErrorMessage { get; set; }
    public long ResponseTime { get; set; } // milliseconds
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

---

## Advanced Features

### Request History & Export

```csharp
// Storage/ResultExporter.cs
public class ResultExporter
{
    private readonly List<TestResult> _results = new();

    public void RecordResult(TestResult result)
    {
        _results.Add(result);
    }

    public async Task ExportToJsonAsync(string filePath)
    {
        var json = JsonConvert.SerializeObject(_results, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
        AnsiConsole.MarkupLine($"[green]✓ Exported to {filePath}[/]");
    }

    public async Task ExportToCsvAsync(string filePath)
    {
        var csv = "Timestamp,Endpoint,Method,StatusCode,ResponseTime,Result\n";
        
        foreach (var result in _results)
        {
            csv += $"{result.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                   $"{result.Endpoint}," +
                   $"{result.Method}," +
                   $"{result.StatusCode}," +
                   $"{result.ResponseTime}ms," +
                   $"{(result.IsSuccess ? "PASS" : "FAIL")}\n";
        }

        await File.WriteAllTextAsync(filePath, csv);
        AnsiConsole.MarkupLine($"[green]✓ Exported to {filePath}[/]");
    }

    public async Task ExportToPostmanAsync(string filePath)
    {
        var collection = new
        {
            info = new
            {
                name = "API Simulator Collection",
                schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            item = _results.Select(r => new
            {
                name = $"{r.Method} {r.Endpoint}",
                request = new
                {
                    method = r.Method,
                    url = new { raw = r.Endpoint },
                    header = new[] { new { key = "Authorization", value = "Bearer {{token}}" } }
                },
                response = new object[] { }
            }).ToArray()
        };

        var json = JsonConvert.SerializeObject(collection, Formatting.Indented);
        await File.WriteAllTextAsync(filePath, json);
        AnsiConsole.MarkupLine($"[green]✓ Postman collection exported to {filePath}[/]");
    }
}
```

### Concurrent Load Testing

```csharp
// Load Testing
public class LoadTester
{
    private readonly ApiClient _apiClient;

    public async Task RunLoadTestAsync(string endpoint, int concurrentRequests, int iterations)
    {
        AnsiConsole.MarkupLine($"[cyan]Running load test: {concurrentRequests} concurrent, {iterations} iterations[/]");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new List<Task>();
        var results = new System.Collections.Concurrent.ConcurrentBag<long>();

        for (int i = 0; i < concurrentRequests; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    await _apiClient.GetAsync<object>(endpoint);
                    sw.Stop();
                    results.Add(sw.ElapsedMilliseconds);
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        DisplayLoadTestResults(results, stopwatch.ElapsedMilliseconds);
    }

    private void DisplayLoadTestResults(
        System.Collections.Concurrent.ConcurrentBag<long> results, 
        long totalTime)
    {
        var sorted = results.OrderBy(r => r).ToList();
        var avg = sorted.Average();
        var min = sorted.Min();
        var max = sorted.Max();
        var p95 = sorted[(int)(sorted.Count * 0.95)];

        AnsiConsole.Write(new Table()
            .AddColumn("[cyan]Metric[/]")
            .AddColumn("[yellow]Value[/]")
            .AddRow("Total Requests", results.Count.ToString())
            .AddRow("Total Time", $"{totalTime}ms")
            .AddRow("Requests/sec", $"{(results.Count / (totalTime / 1000.0)):F2}")
            .AddRow("Avg Response Time", $"{avg}ms")
            .AddRow("Min Response Time", $"{min}ms")
            .AddRow("Max Response Time", $"{max}ms")
            .AddRow("P95 Response Time", $"{p95}ms")
        );
    }
}
```

### Response Validation

```csharp
// HttpClient/ResponseValidator.cs
public class ResponseValidator
{
    public static bool ValidateUserResponse(UserResponseDto user)
    {
        return !string.IsNullOrEmpty(user.Id) &&
               !string.IsNullOrEmpty(user.Email) &&
               !string.IsNullOrEmpty(user.Name);
    }

    public static bool ValidateOrderResponse(OrderResponseDto order)
    {
        return order.Id > 0 &&
               order.Items.Count > 0 &&
               order.Total > 0;
    }

    public static void ValidateAndDisplay(ApiResponse<object> response, string validation)
    {
        if (response.IsSuccess && response.Data != null)
        {
            AnsiConsole.MarkupLine("[green]✓ Validation passed[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]✗ Validation failed[/]");
        }
    }
}
```

---

## Interactive Menu Example

```
═══════════════════════════════════════════════════════════
               🚀 API SIMULATOR
═══════════════════════════════════════════════════════════

Select Workflow:

  ( ) 👤 User Management
  ( ) 📦 Order Processing
  ( ) 💳 Payment Processing
  ( ) 📊 View Results
  ( ) 📤 Export Results
  ( ) ❌ Exit

Select: 1

═══════════════════════════════════════════════════════════
            User Management
═══════════════════════════════════════════════════════════

Select Action:

  ( ) Register New User
  ( ) Login User
  ( ) Get User Profile
  ( ) Update User
  ( ) Delete User
  ( ) Back

Select: 1

═══════════════════════════════════════════════════════════
            Register New User
═══════════════════════════════════════════════════════════

Email: john@example.com
Password: ••••••••••••
Name: John Doe

[Sending request to POST /api/auth/register...]

✓ Request successful!
Status Code: 201
Response Time: 142ms
Response:
┌──────────────────────────────────┐
│ {                                │
│   "id": 123,                     │
│   "email": "john@example.com",   │
│   "name": "John Doe",            │
│   "createdAt": "2024-06-22..."   │
│ }                                │
└──────────────────────────────────┘

Press any key to continue...
```

---

## Running the Simulator

```bash
# Development
dotnet run --project src/YourApi.Simulator

# Production (headless, piped commands)
dotnet run --project src/YourApi.Simulator <<EOF
1
john@example.com
Password123!
EOF
```

---

## Advantages Over Playwright for APIs

| Aspect | Simulator | Playwright |
|--------|-----------|-----------|
| **Setup Time** | 5 minutes | 20 minutes |
| **Learning Curve** | Beginner | Intermediate |
| **Test Speed** | Very Fast | Slower (browser) |
| **API Testing** | ⭐⭐⭐⭐⭐ Perfect | ⭐⭐⭐ Via UI |
| **UI Testing** | ❌ No | ⭐⭐⭐⭐⭐ Perfect |
| **Menu-driven** | ✅ Yes | ❌ No |
| **Interactive** | ✅ Very | ❌ Scripted |
| **Documentation** | ✅ Self-docs | ❌ Scripts |
| **Export Results** | ✅ JSON/CSV/Postman | Limited |

---

## Best Practices

✅ **Organize workflows** by domain (User, Order, Payment)  
✅ **Reuse workflows** across different tests  
✅ **Validate responses** consistently  
✅ **Record history** for auditing  
✅ **Export results** for reporting  
✅ **Use pretty output** (Spectre.Console) for clarity  
✅ **Handle errors gracefully** with user-friendly messages  

---

## Integration with CI/CD

```bash
# Automated testing via stdin
echo -e "1\njohn@example.com\nPassword123!\n6" | \
  dotnet run --project src/YourApi.Simulator > test-results.log

# Check results
if grep -q "✓" test-results.log; then
  echo "Tests passed!"
else
  echo "Tests failed!"
  exit 1
fi
```

---

**See Also**:
- [E2E Testing with Playwright](playwright-guide.md) - For UI testing
- [Integration Testing Guide](../tdd-xunit/integration-testing.md)
- [Decision Tree Step 13](../decision-tree.md#step-13-e2e-testing--api-simulation-strategy)
