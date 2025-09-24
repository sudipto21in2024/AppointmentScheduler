using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Data;
using Shared.Models;
using Shared.DTOs;
using Shared.Contracts;
using PaymentService.Services;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Tests
{
    public class PaymentMethodServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<PaymentMethodService>> _mockLogger;
        private readonly PaymentMethodService _paymentMethodService;

        public PaymentMethodServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            ApplicationDbContext.OverrideTenantId = Guid.NewGuid();
            _context = new ApplicationDbContext(options, new Mock<IHttpContextAccessor>().Object);
            _mockLogger = new Mock<ILogger<PaymentMethodService>>();
    
            _paymentMethodService = new PaymentMethodService(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task GetPaymentMethodsAsync_ValidUserId_ReturnsPaymentMethods()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            var paymentMethod = new PaymentMethod
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = ApplicationDbContext.OverrideTenantId,
                Token = "tok_123",
                Type = "CreditCard",
                LastFourDigits = "1234",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            // Act
            var result = await _paymentMethodService.GetPaymentMethodsAsync(userId, null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            var method = result.First();
            Assert.Equal(paymentMethod.Id, method.Id);
            Assert.Equal(paymentMethod.Type, method.Type);
            Assert.Equal(paymentMethod.LastFourDigits, method.LastFourDigits);
            Assert.Equal(paymentMethod.IsDefault, method.IsDefault);
        }

        [Fact]
        public async Task AddPaymentMethodAsync_ValidRequest_ReturnsPaymentMethodDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreatePaymentMethodRequest
            {
                Type = "CreditCard",
                CardNumber = "4111111111111111",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                CVV = "123",
                CardholderName = "John Doe"
            };

            // Act
            var result = await _paymentMethodService.AddPaymentMethodAsync(userId, null, request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Type, result.Type);
            Assert.Equal("1111", result.LastFourDigits);
            Assert.Equal(request.ExpiryMonth, result.ExpiryMonth);
            Assert.Equal(request.ExpiryYear, result.ExpiryYear);
            Assert.True(result.IsDefault); // First method should be default
        }

        [Fact]
        public async Task AddPaymentMethodAsync_SecondMethod_IsNotDefault()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var firstMethod = new PaymentMethod
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = ApplicationDbContext.OverrideTenantId,
                Token = "tok_123",
                Type = "CreditCard",
                LastFourDigits = "1234",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentMethods.Add(firstMethod);
            await _context.SaveChangesAsync();

            var request = new CreatePaymentMethodRequest
            {
                Type = "DebitCard",
                CardNumber = "5555555555554444",
                ExpiryMonth = 11,
                ExpiryYear = 2026,
                CVV = "456"
            };

            // Act
            var result = await _paymentMethodService.AddPaymentMethodAsync(userId, null, request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsDefault); // Second method should not be default
        }

        [Fact]
        public async Task DeletePaymentMethodAsync_ValidId_DeletesMethod()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var methodId = Guid.NewGuid();

            var paymentMethod = new PaymentMethod
            {
                Id = methodId,
                UserId = userId,
                TenantId = ApplicationDbContext.OverrideTenantId,
                Token = "tok_123",
                Type = "CreditCard",
                LastFourDigits = "1234",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            // Act
            await _paymentMethodService.DeletePaymentMethodAsync(methodId, userId, null);

            // Assert
            var deletedMethod = await _context.PaymentMethods.FindAsync(methodId);
            Assert.Null(deletedMethod);
        }

        [Fact]
        public async Task DeletePaymentMethodAsync_DefaultMethod_SetsAnotherAsDefault()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var defaultMethodId = Guid.NewGuid();
            var otherMethodId = Guid.NewGuid();

            var defaultMethod = new PaymentMethod
            {
                Id = defaultMethodId,
                UserId = userId,
                TenantId = ApplicationDbContext.OverrideTenantId,
                Token = "tok_123",
                Type = "CreditCard",
                LastFourDigits = "1234",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var otherMethod = new PaymentMethod
            {
                Id = otherMethodId,
                UserId = userId,
                TenantId = ApplicationDbContext.OverrideTenantId,
                Token = "tok_456",
                Type = "DebitCard",
                LastFourDigits = "5678",
                ExpiryMonth = 11,
                ExpiryYear = 2026,
                IsDefault = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentMethods.AddRange(defaultMethod, otherMethod);
            await _context.SaveChangesAsync();

            // Act
            await _paymentMethodService.DeletePaymentMethodAsync(defaultMethodId, userId, null);

            // Assert
            var remainingMethod = await _context.PaymentMethods.FindAsync(otherMethodId);
            Assert.NotNull(remainingMethod);
            Assert.True(remainingMethod.IsDefault);
        }

        [Fact]
        public async Task SetDefaultPaymentMethodAsync_ValidId_SetsAsDefault()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var methodId = Guid.NewGuid();
            var otherMethodId = Guid.NewGuid();

            var method = new PaymentMethod
            {
                Id = methodId,
                UserId = userId,
                TenantId = ApplicationDbContext.OverrideTenantId,
                Token = "tok_123",
                Type = "CreditCard",
                LastFourDigits = "1234",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                IsDefault = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var otherMethod = new PaymentMethod
            {
                Id = otherMethodId,
                UserId = userId,
                TenantId = ApplicationDbContext.OverrideTenantId,
                Token = "tok_456",
                Type = "DebitCard",
                LastFourDigits = "5678",
                ExpiryMonth = 11,
                ExpiryYear = 2026,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentMethods.AddRange(method, otherMethod);
            await _context.SaveChangesAsync();

            // Act
            await _paymentMethodService.SetDefaultPaymentMethodAsync(methodId, userId, null);

            // Assert
            var updatedMethod = await _context.PaymentMethods.FindAsync(methodId);
            var updatedOtherMethod = await _context.PaymentMethods.FindAsync(otherMethodId);

            Assert.NotNull(updatedMethod);
            Assert.True(updatedMethod.IsDefault);

            Assert.NotNull(updatedOtherMethod);
            Assert.False(updatedOtherMethod.IsDefault);
        }

        [Fact]
        public async Task UpdatePaymentMethodAsync_ValidRequest_UpdatesMethod()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var methodId = Guid.NewGuid();

            var paymentMethod = new PaymentMethod
            {
                Id = methodId,
                UserId = userId,
                TenantId = ApplicationDbContext.OverrideTenantId,
                Token = "tok_123",
                Type = "CreditCard",
                LastFourDigits = "1234",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            var request = new UpdatePaymentMethodRequest
            {
                ExpiryMonth = 11,
                ExpiryYear = 2026
            };

            // Act
            await _paymentMethodService.UpdatePaymentMethodAsync(methodId, userId, null, request);

            // Assert
            var updatedMethod = await _context.PaymentMethods.FindAsync(methodId);
            Assert.NotNull(updatedMethod);
            Assert.Equal(11, updatedMethod.ExpiryMonth);
            Assert.Equal(2026, updatedMethod.ExpiryYear);
        }

        [Fact]
        public async Task GetPaymentMethodsAsync_InvalidUserId_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            var paymentMethod = new PaymentMethod
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TenantId = ApplicationDbContext.OverrideTenantId,
                Token = "tok_123",
                Type = "CreditCard",
                LastFourDigits = "1234",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                IsDefault = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentMethods.Add(paymentMethod);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _paymentMethodService.DeletePaymentMethodAsync(paymentMethod.Id, otherUserId, null));
        }
    }
}