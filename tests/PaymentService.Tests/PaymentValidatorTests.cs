using System;
using System.Threading.Tasks;
using Moq;
using Shared.Data;
using Shared.DTOs;
using Shared.Models;
using Microsoft.EntityFrameworkCore;
using PaymentService.Validators;
using Microsoft.EntityFrameworkCore.InMemory;

namespace PaymentService.Tests
{
    public class PaymentValidatorTests
    {
        private readonly Mock<ApplicationDbContext> _mockDbContext;
        private readonly PaymentValidator _paymentValidator;

        public PaymentValidatorTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "PaymentValidatorTestDb")
                .Options;
            
            _mockDbContext = new Mock<ApplicationDbContext>(options);
            _paymentValidator = new PaymentValidator(_mockDbContext.Object);
        }

        [Fact]
        public async Task ValidateProcessPaymentRequestAsync_ValidRequest_ReturnsValidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var providerId = Guid.NewGuid();

            var booking = new Booking { Id = bookingId, TenantId = tenantId };
            var customer = new User { Id = customerId, TenantId = tenantId };
            var provider = new User { Id = providerId, TenantId = tenantId };

            _mockDbContext.Setup(db => db.Bookings.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(), default))
                .ReturnsAsync(booking);
            _mockDbContext.Setup(db => db.Users.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), default))
                .ReturnsAsync(customer);
            _mockDbContext.Setup(db => db.Users.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), default))
                .ReturnsAsync(provider);

            var request = new ProcessPaymentRequest
            {
                BookingId = bookingId,
                CustomerId = customerId,
                ProviderId = providerId,
                TenantId = tenantId,
                Amount = 100.00m,
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };

            // Act
            var result = await _paymentValidator.ValidateProcessPaymentRequestAsync(request, tenantId);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateProcessPaymentRequestAsync_MissingBookingId_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();

            var request = new ProcessPaymentRequest
            {
                BookingId = Guid.Empty, // Missing
                CustomerId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
                TenantId = tenantId,
                Amount = 100.00m,
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };

            // Act
            var result = await _paymentValidator.ValidateProcessPaymentRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Booking ID is required", result.Errors);
        }

        [Fact]
        public async Task ValidateProcessPaymentRequestAsync_InvalidBooking_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();

            _mockDbContext.Setup(db => db.Bookings.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(), default))
                .ReturnsAsync((Booking?)null);

            var request = new ProcessPaymentRequest
            {
                BookingId = bookingId,
                CustomerId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
                TenantId = tenantId,
                Amount = 100.00m,
                Currency = "USD",
                PaymentMethod = "CreditCard"
            };

            // Act
            var result = await _paymentValidator.ValidateProcessPaymentRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Invalid booking ID or booking does not belong to the tenant", result.Errors);
        }

        [Fact]
        public async Task ValidateRefundPaymentRequestAsync_ValidRequest_ReturnsValidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();
            var bookingId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var providerId = Guid.NewGuid();

            var payment = new Payment 
            { 
                Id = paymentId, 
                TenantId = tenantId, 
                PaymentStatus = "Completed", 
                Amount = 100.00m 
            };
            var booking = new Booking { Id = bookingId, TenantId = tenantId };
            var customer = new User { Id = customerId, TenantId = tenantId };
            var provider = new User { Id = providerId, TenantId = tenantId };

            _mockDbContext.Setup(db => db.Payments.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>(), default))
                .ReturnsAsync(payment);
            _mockDbContext.Setup(db => db.Bookings.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Booking, bool>>>(), default))
                .ReturnsAsync(booking);
            _mockDbContext.Setup(db => db.Users.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), default))
                .ReturnsAsync(customer);
            _mockDbContext.Setup(db => db.Users.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), default))
                .ReturnsAsync(provider);

            var request = new RefundPaymentRequest
            {
                PaymentId = paymentId,
                BookingId = bookingId,
                CustomerId = customerId,
                ProviderId = providerId,
                TenantId = tenantId,
                RefundAmount = 50.00m,
                Currency = "USD",
                RefundReason = "Customer request"
            };

            // Act
            var result = await _paymentValidator.ValidateRefundPaymentRequestAsync(request, tenantId);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateRefundPaymentRequestAsync_AlreadyRefunded_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var payment = new Payment 
            { 
                Id = paymentId, 
                TenantId = tenantId, 
                PaymentStatus = "Refunded", 
                Amount = 100.00m 
            };

            _mockDbContext.Setup(db => db.Payments.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>(), default))
                .ReturnsAsync(payment);

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

            // Act
            var result = await _paymentValidator.ValidateRefundPaymentRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Payment has already been refunded", result.Errors);
        }

        [Fact]
        public async Task ValidateRefundPaymentRequestAsync_RefundAmountExceedsPaymentAmount_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var paymentId = Guid.NewGuid();

            var payment = new Payment 
            { 
                Id = paymentId, 
                TenantId = tenantId, 
                PaymentStatus = "Completed", 
                Amount = 100.00m 
            };

            _mockDbContext.Setup(db => db.Payments.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Payment, bool>>>(), default))
                .ReturnsAsync(payment);

            var request = new RefundPaymentRequest
            {
                PaymentId = paymentId,
                BookingId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
                TenantId = tenantId,
                RefundAmount = 150.00m, // Exceeds payment amount
                Currency = "USD",
                RefundReason = "Customer request"
            };

            // Act
            var result = await _paymentValidator.ValidateRefundPaymentRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Refund amount cannot be greater than the original payment amount", result.Errors);
        }

        [Fact]
        public async Task ValidateCreateSubscriptionRequestAsync_ValidRequest_ReturnsValidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var customerId = Guid.NewGuid();
            var providerId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();

            var customer = new User { Id = customerId, TenantId = tenantId };
            var provider = new User { Id = providerId, TenantId = tenantId };
            var service = new Service { Id = serviceId, TenantId = tenantId };

            _mockDbContext.Setup(db => db.Users.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), default))
                .ReturnsAsync(customer);
            _mockDbContext.Setup(db => db.Users.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), default))
                .ReturnsAsync(provider);
            _mockDbContext.Setup(db => db.Services.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Service, bool>>>(), default))
                .ReturnsAsync(service);

            var request = new CreateSubscriptionRequest
            {
                CustomerId = customerId,
                ProviderId = providerId,
                TenantId = tenantId,
                ServiceId = serviceId,
                Amount = 50.00m,
                Currency = "USD",
                Frequency = "Monthly",
                PaymentMethod = "CreditCard",
                StartDate = DateTime.UtcNow.AddDays(1), // Future date
                EndDate = DateTime.UtcNow.AddYears(1)
            };

            // Act
            var result = await _paymentValidator.ValidateCreateSubscriptionRequestAsync(request, tenantId);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateCreateSubscriptionRequestAsync_StartDateInPast_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();

            var request = new CreateSubscriptionRequest
            {
                CustomerId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
                TenantId = tenantId,
                ServiceId = Guid.NewGuid(),
                Amount = 50.00m,
                Currency = "USD",
                Frequency = "Monthly",
                PaymentMethod = "CreditCard",
                StartDate = DateTime.UtcNow.AddDays(-1), // Past date
                EndDate = DateTime.UtcNow.AddYears(1)
            };

            // Act
            var result = await _paymentValidator.ValidateCreateSubscriptionRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Start date cannot be in the past", result.Errors);
        }

        [Fact]
        public async Task ValidateCreateSubscriptionRequestAsync_EndDateBeforeStartDate_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();

            var request = new CreateSubscriptionRequest
            {
                CustomerId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
                TenantId = tenantId,
                ServiceId = Guid.NewGuid(),
                Amount = 50.00m,
                Currency = "USD",
                Frequency = "Monthly",
                PaymentMethod = "CreditCard",
                StartDate = DateTime.UtcNow.AddDays(10),
                EndDate = DateTime.UtcNow.AddDays(5) // Before start date
            };

            // Act
            var result = await _paymentValidator.ValidateCreateSubscriptionRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("End date must be after start date", result.Errors);
        }
    }
}