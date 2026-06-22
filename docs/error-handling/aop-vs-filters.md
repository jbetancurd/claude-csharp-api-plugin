# Error Handling: AOP Decorators vs Filters

Two powerful patterns for cross-cutting concerns: Decorators (AOP) and Filters (HTTP-layer).

## Quick Comparison

| Aspect | AOP Decorator | Filter |
|--------|---------------|--------|
| **Layer** | Application (Business) | Presentation (HTTP) |
| **When it runs** | Service method call | HTTP pipeline |
| **Scope** | Any service/method | Controllers only |
| **Error type** | Any exception | Any exception |
| **Reusability** | Any service | Controllers only |
| **Testing** | Test without decorator | Need HTTP context |
| **Performance** | Direct wrapper call | HTTP pipeline processing |
| **Complexity** | Simple interface wrapping | Filter pipeline |

## AOP Decorator Pattern

### What It Does

Wraps a service with additional behavior (logging, error handling, metrics) without modifying the service.

```csharp
// Original service - pure business logic
public interface IOrderService
{
    Task<Order> CreateAsync(CreateOrderDto dto);
}

public class OrderService : IOrderService
{
    public async Task<Order> CreateAsync(CreateOrderDto dto)
    {
        // Just business logic
        var order = new Order(dto.CustomerId);
        await _repository.AddAsync(order);
        return order;
    }
}

// Decorator - adds logging
public class LoggingDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly ILogger _logger;

    public async Task<Order> CreateAsync(CreateOrderDto dto)
    {
        _logger.LogInformation("Creating order...");
        try
        {
            var result = await _inner.CreateAsync(dto);
            _logger.LogInformation("Order created");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order");
            throw;
        }
    }
}

// Usage: wrap the service
IOrderService service = new OrderService();
IOrderService decorated = new LoggingDecorator(service, logger);
```

### Pros ✅
- **Separation of concerns** - Service has only business logic
- **Testable** - Test service and decorator separately
- **Reusable** - Use same decorator for multiple services
- **Composable** - Stack multiple decorators
- **Explicit** - Clear dependency flow
- **No magic** - Easy to understand

### Cons ❌
- **Boilerplate** - Must implement interface for each decorator
- **Manual DI registration** - Need to wire up decorators
- **Per-service** - Each service needs its own decorator chain
- **Not automatic** - Must explicitly apply decorators

### When to Use

✅ **Choose Decorators when:**
- Building domain services
- Need fine-grained control over behavior
- Want to test service independently
- Sharing decorators across multiple services
- Building reusable cross-cutting concerns

## Filter Pattern

### What It Does

Intercepts HTTP requests/responses in the ASP.NET Core pipeline. Runs automatically for all controllers.

```csharp
// Global exception filter
public class GlobalExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger _logger;

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        var exception = context.Exception;
        _logger.LogError(exception, "Request failed");

        context.Result = new ObjectResult(new
        {
            message = "An error occurred",
            traceId = context.HttpContext.TraceIdentifier
        })
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };

        context.ExceptionHandled = true;
    }
}

// Register globally
builder.Services.AddControllers(options =>
    options.Filters.Add<GlobalExceptionFilter>());
```

### Pros ✅
- **Automatic** - Applies to all controllers without extra code
- **HTTP-focused** - Natural for API error handling
- **Built-in** - Part of ASP.NET Core pipeline
- **Less boilerplate** - Single registration for all controllers
- **Standardization** - Consistent error responses across API
- **Access to HTTP context** - Request/response metadata available

### Cons ❌
- **HTTP only** - Can't use for non-HTTP services
- **Tight coupling** - Tied to ASP.NET Core
- **Less testable** - Need HTTP context in tests
- **Pipeline complexity** - Harder to understand execution order
- **Limited reuse** - Only for controllers

### When to Use

✅ **Choose Filters when:**
- Handling HTTP-layer errors
- Returning standardized API responses
- Need consistent error handling across all endpoints
- Want automatic application to all controllers
- Building REST/GraphQL APIs

## Hybrid Approach (Recommended)

Use **both** for maximum benefit:

```
Service Layer (AOP Decorators)
    ↓
    Business logic errors → Decorator handles → throws application exception
    ↓
HTTP Layer (Filters)
    ↓
    Application exception → Filter catches → returns 400 Bad Request
```

### Example: Combined Implementation

```csharp
// 1. Domain exception
public class InvalidOrderException : Exception
{
    public InvalidOrderException(string msg) : base(msg) { }
}

// 2. Service with decorator (AOP)
public interface IOrderService
{
    Task<OrderDto> CreateAsync(CreateOrderDto dto);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repo;

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto)
    {
        if (string.IsNullOrEmpty(dto.OrderNumber))
            throw new InvalidOrderException("Order number required");

        var order = new Order(dto.OrderNumber);
        await _repo.AddAsync(order);
        return MapToDto(order);
    }
}

// Decorator: Logging + Retry
public class LoggingRetryDecorator : IOrderService
{
    private readonly IOrderService _inner;
    private readonly ILogger _logger;

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto)
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                _logger.LogInformation("Attempt {Attempt} to create order", i + 1);
                return await _inner.CreateAsync(dto);
            }
            catch (InvalidOperationException ex) when (i < 2)
            {
                _logger.LogWarning(ex, "Transient failure, retrying...");
                await Task.Delay(1000);
            }
            catch (InvalidOrderException ex)
            {
                _logger.LogWarning(ex, "Invalid order data");
                throw; // Don't retry
            }
        }

        throw new Exception("Max retries exceeded");
    }
}

// 3. Filter: Convert exceptions to HTTP responses (Filters)
public class GlobalExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger _logger;

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        var (statusCode, message) = context.Exception switch
        {
            InvalidOrderException ioe =>
                (StatusCodes.Status400BadRequest, ioe.Message),

            KeyNotFoundException knfe =>
                (StatusCodes.Status404NotFound, "Order not found"),

            _ =>
                (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        _logger.LogError(context.Exception, "Request failed");

        context.Result = new ObjectResult(new
        {
            message,
            traceId = context.HttpContext.TraceIdentifier
        })
        { StatusCode = statusCode };

        context.ExceptionHandled = true;
    }
}

// 4. Controller: Thin, orchestration only
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _service;

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }
}
```

## Error Flow with Hybrid Approach

```
1. HTTP Request
   ↓
2. Controller calls service
   ↓
3. Service (wrapped by Decorators)
   ├─ Decorator 1: Logging
   └─ Decorator 2: Retry
   ↓
4. Service throws InvalidOrderException
   ↓
5. Decorator catches → logs → re-throws
   ↓
6. Filter catches → converts to HTTP 400
   ↓
7. HTTP Response: { message: "Order number required", traceId: "xxx" }
```

## Practical Scenarios

### Scenario 1: Payment Processing

**AOP Decorator**: Retry with exponential backoff (business concern)
```csharp
// Retry payment service calls
public class PaymentRetryDecorator : IPaymentService
{
    public async Task<PaymentResult> ProcessAsync(Payment payment)
    {
        return await Policy<PaymentResult>
            .Handle<TimeoutException>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))
            .ExecuteAsync(() => _inner.ProcessAsync(payment));
    }
}
```

**Filter**: Convert payment errors to HTTP 402 (HTTP concern)
```csharp
public class PaymentErrorFilter : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is PaymentFailedException pfe)
        {
            context.Result = new ObjectResult(new
            {
                message = "Payment failed",
                error = pfe.Message
            })
            {
                StatusCode = StatusCodes.Status402PaymentRequired
            };
            context.ExceptionHandled = true;
        }
    }
}
```

### Scenario 2: Email Service

**AOP Decorator**: Retry + Fallback (business concern)
```csharp
public class EmailRetryDecorator : IEmailService
{
    public async Task SendAsync(string to, string subject, string body)
    {
        try
        {
            return await _inner.SendAsync(to, subject, body);
        }
        catch (SmtpException)
        {
            // Fallback: Queue for later retry
            await _emailQueue.EnqueueAsync(to, subject, body);
        }
    }
}
```

**Filter**: Don't expose email errors (HTTP concern)
```csharp
public class SensitiveErrorFilter : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is EmailServiceException)
        {
            // Log the real error
            _logger.LogError(context.Exception, "Email service failed");

            // Return generic response to user
            context.Result = new ObjectResult(new
            {
                message = "Request accepted for processing"
            })
            {
                StatusCode = StatusCodes.Status202Accepted
            };
            context.ExceptionHandled = true;
        }
    }
}
```

## Decision Tree

```
Do you need to:
├─ Handle errors at service level?
│  └─ YES → Use AOP Decorator
│
├─ Retry transient failures?
│  └─ YES → Use AOP Decorator (easier with Polly)
│
├─ Transform exceptions for API?
│  └─ YES → Use Filter (HTTP concern)
│
├─ Share behavior across services?
│  └─ YES → Use AOP Decorator
│
└─ Apply to all controllers automatically?
   └─ YES → Use Filter
```

## Best Practice Pattern

```csharp
// LAYER 1: Domain (Business)
// Pure entities, exceptions, value objects

// LAYER 2: Application (Services with AOP)
// Services wrapped by decorators for:
// - Logging
// - Retry/resilience
// - Performance monitoring
// - Validation

// LAYER 3: Infrastructure
// Repositories, external clients

// LAYER 4: Presentation (HTTP with Filters)
// Controllers
// Filters for:
// - Converting exceptions to HTTP responses
// - Validating models
// - Authentication/authorization
// - Standardizing error responses
```

## Implementation Checklist

Choose your approach:

### Full AOP (Services only)
- [ ] Create domain exceptions
- [ ] Create service interfaces
- [ ] Create service implementations
- [ ] Create decorator wrappers
- [ ] Register in DI with decorator chain
- [ ] Test service independently

### Full Filter (Controllers only)
- [ ] Create exception filters
- [ ] Create error response DTOs
- [ ] Register filters globally
- [ ] Test with HTTP client
- [ ] Document error codes

### Hybrid (Recommended)
- [ ] Create domain exceptions (services)
- [ ] Create service decorators (retry, logging)
- [ ] Create exception filters (HTTP responses)
- [ ] Register both in DI
- [ ] Test service layer independently
- [ ] Test API responses with HTTP client

---

**See Also**:
- [AOP Error Handler Decorator Template](../../templates/shared/aop/error-handler-decorator.template.cs)
- [Global Exception Filter Template](../../templates/shared/middleware/global-exception-filter.template.cs)
- [Serilog Logging Strategy](../logging/serilog-strategy.md)
- [Polly Resilience Patterns](../resilience/polly-patterns.md)
