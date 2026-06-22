# Polly Resilience Patterns

Polly is a .NET library for handling transient faults and latency. Essential for microservices and distributed systems.

## Installation
```bash
dotnet add package Polly
dotnet add package Polly.Extensions.Http
```

## Core Patterns

### 1. Retry Policy

**Purpose**: Retry failed requests with exponential backoff.

```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt =>
            TimeSpan.FromSeconds(Math.Pow(2, attempt)), // 2s, 4s, 8s
        onRetry: (exception, duration, retryCount, context) =>
        {
            _logger.LogWarning(
                $"Retry {retryCount} after {duration.TotalSeconds}s due to: {exception.Message}");
        });

// Usage
var response = await retryPolicy.ExecuteAsync(() =>
    _httpClient.GetAsync("https://api.example.com/data"));
```

### 2. Circuit Breaker Pattern

**Purpose**: Fail fast when a service is unavailable, preventing cascade failures.

```csharp
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5, // Fail 5 times before breaking
        durationOfBreak: TimeSpan.FromSeconds(30), // Break for 30 seconds
        onBreak: (exception, duration, context) =>
        {
            _logger.LogError($"Circuit breaker opened for {duration.TotalSeconds}s");
        },
        onReset: (context) =>
        {
            _logger.LogInformation("Circuit breaker reset");
        });

// Usage - throws BrokenCircuitException if circuit is open
var response = await circuitBreakerPolicy.ExecuteAsync(() =>
    _httpClient.GetAsync("https://api.example.com/data"));
```

### 3. Timeout Policy

**Purpose**: Prevent hanging requests.

```csharp
var timeoutPolicy = Policy
    .TimeoutAsync<HttpResponseMessage>(
        timeout: TimeSpan.FromSeconds(10),
        timeoutStrategy: TimeoutStrategy.Optimistic);

// Usage
var response = await timeoutPolicy.ExecuteAsync(() =>
    _httpClient.GetAsync("https://api.example.com/data"));
```

### 4. Bulkhead (Isolation) Policy

**Purpose**: Isolate resources to prevent cascading failures.

```csharp
var bulkheadPolicy = Policy.BulkheadAsync(
    parallelization: 10, // Allow 10 concurrent calls
    queueingStrategy: 5, // Queue up to 5 more
    onBulkheadRejectedAsync: context =>
    {
        _logger.LogWarning("Bulkhead rejected request");
        return Task.CompletedTask;
    });

// Usage - throws BulkheadRejectedException if full
var response = await bulkheadPolicy.ExecuteAsync(() =>
    _httpClient.GetAsync("https://api.example.com/data"));
```

### 5. Fallback Policy

**Purpose**: Provide graceful degradation with a fallback value.

```csharp
var fallbackPolicy = Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .OrResult(r => !r.IsSuccessStatusCode)
    .FallbackAsync(async (context) =>
    {
        _logger.LogWarning("Using fallback response");
        return new HttpResponseMessage
        {
            Content = new StringContent(
                JsonConvert.SerializeObject(new { data = "cached" })),
            StatusCode = HttpStatusCode.OK
        };
    });

// Usage
var response = await fallbackPolicy.ExecuteAsync(() =>
    _httpClient.GetAsync("https://api.example.com/data"));
```

## Combining Policies (Wrap)

**Best Practice**: Combine multiple policies in order.

```csharp
var policy = Policy.WrapAsync(
    retryPolicy,              // Try again if fails
    circuitBreakerPolicy,     // Stop if too many failures
    timeoutPolicy,            // Timeout each attempt
    bulkheadPolicy,           // Limit concurrent requests
    fallbackPolicy            // Fallback if all else fails
);

// Usage - applies all policies
var response = await policy.ExecuteAsync(() =>
    _httpClient.GetAsync("https://api.example.com/data"));
```

## DI Setup

```csharp
public static IServiceCollection AddHttpClientWithPolly(
    this IServiceCollection services)
{
    services.AddHttpClient<ExternalApiClient>()
        .AddTransientHttpErrorPolicy(p =>
            p.WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt))))
        .AddTransientHttpErrorPolicy(p =>
            p.CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30)))
        .ConfigureHttpClient(c =>
        {
            c.Timeout = TimeSpan.FromSeconds(10);
        });
    
    return services;
}

// In Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClientWithPolly();
```

## Real-World Example: Resilient HTTP Client

```csharp
public class ResilientHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;
    private readonly ILogger<ResilientHttpClient> _logger;
    
    public ResilientHttpClient(
        HttpClient httpClient,
        ILogger<ResilientHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Define resilience policy
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutException>()
            .OrResult<HttpResponseMessage>(r =>
                r.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                r.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: LogRetry);
        
        var circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: LogBreak,
                onReset: LogReset);
        
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(10));
        
        _policy = Policy.WrapAsync(
            retryPolicy,
            circuitBreakerPolicy,
            timeoutPolicy);
    }
    
    public async Task<T> GetAsync<T>(string url)
    {
        try
        {
            var response = await _policy.ExecuteAsync(() =>
                _httpClient.GetAsync(url));
            
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError("Circuit breaker open", ex);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"HTTP request failed: {ex.Message}", ex);
            throw;
        }
    }
    
    private void LogRetry(
        DelegateResult<HttpResponseMessage> outcome,
        TimeSpan duration,
        int retryCount,
        Context context)
    {
        _logger.LogWarning(
            $"Retry {retryCount} after {duration.TotalSeconds}s. " +
            $"Reason: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
    }
    
    private void LogBreak(
        DelegateResult<HttpResponseMessage> outcome,
        TimeSpan duration,
        Context context)
    {
        _logger.LogError(
            $"Circuit breaker opened for {duration.TotalSeconds}s. " +
            $"Reason: {outcome.Exception?.Message ?? "too many failures"}");
    }
    
    private void LogReset(Context context)
    {
        _logger.LogInformation("Circuit breaker reset");
    }
}
```

## Health Checks with Resilience

```csharp
public class ResilienceHealthCheck : IHealthCheck
{
    private readonly IAsyncPolicy<HttpResponseMessage> _policy;
    private readonly HttpClient _httpClient;
    
    public ResilienceHealthCheck(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _policy = CreatePolicy();
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _policy.ExecuteAsync(async () =>
                await _httpClient.GetAsync("https://api.example.com/health"));
            
            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy("Service is healthy");
            else
                return HealthCheckResult.Degraded("Service returned non-success status");
        }
        catch (BrokenCircuitException)
        {
            return HealthCheckResult.Unhealthy("Service circuit breaker is open");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Service check failed: {ex.Message}");
        }
    }
    
    private IAsyncPolicy<HttpResponseMessage> CreatePolicy()
    {
        return Policy.WrapAsync(
            Policy.Handle<HttpRequestException>()
                .WaitAndRetryAsync(2, _ => TimeSpan.FromSeconds(1)),
            Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5)),
            Policy.Handle<HttpRequestException>()
                .CircuitBreakerAsync(3, TimeSpan.FromSeconds(10))
        );
    }
}
```

## Testing Resilience Policies

```csharp
public class ResilientHttpClientTests
{
    [Fact]
    public async Task GetAsync_WithTransientFailure_RetriesAndSucceeds()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.SetupResponse(
            new HttpResponseMessage(HttpStatusCode.ServiceUnavailable), // First: fails
            new HttpResponseMessage(HttpStatusCode.OK)                 // Second: succeeds
        );
        
        var httpClient = new HttpClient(handler) 
            { BaseAddress = new Uri("https://api.example.com") };
        var client = new ResilientHttpClient(httpClient, Mock.Of<ILogger<ResilientHttpClient>>());
        
        // Act
        var result = await client.GetAsync<dynamic>("https://api.example.com/data");
        
        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, handler.RequestCount);
    }
}
```

## Polly Best Practices

✅ **Always use timeouts** - Prevent hanging forever
✅ **Order policies correctly** - Timeout innermost, then circuit breaker, then retry
✅ **Log policy actions** - Understand what's happening
✅ **Monitor circuit breakers** - Alert when they open
✅ **Test failure scenarios** - Verify fallbacks work
✅ **Use exponential backoff** - Reduce load on failing service

❌ **Avoid**:
- No timeout (infinite wait)
- Too many retries (wasting time)
- Circuit breaker too sensitive (false positives)
- Logging every retry (noise)
- Synchronous Polly (deadlocks)

---

**See also**: [Caching Strategies](../performance/caching-strategies.md) for combining with cache-aside pattern.
