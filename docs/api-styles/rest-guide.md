# REST API Guide (Action-Based)

REST stands for "action-oriented API" where URLs represent actions and HTTP verbs are less strict about semantics.

## REST vs RESTful

```
REST (Action-Based)          RESTful (Resource-Based)
POST /api/users/approve      PUT /api/users/123
POST /api/orders/cancel      DELETE /api/orders/123
GET /api/reports/generate    GET /api/users/123
POST /api/cache/clear        DELETE /api/cache
```

REST is more **intuitive** for non-CRUD operations.
RESTful is more **standard** and HTTP-semantic.

## REST API Patterns

### Pattern 1: Command Actions
```csharp
[HttpPost("api/orders/{id}/approve")]
public async Task<ActionResult> ApproveOrder(int id)
{
    await _orderService.ApproveAsync(id);
    return Ok(new { message = "Order approved" });
}

[HttpPost("api/orders/{id}/cancel")]
public async Task<ActionResult> CancelOrder(int id)
{
    await _orderService.CancelAsync(id);
    return Ok(new { message = "Order cancelled" });
}
```

### Pattern 2: Bulk Operations
```csharp
[HttpPost("api/orders/bulk-approve")]
public async Task<ActionResult> BulkApproveOrders([FromBody] BulkApproveDto dto)
{
    var results = await _orderService.BulkApproveAsync(dto.OrderIds);
    return Ok(results);
}

[HttpPost("api/users/send-notifications")]
public async Task<ActionResult> SendNotifications([FromBody] SendNotificationsDto dto)
{
    var sent = await _notificationService.SendAsync(dto.UserIds, dto.Message);
    return Ok(new { sentCount = sent });
}
```

### Pattern 3: Complex Operations
```csharp
[HttpPost("api/reports/generate")]
public async Task<ActionResult> GenerateReport([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
{
    var report = await _reportService.GenerateAsync(startDate, endDate);
    return Ok(report);
}

[HttpPost("api/inventory/reconcile")]
public async Task<ActionResult> ReconcileInventory()
{
    var result = await _inventoryService.ReconcileAsync();
    return Ok(result);
}
```

## REST Controller Template

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly OrderApplicationService _orderService;
    private readonly ILogger<OrdersController> _logger;
    
    public OrdersController(
        OrderApplicationService orderService,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }
    
    // Read operations - GET
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        var order = await _orderService.GetOrderAsync(id);
        if (order == null)
            return NotFound();
        return Ok(order);
    }
    
    // Create operations - POST
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var order = await _orderService.CreateOrderAsync(dto);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }
    
    // Action: Approve
    [HttpPost("{id}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ApproveOrder(int id)
    {
        await _orderService.ApproveOrderAsync(id);
        return Ok(new { message = "Order approved" });
    }
    
    // Action: Cancel
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CancelOrder(int id, [FromBody] CancelOrderDto dto)
    {
        await _orderService.CancelOrderAsync(id, dto.Reason);
        return Ok(new { message = "Order cancelled" });
    }
    
    // Action: Ship
    [HttpPost("{id}/ship")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> ShipOrder(int id, [FromBody] ShipOrderDto dto)
    {
        var order = await _orderService.ShipOrderAsync(id, dto.TrackingNumber);
        return Ok(order);
    }
    
    // Bulk action
    [HttpPost("bulk-approve")]
    [ProducesResponseType(typeof(BulkApproveResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkApproveResultDto>> BulkApproveOrders(
        [FromBody] BulkApproveDto dto)
    {
        var result = await _orderService.BulkApproveAsync(dto.OrderIds);
        return Ok(result);
    }
}
```

## REST DTOs

```csharp
// Input DTOs
public class CreateOrderDto
{
    public int CustomerId { get; set; }
    public List<OrderItemDto> Items { get; set; }
}

public class ApproveOrderDto
{
    public string ApprovedBy { get; set; }
    public string Notes { get; set; }
}

public class CancelOrderDto
{
    public string Reason { get; set; }
}

public class BulkApproveDto
{
    public List<int> OrderIds { get; set; }
}

// Output DTOs
public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public List<OrderItemDto> Items { get; set; }
}

public class BulkApproveResultDto
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<BulkApproveErrorDto> Errors { get; set; }
}
```

## REST Testing with xUnit

```csharp
public class OrdersControllerTests
{
    private readonly Mock<OrderApplicationService> _mockOrderService;
    private readonly OrdersController _controller;
    
    public OrdersControllerTests()
    {
        _mockOrderService = new Mock<OrderApplicationService>();
        _controller = new OrdersController(_mockOrderService.Object, Mock.Of<ILogger<OrdersController>>());
    }
    
    [Fact]
    public async Task ApproveOrder_WithValidId_ReturnsOk()
    {
        // Arrange
        var orderId = 1;
        _mockOrderService
            .Setup(s => s.ApproveOrderAsync(orderId))
            .Returns(Task.CompletedTask);
        
        // Act
        var result = await _controller.ApproveOrder(orderId);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _mockOrderService.Verify(s => s.ApproveOrderAsync(orderId), Times.Once);
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ApproveOrder_WithInvalidId_ReturnsBadRequest(int orderId)
    {
        // Arrange
        // Act
        // Assert - controller should validate before calling service
        var result = await _controller.ApproveOrder(orderId);
        Assert.IsType<BadRequestObjectResult>(result);
    }
    
    [Fact]
    public async Task BulkApproveOrders_WithValidData_ReturnsSuccessCount()
    {
        // Arrange
        var dto = new BulkApproveDto { OrderIds = new List<int> { 1, 2, 3 } };
        var expectedResult = new BulkApproveResultDto { SuccessCount = 3, FailureCount = 0 };
        
        _mockOrderService
            .Setup(s => s.BulkApproveAsync(dto.OrderIds))
            .ReturnsAsync(expectedResult);
        
        // Act
        var result = await _controller.BulkApproveOrders(dto);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedDto = Assert.IsType<BulkApproveResultDto>(okResult.Value);
        Assert.Equal(3, returnedDto.SuccessCount);
    }
}
```

## REST Best Practices

✅ **Clear Intent** - URL clearly shows what action is performed
✅ **Logical Grouping** - Related actions grouped under resource
✅ **Versioning** - Consider /api/v1/orders/approve for future compatibility
✅ **Error Handling** - Consistent error responses

❌ **Avoid**:
- Non-RESTful HTTP methods (GET for actions)
- Unclear action names (GetAndApprove)
- Inconsistent URL patterns
- Not using POST for actions that modify state

## When to Use REST

✅ Use REST when:
- Operations don't map to CRUD
- Actions have side effects
- API is action-oriented
- Simplicity and intuition matter most

❌ Avoid REST when:
- Standard CRUD fits better
- HTTP semantics matter
- Building public/standard APIs
- Team prefers RESTful standards

---

**Comparison**: See [RESTful Guide](restful-guide.md) for resource-based approach.
