// Template: xUnit Tests with TDD (Test-Driven Development)
// Uses AAA (Arrange-Act-Assert) pattern

using Xunit;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;

namespace YourApp.Tests.Application
{
    /// <summary>
    /// Test class for OrderApplicationService
    ///
    /// TDD Workflow:
    /// 1. Write test (RED - fails)
    /// 2. Write minimum code to pass (GREEN)
    /// 3. Refactor while keeping test green (REFACTOR)
    /// 4. Repeat
    ///
    /// AAA Pattern:
    /// - Arrange: Set up test data and mocks
    /// - Act: Execute the operation
    /// - Assert: Verify the result
    /// </summary>
    public class OrderApplicationServiceTests : IDisposable
    {
        // ============ Setup & Fixtures ============

        private readonly Mock<IOrderRepository> _mockOrderRepository;
        private readonly Mock<IOrderCalculationService> _mockCalculationService;
        private readonly Mock<IOrderValidator> _mockValidator;
        private readonly Mock<ILogger<OrderApplicationService>> _mockLogger;
        private readonly OrderApplicationService _service;

        public OrderApplicationServiceTests()
        {
            // Arrange - Setup mocks (shared across tests)
            _mockOrderRepository = new Mock<IOrderRepository>();
            _mockCalculationService = new Mock<IOrderCalculationService>();
            _mockValidator = new Mock<IOrderValidator>();
            _mockLogger = new Mock<ILogger<OrderApplicationService>>();

            // Create service under test with mocks
            _service = new OrderApplicationService(
                _mockOrderRepository.Object,
                _mockCalculationService.Object,
                _mockValidator.Object,
                _mockLogger.Object);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }

        // ============ CREATE ORDER TESTS ============

        [Fact]
        public async Task CreateOrder_WithValidData_ReturnsOrderDto()
        {
            // Arrange
            var dto = new CreateOrderDto
            {
                CustomerId = 1,
                Items = new List<CreateOrderItemDto>
                {
                    new() { ProductId = 1, Quantity = 2, UnitPrice = 10m }
                }
            };

            _mockValidator
                .Setup(v => v.ValidateCreateOrder(It.IsAny<CreateOrderDto>()))
                .Returns(new ValidationResult { IsValid = true });

            _mockCalculationService
                .Setup(c => c.CalculateTotalPrice(It.IsAny<List<OrderItem>>()))
                .Returns(20m);

            _mockOrderRepository
                .Setup(r => r.AddAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CreateOrderAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.CustomerId.Should().Be(1);
            result.Total.Should().Be(20m);

            // Verify repository was called
            _mockOrderRepository.Verify(
                r => r.AddAsync(It.IsAny<Order>()),
                Times.Once,
                "Repository should persist the order");
        }

        [Fact]
        public async Task CreateOrder_WithInvalidData_ThrowsValidationException()
        {
            // Arrange
            var dto = new CreateOrderDto { CustomerId = -1, Items = new List<CreateOrderItemDto>() };

            var errors = new List<string> { "Customer ID must be positive", "Items required" };
            _mockValidator
                .Setup(v => v.ValidateCreateOrder(It.IsAny<CreateOrderDto>()))
                .Returns(new ValidationResult { IsValid = false, Errors = errors });

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ApplicationValidationException>(
                () => _service.CreateOrderAsync(dto));

            exception.Errors.Should().Contain("Customer ID must be positive");
            _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task CreateOrder_WithEmptyItems_ThrowsException(string productName)
        {
            // Arrange
            var dto = new CreateOrderDto
            {
                CustomerId = 1,
                Items = new List<CreateOrderItemDto>() // Empty
            };

            _mockValidator
                .Setup(v => v.ValidateCreateOrder(It.IsAny<CreateOrderDto>()))
                .Returns(new ValidationResult { IsValid = false, Errors = new List<string> { "Items required" } });

            // Act & Assert
            await Assert.ThrowsAsync<ApplicationValidationException>(
                () => _service.CreateOrderAsync(dto));
        }

        // ============ GET ORDER TESTS ============

        [Fact]
        public async Task GetOrder_WithValidId_ReturnsOrderDto()
        {
            // Arrange
            var orderId = 1;
            var order = new Order(customerId: 1);
            order.AddItem(productId: 1, quantity: 2, unitPrice: 10m);

            _mockOrderRepository
                .Setup(r => r.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            // Act
            var result = await _service.GetOrderAsync(orderId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(order.Id);
            result.CustomerId.Should().Be(1);
        }

        [Fact]
        public async Task GetOrder_WithNonExistentId_ThrowsOrderNotFoundException()
        {
            // Arrange
            var orderId = 999;
            _mockOrderRepository
                .Setup(r => r.GetByIdAsync(orderId))
                .ReturnsAsync((Order)null);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<OrderNotFoundException>(
                () => _service.GetOrderAsync(orderId));

            exception.Message.Should().Contain("999");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetOrder_WithInvalidId_ThrowsArgumentException(int invalidId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.GetOrderAsync(invalidId));
        }

        // ============ APPROVE ORDER TESTS ============

        [Fact]
        public async Task ApproveOrder_WithPendingOrder_ChangesStatusToApproved()
        {
            // Arrange
            var orderId = 1;
            var order = new Order(customerId: 1);
            order.SetStatus(OrderStatus.Pending); // Domain state

            _mockOrderRepository
                .Setup(r => r.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            _mockOrderRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.ApproveOrderAsync(orderId);

            // Assert
            result.Status.Should().Be(OrderStatus.Approved);
            _mockOrderRepository.Verify(r => r.UpdateAsync(order), Times.Once);
        }

        [Fact]
        public async Task ApproveOrder_WithAlreadyApprovedOrder_ThrowsException()
        {
            // Arrange
            var orderId = 1;
            var order = new Order(customerId: 1);
            order.SetStatus(OrderStatus.Approved); // Already approved

            _mockOrderRepository
                .Setup(r => r.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ApplicationException>(
                () => _service.ApproveOrderAsync(orderId));

            exception.Message.Should().Contain("cannot be approved");
            _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        }

        // ============ CANCEL ORDER TESTS ============

        [Fact]
        public async Task CancelOrder_WithValidReason_CancelsSuccessfully()
        {
            // Arrange
            var orderId = 1;
            var reason = "Customer requested cancellation";
            var order = new Order(customerId: 1);
            order.SetStatus(OrderStatus.Pending);

            _mockOrderRepository
                .Setup(r => r.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            _mockOrderRepository
                .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.CancelOrderAsync(orderId, reason);

            // Assert
            result.Status.Should().Be(OrderStatus.Cancelled);
            _mockOrderRepository.Verify(r => r.UpdateAsync(order), Times.Once);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CancelOrder_WithEmptyReason_ThrowsArgumentException(string reason)
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _service.CancelOrderAsync(1, reason));
        }

        // ============ DELETE ORDER TESTS ============

        [Fact]
        public async Task DeleteOrder_WithDraftOrder_DeletesSuccessfully()
        {
            // Arrange
            var orderId = 1;
            var order = new Order(customerId: 1);
            order.SetStatus(OrderStatus.Draft);

            _mockOrderRepository
                .Setup(r => r.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            _mockOrderRepository
                .Setup(r => r.DeleteAsync(orderId))
                .Returns(Task.CompletedTask);

            // Act
            await _service.DeleteOrderAsync(orderId);

            // Assert
            _mockOrderRepository.Verify(r => r.DeleteAsync(orderId), Times.Once);
        }

        [Fact]
        public async Task DeleteOrder_WithNonDraftOrder_ThrowsException()
        {
            // Arrange
            var orderId = 1;
            var order = new Order(customerId: 1);
            order.SetStatus(OrderStatus.Approved); // Not draft

            _mockOrderRepository
                .Setup(r => r.GetByIdAsync(orderId))
                .ReturnsAsync(order);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ApplicationException>(
                () => _service.DeleteOrderAsync(orderId));

            exception.Message.Should().Contain("draft");
            _mockOrderRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        // ============ LIST ORDERS TESTS ============

        [Fact]
        public async Task ListOrders_WithValidPaging_ReturnsOrderList()
        {
            // Arrange
            var orders = new List<Order>
            {
                new Order(customerId: 1),
                new Order(customerId: 2)
            };

            _mockOrderRepository
                .Setup(r => r.FindAsync(It.IsAny<Specification<Order>>()))
                .ReturnsAsync(orders);

            // Act
            var result = await _service.ListOrdersAsync(page: 1, pageSize: 20);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(o => o.Should().BeOfType<OrderDto>());
        }
    }

    // ============ FIXTURE PATTERN (for shared test data) ============

    public class OrderTestFixture
    {
        public Order CreateValidOrder()
        {
            var order = new Order(customerId: 1);
            order.AddItem(productId: 1, quantity: 2, unitPrice: 10m);
            return order;
        }

        public CreateOrderDto CreateValidCreateOrderDto()
        {
            return new CreateOrderDto
            {
                CustomerId = 1,
                Items = new List<CreateOrderItemDto>
                {
                    new() { ProductId = 1, Quantity = 2, UnitPrice = 10m }
                }
            };
        }
    }

    public class OrderApplicationServiceWithFixtureTests : IClassFixture<OrderTestFixture>
    {
        private readonly OrderTestFixture _fixture;
        private readonly OrderApplicationService _service;

        public OrderApplicationServiceWithFixtureTests(OrderTestFixture fixture)
        {
            _fixture = fixture;
            // Initialize service with mocks
            _service = new OrderApplicationService(
                new Mock<IOrderRepository>().Object,
                new Mock<IOrderCalculationService>().Object,
                new Mock<IOrderValidator>().Object,
                new Mock<ILogger<OrderApplicationService>>().Object);
        }

        [Fact]
        public void ValidOrder_IsValidDomainEntity()
        {
            // Arrange
            var order = _fixture.CreateValidOrder();

            // Act & Assert
            order.Should().NotBeNull();
            order.Items.Should().HaveCount(1);
        }
    }

    // ============ INTEGRATION TEST EXAMPLE ============

    public class OrderApplicationServiceIntegrationTests
    {
        [Fact]
        public async Task CreateAndApproveOrder_WithRealRepository_WorksEnd2End()
        {
            // This would use a real (or in-memory) database
            // Arrange
            // var dbContext = new OrderDbContext(...);
            // var repository = new OrderRepository(dbContext);
            // var service = new OrderApplicationService(repository, ...);

            // Act
            // var result = await service.CreateOrderAsync(...);
            // var approved = await service.ApproveOrderAsync(result.Id);

            // Assert
            // approved.Status.Should().Be(OrderStatus.Approved);

            await Task.CompletedTask; // Placeholder
        }
    }
}
