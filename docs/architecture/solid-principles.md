# SOLID Principles in C# APIs

SOLID is a set of five design principles that make software more understandable, flexible, and maintainable.

## S - Single Responsibility Principle (SRP)

**Definition**: A class should have only one reason to change.

### ❌ Wrong - Multiple Responsibilities
```csharp
public class OrderService
{
    private readonly DbContext _context;
    
    // Responsibility 1: Business logic
    public void ApproveOrder(int orderId)
    {
        var order = _context.Orders.Find(orderId);
        if (order.Total > 1000 && order.Customer.IsVip)
        {
            order.Status = OrderStatus.Approved;
            _context.SaveChanges();
        }
    }
    
    // Responsibility 2: Persistence
    public void SaveOrder(Order order)
    {
        _context.Orders.Add(order);
        _context.SaveChanges();
    }
    
    // Responsibility 3: Email notification
    public void SendOrderEmail(int orderId)
    {
        var order = _context.Orders.Find(orderId);
        var emailBody = $"Order {order.Id} approved";
        SmtpClient.Send("orders@example.com", emailBody);
    }
}
```

### ✅ Correct - Single Responsibility Each
```csharp
// Service - Business logic only
public class OrderApprovalService
{
    private readonly IOrderRepository _orderRepository;
    
    public async Task ApproveOrderAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order.IsEligibleForApproval())
        {
            order.Approve();
            await _orderRepository.SaveAsync(order);
        }
    }
}

// Repository - Data persistence only
public class OrderRepository : IOrderRepository
{
    private readonly DbContext _context;
    
    public async Task<Order> GetByIdAsync(int id)
    {
        return await _context.Orders.FindAsync(id);
    }
    
    public async Task SaveAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }
}

// Notification Service - Email only
public class OrderNotificationService
{
    private readonly IEmailService _emailService;
    
    public async Task SendApprovalEmailAsync(Order order)
    {
        await _emailService.SendAsync("orders@example.com", 
            $"Order {order.Id} approved", GetEmailTemplate(order));
    }
}
```

---

## O - Open/Closed Principle (OCP)

**Definition**: Classes should be open for extension but closed for modification.

### ❌ Wrong - Requires Modification
```csharp
public class OrderProcessor
{
    public void ProcessPayment(Order order, string paymentMethod)
    {
        if (paymentMethod == "CreditCard")
        {
            // Process credit card
        }
        else if (paymentMethod == "PayPal")
        {
            // Process PayPal
        }
        else if (paymentMethod == "ApplePay")
        {
            // Process Apple Pay
        }
        // Adding new payment method requires changing this class!
    }
}
```

### ✅ Correct - Extensible Without Modification
```csharp
public interface IPaymentProcessor
{
    Task ProcessAsync(Order order, Payment payment);
}

public class CreditCardPaymentProcessor : IPaymentProcessor
{
    public async Task ProcessAsync(Order order, Payment payment)
    {
        // Credit card logic
    }
}

public class PayPalPaymentProcessor : IPaymentProcessor
{
    public async Task ProcessAsync(Order order, Payment payment)
    {
        // PayPal logic
    }
}

public class ApplePayPaymentProcessor : IPaymentProcessor
{
    public async Task ProcessAsync(Order order, Payment payment)
    {
        // Apple Pay logic
    }
}

// Main processor - closed for modification, open for extension
public class OrderProcessor
{
    private readonly IPaymentProcessor _paymentProcessor;
    
    public OrderProcessor(IPaymentProcessor paymentProcessor)
    {
        _paymentProcessor = paymentProcessor;
    }
    
    public async Task ProcessPaymentAsync(Order order, Payment payment)
    {
        await _paymentProcessor.ProcessAsync(order, payment);
        // Adding new payment method needs no changes here!
    }
}
```

---

## L - Liskov Substitution Principle (LSP)

**Definition**: Objects of a superclass should be replaceable with objects of its subclasses without breaking the application.

### ❌ Wrong - Violates Contract
```csharp
public abstract class PaymentProcessor
{
    public abstract Task ProcessAsync(Payment payment);
}

public class CreditCardProcessor : PaymentProcessor
{
    public override async Task ProcessAsync(Payment payment)
    {
        // Takes 5 seconds to process
        await Task.Delay(5000);
    }
}

public class InstantPaymentProcessor : PaymentProcessor
{
    public override async Task ProcessAsync(Payment payment)
    {
        // Throws exception - doesn't process at all!
        throw new NotImplementedException();
    }
}

// Client code assumes ProcessAsync works - violated!
var processor = GetProcessor(); // Could be InstantPaymentProcessor
await processor.ProcessAsync(payment); // Might throw!
```

### ✅ Correct - Honors Contract
```csharp
public interface IPaymentProcessor
{
    Task<PaymentResult> ProcessAsync(Payment payment);
}

public class CreditCardProcessor : IPaymentProcessor
{
    public async Task<PaymentResult> ProcessAsync(Payment payment)
    {
        // Always returns a result
        return new PaymentResult { Success = true, TransactionId = "..." };
    }
}

public class PayPalProcessor : IPaymentProcessor
{
    public async Task<PaymentResult> ProcessAsync(Payment payment)
    {
        // Also returns a result, behavior consistent
        try
        {
            // Process payment
            return new PaymentResult { Success = true, TransactionId = "..." };
        }
        catch
        {
            return new PaymentResult { Success = false, Error = "Processing failed" };
        }
    }
}

// Client code - any implementation works correctly
var processor = GetProcessor();
var result = await processor.ProcessAsync(payment); // Always returns PaymentResult
if (result.Success) { /*...*/ }
```

---

## I - Interface Segregation Principle (ISP)

**Definition**: Clients should not be forced to depend on interfaces they don't use.

### ❌ Wrong - Fat Interface
```csharp
public interface IOrderService
{
    Task<OrderDto> GetOrderAsync(int id);
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
    Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto);
    Task<bool> DeleteOrderAsync(int id);
    Task<List<OrderDto>> GetAllOrdersAsync();
    Task<bool> ApproveOrderAsync(int id);
    Task<bool> ShipOrderAsync(int id);
    Task SendOrderEmailAsync(int id);
    Task GenerateOrderReportAsync(int id);
    Task<byte[]> ExportOrdersPdfAsync();
    Task RecalculateInventoryAsync();
    // Client forced to implement everything even if it only needs GetOrder!
}
```

### ✅ Correct - Focused Interfaces
```csharp
public interface IOrderQueryService
{
    Task<OrderDto> GetOrderAsync(int id);
    Task<List<OrderDto>> GetAllOrdersAsync();
}

public interface IOrderCommandService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
    Task<OrderDto> UpdateOrderAsync(int id, UpdateOrderDto dto);
    Task<bool> DeleteOrderAsync(int id);
}

public interface IOrderApprovalService
{
    Task ApproveOrderAsync(int id);
    Task RejectOrderAsync(int id);
}

public interface IOrderShippingService
{
    Task ShipOrderAsync(int id);
    Task TrackOrderAsync(int id);
}

public interface IOrderNotificationService
{
    Task SendOrderEmailAsync(int id);
    Task SendApprovalNotificationAsync(int id);
}

// Client only depends on what it needs
public class OrderController
{
    private readonly IOrderQueryService _queryService;
    private readonly IOrderCommandService _commandService;
    
    public OrderController(IOrderQueryService queryService, IOrderCommandService commandService)
    {
        _queryService = queryService;
        _commandService = commandService;
    }
}

public class OrderApprovalWorker
{
    private readonly IOrderApprovalService _approvalService;
    private readonly IOrderNotificationService _notificationService;
    
    public OrderApprovalWorker(IOrderApprovalService approvalService, 
        IOrderNotificationService notificationService)
    {
        _approvalService = approvalService;
        _notificationService = notificationService;
    }
}
```

---

## D - Dependency Inversion Principle (DIP)

**Definition**: High-level modules should not depend on low-level modules. Both should depend on abstractions.

### ❌ Wrong - Direct Dependency on Concrete
```csharp
// High-level module depends on low-level details
public class OrderService
{
    private readonly SqlServerOrderRepository _repository; // Concrete!
    private readonly SmtpEmailService _emailService;        // Concrete!
    private readonly PayPalPaymentProcessor _processor;    // Concrete!
    
    public OrderService()
    {
        _repository = new SqlServerOrderRepository();
        _emailService = new SmtpEmailService();
        _processor = new PayPalPaymentProcessor();
    }
    
    // Hard to test - can't substitute implementations
}
```

### ✅ Correct - Depend on Abstractions
```csharp
// Both high-level and low-level depend on abstractions
public interface IOrderRepository
{
    Task<Order> GetByIdAsync(int id);
    Task SaveAsync(Order order);
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

public interface IPaymentProcessor
{
    Task<PaymentResult> ProcessAsync(Payment payment);
}

// High-level module - depends on abstractions only
public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly IEmailService _emailService;
    private readonly IPaymentProcessor _processor;
    
    // Injected dependencies
    public OrderService(
        IOrderRepository repository,
        IEmailService emailService,
        IPaymentProcessor processor)
    {
        _repository = repository;
        _emailService = emailService;
        _processor = processor;
    }
    
    public async Task ProcessOrderAsync(Order order)
    {
        // Works with any implementation
        var result = await _processor.ProcessAsync(order.Payment);
        if (result.Success)
        {
            order.Status = OrderStatus.Processed;
            await _repository.SaveAsync(order);
            await _emailService.SendAsync(order.Customer.Email, "Order Processed", "...");
        }
    }
}

// Easy to test with mock implementations
[Fact]
public async Task ProcessOrder_WithSuccessfulPayment_SavesOrder()
{
    // Arrange
    var mockRepository = new Mock<IOrderRepository>();
    var mockEmailService = new Mock<IEmailService>();
    var mockProcessor = new Mock<IPaymentProcessor>();
    
    mockProcessor
        .Setup(p => p.ProcessAsync(It.IsAny<Payment>()))
        .ReturnsAsync(new PaymentResult { Success = true });
    
    var service = new OrderService(mockRepository.Object, mockEmailService.Object, mockProcessor.Object);
    var order = new Order { /* ... */ };
    
    // Act
    await service.ProcessOrderAsync(order);
    
    // Assert
    mockRepository.Verify(r => r.SaveAsync(It.IsAny<Order>()), Times.Once);
}
```

---

## SOLID Summary Table

| Principle | Definition | Benefit | Violation Sign |
|-----------|-----------|---------|-----------------|
| **SRP** | One reason to change | Easy to understand | Class has multiple reasons to change |
| **OCP** | Open for extension, closed for modification | Easy to extend | Adding feature requires modifying existing code |
| **LSP** | Substitutable implementations | Polymorphism works | Derived class breaks parent contract |
| **ISP** | Focused, small interfaces | Flexible dependencies | Client forced to implement unused methods |
| **DIP** | Depend on abstractions | Testable, flexible | Hard-coded new() of concrete classes |

---

## SOLID in Onion Architecture

The Onion Architecture naturally supports SOLID:

```
Domain Layer (Core)
├─ SRP: Each entity has one business reason to change
├─ OCP: Entities extended via inheritance/composition
├─ LSP: Derived entities honor base contracts
└─ DIP: Domain depends on abstractions (interfaces)

Application Layer (Use Cases)
├─ SRP: Each service orchestrates one use case
├─ OCP: New use cases via new services
├─ LSP: Services honor interface contracts
├─ ISP: Small, focused service interfaces
└─ DIP: Depends on interfaces from infrastructure

Infrastructure Layer (Details)
├─ SRP: Repository = persistence, Service = API calls
├─ OCP: New implementations without changing consumers
└─ LSP: All implementations honor interface contracts

Presentation Layer (API)
├─ SRP: Controller = HTTP orchestration
└─ DIP: Depends on application services via DI
```

---

## Practical Checklist

When reviewing code, verify:

✅ **SRP**: Does this class have more than one reason to change?
✅ **OCP**: Can I add new behavior without modifying existing classes?
✅ **LSP**: Can I substitute any implementation for another?
✅ **ISP**: Is my interface focused or bloated?
✅ **DIP**: Am I depending on abstractions or concrete classes?

---

**Next**: Read about [DRY Principles](dry-principles.md) for avoiding code duplication.
