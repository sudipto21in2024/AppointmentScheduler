using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Data;
using Shared.DTOs;
using Shared.Models;
using Shared.Events;
using MassTransit;
using PaymentService.Services;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Tests
{
    public class PaymentServiceTests
    {
        private readonly Mock<ApplicationDbContext> _mockDbContext;
        private readonly Mock<ILogger<PaymentService.Services.PaymentService>> _mockLogger;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
        private readonly PaymentService.Services.PaymentService _paymentService;

        public PaymentServiceTests()
        {
            _mockDbContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
            _mockLogger = new Mock<ILogger<PaymentService.Services.PaymentService>>();
            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
            
            _paymentService = new PaymentService.Services.PaymentService(
                _mockDbContext.Object,
                _mockLogger.Object,
                _mockPublishEndpoint.Object);
        }

        [Fact]
        public async Task ProcessPaymentAsync_ValidRequest_ReturnsPaymentDetails()
        {
            // Arrange
            var request = new ProcessPaymentRequest
            {
                BookingId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Amount = 100.00m,
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };

            // Act
            var result = await _paymentService.ProcessPaymentAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.BookingId, result.BookingId);
            Assert.Equal(request.Amount, result.Amount);
            Assert.Equal(request.Currency, result.Currency);
            Assert.Equal(request.PaymentMethod, result.PaymentMethod);
            Assert.Equal("Completed", result.PaymentStatus);
            Assert.NotNull(result.TransactionId);
            Assert.NotNull(result.PaymentGateway);
            Assert.NotNull(result.PaidAt);
        }

        [Fact]
        public async Task ProcessRefundAsync_ValidRequest_ReturnsUpdatedPaymentDetails()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            
            var payment = new Payment
            {
                Id = paymentId,
                BookingId = Guid.NewGuid(),
                Amount = 100.00m,
                Currency = "USD",
                PaymentMethod = "CreditCard",
                PaymentStatus = "Completed",
                TransactionId = "txn_123456",
                PaymentGateway = "SimulatedGateway",
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RefundAmount = 0,
                TenantId = tenantId
            };

            _mockDbContext.Setup(db => db.Payments.FindAsync(paymentId))
                .Returns(new ValueTask<Payment?>(payment));

            var request = new RefundPaymentRequest
            {
                PaymentId = paymentId,
                BookingId = payment.BookingId,
                CustomerId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
                TenantId = tenantId,
                RefundAmount = 50.00m,
                Currency = "USD",
                RefundReason = "Customer request"
            };

            // Act
            var result = await _paymentService.ProcessRefundAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(payment.Id, result.Id);
            Assert.Equal(payment.BookingId, result.BookingId);
            Assert.Equal(payment.Amount, result.Amount);
            Assert.Equal(payment.Currency, result.Currency);
            Assert.Equal(payment.PaymentMethod, result.PaymentMethod);
            Assert.Equal("Refunded", result.PaymentStatus);
            Assert.Equal(50.00m, result.RefundAmount);
        }

        [Fact]
        public async Task ProcessRefundAsync_InvalidPaymentId_ThrowsArgumentException()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            _mockDbContext.Setup(db => db.Payments.FindAsync(paymentId))
                .Returns(new ValueTask<Payment?>((Payment?)null));

            var request = new RefundPaymentRequest
            {
                PaymentId = paymentId,
                BookingId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
                TenantId = tenantId,
                RefundAmount = 50.00m,
                Currency = "USD",
                RefundReason = "Customer request"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _paymentService.ProcessRefundAsync(request));
        }

        [Fact]
        public async Task GetPaymentDetailsAsync_ValidId_ReturnsPaymentDetails()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            
            var payment = new Payment
            {
                Id = paymentId,
                BookingId = Guid.NewGuid(),
                Amount = 100.00m,
                Currency = "USD",
                PaymentMethod = "CreditCard",
                PaymentStatus = "Completed",
                TransactionId = "txn_123456",
                PaymentGateway = "SimulatedGateway",
                PaidAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                RefundAmount = 0,
                TenantId = tenantId
            };

            _mockDbContext.Setup(db => db.Payments.FindAsync(paymentId))
                .Returns(new ValueTask<Payment?>(payment));

            // Act
            var result = await _paymentService.GetPaymentDetailsAsync(paymentId, tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(payment.Id, result.Id);
            Assert.Equal(payment.BookingId, result.BookingId);
            Assert.Equal(payment.Amount, result.Amount);
            Assert.Equal(payment.Currency, result.Currency);
            Assert.Equal(payment.PaymentMethod, result.PaymentMethod);
            Assert.Equal(payment.PaymentStatus, result.PaymentStatus);
            Assert.Equal(payment.TransactionId, result.TransactionId);
            Assert.Equal(payment.PaymentGateway, result.PaymentGateway);
            Assert.Equal(payment.PaidAt, result.PaidAt);
            Assert.Equal(payment.CreatedAt, result.CreatedAt);
            Assert.Equal(payment.UpdatedAt, result.UpdatedAt);
            Assert.Equal(payment.RefundAmount, result.RefundAmount);
            Assert.Equal(payment.TenantId, result.TenantId);
        }

        [Fact]
        public async Task GetPaymentDetailsAsync_InvalidPaymentId_ThrowsArgumentException()
        {
            // Arrange
            var paymentId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            _mockDbContext.Setup(db => db.Payments.FindAsync(paymentId))
                .Returns(new ValueTask<Payment?>((Payment?)null));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _paymentService.GetPaymentDetailsAsync(paymentId, tenantId));
        }

        [Fact]
        public async Task CreateSubscriptionAsync_ValidRequest_ReturnsPaymentDetails()
        {
            // Arrange
            var request = new CreateSubscriptionRequest
            {
                CustomerId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                ServiceId = Guid.NewGuid(),
                Amount = 50.00m,
                Currency = "USD",
                Frequency = "Monthly",
                PaymentMethod = "CreditCard",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(1)
            };

            // Act
            var result = await _paymentService.CreateSubscriptionAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(Guid.Empty, result.BookingId); // Placeholder value
            Assert.Equal(request.Amount, result.Amount);
            Assert.Equal(request.Currency, result.Currency);
            Assert.Equal(request.PaymentMethod, result.PaymentMethod);
            Assert.Equal("SubscriptionCreated", result.PaymentStatus);
            Assert.NotNull(result.TransactionId);
            Assert.NotNull(result.PaymentGateway);
            Assert.NotNull(result.PaidAt);
        }
    }
}