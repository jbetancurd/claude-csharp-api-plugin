// Template: Application Service (Use Case Orchestration)
// Copy and customize for your domain service

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace YourApp.Application.Services
{
    /// <summary>
    /// Application service orchestrates domain logic and coordinates use cases.
    ///
    /// Responsibility:
    /// - Orchestrate domain objects
    /// - Coordinate repositories and domain services
    /// - Handle application-level concerns (validation, mapping)
    /// - Does NOT contain business rules (those are in Domain)
    ///
    /// Dependencies: Only from Application layer (interfaces) and Domain layer
    /// </summary>
    public interface IOrderApplicationService
    {
        Task<OrderDto> CreateOrderAsync(CreateOrderDto dto);
        Task<OrderDto> GetOrderAsync(int id);
        Task<List<OrderDto>> ListOrdersAsync(int page = 1, int pageSize = 20);
        Task<OrderDto> ApproveOrderAsync(int id);
        Task<OrderDto> CancelOrderAsync(int id, string reason);
        Task DeleteOrderAsync(int id);
    }

    public class OrderApplicationService : IOrderApplicationService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderCalculationService _calculationService;
        private readonly IOrderValidator _validator;
        private readonly ILogger<OrderApplicationService> _logger;

        public OrderApplicationService(
            IOrderRepository orderRepository,
            IOrderCalculationService calculationService,
            IOrderValidator validator,
            ILogger<OrderApplicationService> logger)
        {
            _orderRepository = orderRepository;
            _calculationService = calculationService;
            _validator = validator;
            _logger = logger;
        }

        /// <summary>
        /// Create Order - TDD: Write test first
        ///
        /// Test:
        /// [Fact]
        /// public async Task CreateOrder_WithValidData_ReturnsOrderDto()
        /// {
        ///     // Arrange
        ///     var dto = new CreateOrderDto { CustomerId = 1, Items = new List<OrderItemDto>() };
        ///     var service = new OrderApplicationService(...);
        ///
        ///     // Act
        ///     var result = await service.CreateOrderAsync(dto);
        ///
        ///     // Assert
        ///     Assert.NotNull(result);
        ///     Assert.Equal(dto.CustomerId, result.CustomerId);
        /// }
        /// </summary>
        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
        {
            // 1. Validate input (Application layer concern)
            var validationResult = _validator.ValidateCreateOrder(dto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid create order request: {Errors}",
                    string.Join(", ", validationResult.Errors));
                throw new ApplicationValidationException(validationResult.Errors);
            }

            // 2. Create domain entity (Domain layer)
            var order = new Order(customerId: dto.CustomerId);

            // 3. Add items to order (Domain logic)
            foreach (var itemDto in dto.Items)
            {
                order.AddItem(itemDto.ProductId, itemDto.Quantity, itemDto.UnitPrice);
            }

            // 4. Calculate total (Domain service)
            var total = _calculationService.CalculateTotalPrice(order.Items);
            order.SetTotal(total);

            // 5. Persist (Infrastructure abstraction via interface)
            await _orderRepository.AddAsync(order);

            _logger.LogInformation("Order created: {OrderId}", order.Id);

            // 6. Return DTO (Application layer output)
            return MapToDto(order);
        }

        /// <summary>
        /// Get Order by ID
        /// </summary>
        public async Task<OrderDto> GetOrderAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Order ID must be positive");

            var order = await _orderRepository.GetByIdAsync(id);

            if (order == null)
            {
                _logger.LogWarning("Order not found: {OrderId}", id);
                throw new OrderNotFoundException(id);
            }

            return MapToDto(order);
        }

        /// <summary>
        /// List Orders with pagination
        /// </summary>
        public async Task<List<OrderDto>> ListOrdersAsync(int page = 1, int pageSize = 20)
        {
            // Validate pagination
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // Use specification pattern for complex queries
            var spec = new OrdersListSpecification(page, pageSize);
            var orders = await _orderRepository.FindAsync(spec);

            return orders.ConvertAll(MapToDto);
        }

        /// <summary>
        /// Approve Order - Domain operation
        /// </summary>
        public async Task<OrderDto> ApproveOrderAsync(int id)
        {
            // 1. Get order
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new OrderNotFoundException(id);

            // 2. Invoke domain method (encapsulates business rules)
            try
            {
                order.Approve();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Cannot approve order {OrderId}: {Reason}", id, ex.Message);
                throw new ApplicationException($"Order cannot be approved: {ex.Message}");
            }

            // 3. Persist change
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("Order approved: {OrderId}", id);

            return MapToDto(order);
        }

        /// <summary>
        /// Cancel Order with reason
        /// </summary>
        public async Task<OrderDto> CancelOrderAsync(int id, string reason)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Cancel reason required");

            // Get order
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new OrderNotFoundException(id);

            // Cancel (domain operation)
            try
            {
                order.Cancel(reason);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Cannot cancel order {OrderId}: {Reason}", id, ex.Message);
                throw new ApplicationException($"Order cannot be cancelled: {ex.Message}");
            }

            // Persist
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("Order cancelled: {OrderId}, Reason: {Reason}", id, reason);

            return MapToDto(order);
        }

        /// <summary>
        /// Delete Order
        /// </summary>
        public async Task DeleteOrderAsync(int id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
                throw new OrderNotFoundException(id);

            // Domain logic: Can only delete draft orders
            if (order.Status != OrderStatus.Draft)
                throw new ApplicationException("Can only delete draft orders");

            await _orderRepository.DeleteAsync(id);

            _logger.LogInformation("Order deleted: {OrderId}", id);
        }

        /// <summary>
        /// Mapping: Domain entity → Application DTO
        /// </summary>
        private OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                CustomerId = order.CustomerId,
                Total = order.Total,
                CreatedAt = order.CreatedAt,
                Items = order.Items.ConvertAll(i => new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                })
            };
        }
    }

    // ============ DTOs (Application layer contracts) ============

    public class CreateOrderDto
    {
        public int CustomerId { get; set; }
        public List<CreateOrderItemDto> Items { get; set; }
    }

    public class CreateOrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public OrderStatus Status { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemDto> Items { get; set; }
    }

    public class OrderItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    // ============ Domain Interfaces (from Application layer) ============

    public interface IOrderRepository
    {
        Task<Order> GetByIdAsync(int id);
        Task<List<Order>> FindAsync(Specification<Order> spec);
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
        Task DeleteAsync(int id);
    }

    public interface IOrderCalculationService
    {
        decimal CalculateTotalPrice(List<OrderItem> items);
    }

    public interface IOrderValidator
    {
        ValidationResult ValidateCreateOrder(CreateOrderDto dto);
    }

    // ============ Exceptions ============

    public class ApplicationValidationException : Exception
    {
        public List<string> Errors { get; }

        public ApplicationValidationException(List<string> errors)
            : base($"Validation failed: {string.Join(", ", errors)}")
        {
            Errors = errors;
        }
    }

    public class OrderNotFoundException : Exception
    {
        public OrderNotFoundException(int orderId)
            : base($"Order with ID {orderId} not found") { }
    }

    // ============ Supporting Classes ============

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
    }

    public class OrdersListSpecification : Specification<Order>
    {
        public OrdersListSpecification(int page, int pageSize)
        {
            var skip = (page - 1) * pageSize;
            SetPaging(skip, pageSize);
            AddOrderBy("CreatedAt DESC");
        }
    }
}
