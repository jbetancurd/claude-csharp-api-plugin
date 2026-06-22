# RESTful API Guide (Resource-Based)

RESTful (Representational State Transfer) is a resource-oriented architectural style that leverages HTTP semantics and verbs correctly.

## REST vs RESTful

**REST** = Action-oriented
```
POST /api/orders/approve
POST /api/users/disable
```

**RESTful** = Resource-oriented with HTTP verbs
```
PUT /api/orders/123        (update)
DELETE /api/users/456      (delete)
GET /api/orders            (list)
POST /api/orders           (create)
```

## Core Concepts

### 1. Resources
Everything is a resource with a unique URL.

```csharp
/api/orders           // Collection
/api/orders/123       // Specific resource
/api/orders/123/items // Nested resource
/api/orders/123/items/456 // Specific nested
```

### 2. HTTP Methods (Verbs)

| Method | Purpose | Idempotent | Safe |
|--------|---------|-----------|------|
| **GET** | Retrieve | ✅ Yes | ✅ Yes (no side effects) |
| **POST** | Create | ❌ No | ❌ No |
| **PUT** | Replace entirely | ✅ Yes | ❌ No |
| **PATCH** | Partial update | ❌ No | ❌ No |
| **DELETE** | Remove | ✅ Yes | ❌ No |
| **HEAD** | Like GET, no body | ✅ Yes | ✅ Yes |

### 3. HTTP Status Codes

```csharp
// Success
200 OK                   // Request succeeded
201 Created              // Resource created
202 Accepted             // Request accepted for processing
204 No Content           // Success, no response body

// Client Error
400 Bad Request          // Invalid request
401 Unauthorized         // Authentication required
403 Forbidden            // Not authorized for resource
404 Not Found            // Resource doesn't exist
409 Conflict             // Request conflicts with current state
422 Unprocessable Entity // Request validation failed

// Server Error
500 Internal Server Error    // Server error
503 Service Unavailable      // Temporarily unavailable
```

## RESTful Controller Template

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderApplicationService _orderService;
    private readonly ILogger<OrdersController> _logger;
    
    public OrdersController(
        IOrderApplicationService orderService,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }
    
    // GET /api/orders
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<OrderDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<OrderDto>>> ListOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null)
    {
        var orders = await _orderService.ListOrdersAsync(page, pageSize, status);
        return Ok(orders);
    }
    
    // GET /api/orders/123
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
    
    // POST /api/orders
    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var order = await _orderService.CreateOrderAsync(dto);
        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
    }
    
    // PUT /api/orders/123 (Full replacement)
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> UpdateOrder(int id, [FromBody] UpdateOrderDto dto)
    {
        var order = await _orderService.UpdateOrderAsync(id, dto);
        if (order == null)
            return NotFound();
        return Ok(order);
    }
    
    // PATCH /api/orders/123 (Partial update)
    [HttpPatch("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> PartialUpdateOrder(
        int id,
        [FromBody] JsonPatchDocument<UpdateOrderDto> patchDoc)
    {
        var order = await _orderService.PatchOrderAsync(id, patchDoc);
        if (order == null)
            return NotFound();
        return Ok(order);
    }
    
    // DELETE /api/orders/123
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteOrder(int id)
    {
        var success = await _orderService.DeleteOrderAsync(id);
        if (!success)
            return NotFound();
        return NoContent();
    }
    
    // GET /api/orders/123/items
    [HttpGet("{orderId}/items")]
    [ProducesResponseType(typeof(List<OrderItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderItemDto>>> GetOrderItems(int orderId)
    {
        var items = await _orderService.GetOrderItemsAsync(orderId);
        return Ok(items);
    }
    
    // POST /api/orders/123/items
    [HttpPost("{orderId}/items")]
    [ProducesResponseType(typeof(OrderItemDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<OrderItemDto>> AddOrderItem(
        int orderId,
        [FromBody] AddOrderItemDto dto)
    {
        var item = await _orderService.AddOrderItemAsync(orderId, dto);
        return CreatedAtAction(nameof(GetOrderItems), new { orderId }, item);
    }
    
    // DELETE /api/orders/123/items/456
    [HttpDelete("{orderId}/items/{itemId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveOrderItem(int orderId, int itemId)
    {
        var success = await _orderService.RemoveOrderItemAsync(orderId, itemId);
        if (!success)
            return NotFound();
        return NoContent();
    }
}
```

## RESTful DTOs

```csharp
// List/Pagination Response
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

// Input DTOs
public class CreateOrderDto
{
    [Required]
    public int CustomerId { get; set; }
    
    [Required]
    [MinLength(1)]
    public List<CreateOrderItemDto> Items { get; set; }
}

public class UpdateOrderDto
{
    public OrderStatus Status { get; set; }
    public string Notes { get; set; }
}

public class AddOrderItemDto
{
    [Required]
    public int ProductId { get; set; }
    
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}

// Output DTOs
public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; }
    
    // HATEOAS - Self link
    public string Self => $"/api/orders/{Id}";
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// Error Response
public class ErrorResponse
{
    public string Message { get; set; }
    public List<string> Errors { get; set; }
    public string TraceId { get; set; }
}
```

## RESTful Testing

```csharp
public class OrdersControllerTests
{
    [Fact]
    public async Task GetOrders_Returns200WithList()
    {
        // Arrange
        var mockService = new Mock<IOrderApplicationService>();
        var expected = new PaginatedResponse<OrderDto>
        {
            Data = new List<OrderDto> { new OrderDto { Id = 1, OrderNumber = "ORD-001" } },
            Page = 1,
            PageSize = 20,
            TotalCount = 1
        };
        mockService.Setup(s => s.ListOrdersAsync(1, 20, null)).ReturnsAsync(expected);
        
        var controller = new OrdersController(mockService.Object, Mock.Of<ILogger<OrdersController>>());
        
        // Act
        var result = await controller.ListOrders(1, 20, null);
        
        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PaginatedResponse<OrderDto>>(okResult.Value);
        Assert.Single(response.Data);
    }
    
    [Fact]
    public async Task CreateOrder_Returns201WithLocation()
    {
        // Arrange
        var mockService = new Mock<IOrderApplicationService>();
        var dto = new CreateOrderDto { CustomerId = 1, Items = new List<CreateOrderItemDto>() };
        var createdOrder = new OrderDto { Id = 1, OrderNumber = "ORD-001" };
        
        mockService.Setup(s => s.CreateOrderAsync(dto)).ReturnsAsync(createdOrder);
        var controller = new OrdersController(mockService.Object, Mock.Of<ILogger<OrdersController>>());
        
        // Act
        var result = await controller.CreateOrder(dto);
        
        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(OrdersController.GetOrder), createdResult.ActionName);
        Assert.Equal(createdOrder.Id, ((OrderDto)createdResult.Value).Id);
    }
    
    [Fact]
    public async Task DeleteOrder_Returns204NoContent()
    {
        // Arrange
        var mockService = new Mock<IOrderApplicationService>();
        mockService.Setup(s => s.DeleteOrderAsync(1)).ReturnsAsync(true);
        var controller = new OrdersController(mockService.Object, Mock.Of<ILogger<OrdersController>>());
        
        // Act
        var result = await controller.DeleteOrder(1);
        
        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}
```

## RESTful Best Practices

✅ **Use Correct HTTP Methods** - GET for retrieval, POST for creation, etc.
✅ **Meaningful Status Codes** - 201 for created, 204 for no content, etc.
✅ **Plural Resource Names** - `/api/orders`, not `/api/order`
✅ **Hierarchy in URLs** - `/api/orders/123/items` for relationships
✅ **Consistent Response Format** - Always return JSON with same structure
✅ **Pagination** - For large lists use page/pageSize
✅ **HATEOAS** - Include links to related resources (advanced)
✅ **Filtering/Sorting** - Query parameters: `?status=pending&sort=createdAt`

❌ **Avoid**:
- Using GET for mutations
- Inconsistent status codes
- Singular resource names
- Deep nesting (more than 3 levels)
- Mixing action verbs in URLs

## When to Use RESTful

✅ Use RESTful when:
- API follows CRUD patterns
- Resource model maps to your domain
- Building public/standard APIs
- Team values HTTP semantics
- Integrating with REST tools/clients

❌ Avoid RESTful when:
- Complex operations don't map to CRUD
- Action-oriented API preferred
- WebSocket/GraphQL better fits needs

---

**Comparison**: See [REST Guide](rest-guide.md) for action-based approach.
