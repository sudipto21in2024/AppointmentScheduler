using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ServiceManagementService.Services;
using ServiceManagementService.Validators;
using Shared.Data;
using Shared.Models;
using Shared.Events;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using System.Threading.Tasks;

namespace ServiceManagementService.Tests
{
    public class ServiceServiceTests
    {
        private readonly Mock<ILogger<ServiceService>> _loggerMock;
        private readonly Mock<IPublishEndpoint> _publishEndpointMock;
        private readonly Mock<IServiceValidator> _validatorMock;
        private readonly ServiceService _serviceService;
        private readonly ApplicationDbContext _dbContext;

        public ServiceServiceTests()
        {
            // Create a simple in-memory database context for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_Service")
                .Options;
            _dbContext = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<ServiceService>>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();
            _validatorMock = new Mock<IServiceValidator>();
            _serviceService = new ServiceService(_dbContext, _validatorMock.Object, _publishEndpointMock.Object);
        }

        [Fact]
        public async Task CreateServiceAsync_ValidRequest_CreatesService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();

            // Add a provider user to the database
            var provider = new User
            {
                Id = userId,
                Email = "provider@test.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "Provider",
                UserType = UserRole.Provider,
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(provider);

            // Add a category to the database
            var category = new ServiceCategory
            {
                Id = categoryId,
                Name = "Test Category",
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.ServiceCategories.Add(category);
            await _dbContext.SaveChangesAsync();

            var request = new CreateServiceRequest
            {
                Name = "Test Service",
                Description = "Test Description",
                CategoryId = categoryId,
                Duration = 60,
                Price = 100.00m,
                Currency = "USD",
                IsActive = true,
                IsFeatured = false,
                MaxBookingsPerDay = 5
            };

            // Setup validator to return valid result
            _validatorMock.Setup(v => v.ValidateCreateServiceRequestAsync(It.IsAny<CreateServiceRequest>(), It.IsAny<Guid>()))
                .ReturnsAsync(new ValidationResult { IsValid = true });

            // Act
            var result = await _serviceService.CreateServiceAsync(request, userId, tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(request.Name, result.Name);
            Assert.Equal(request.Description, result.Description);
            Assert.Equal(request.CategoryId, result.CategoryId);
            Assert.Equal(userId, result.ProviderId);
            Assert.Equal(tenantId, result.TenantId);
            Assert.Equal(request.Duration, result.Duration);
            Assert.Equal(request.Price, result.Price);
            Assert.Equal(request.Currency, result.Currency);
            Assert.Equal(request.IsActive, result.IsActive);
            Assert.Equal(request.IsFeatured, result.IsFeatured);
            Assert.Equal(request.MaxBookingsPerDay, result.MaxBookingsPerDay);

            // Verify that the event was published
            _publishEndpointMock.Verify(p => p.Publish(It.IsAny<ServiceCreatedEvent>(), default), Times.Once);
        }

        [Fact]
        public async Task CreateServiceAsync_InvalidUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            // Add a customer user to the database (not a provider)
            var customer = new User
            {
                Id = userId,
                Email = "customer@test.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "Customer",
                UserType = UserRole.Customer,
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(customer);
            await _dbContext.SaveChangesAsync();

            var request = new CreateServiceRequest
            {
                Name = "Test Service",
                Description = "Test Description",
                CategoryId = Guid.NewGuid(),
                Duration = 60,
                Price = 100.00m,
                Currency = "USD",
                IsActive = true,
                IsFeatured = false,
                MaxBookingsPerDay = 5
            };

            // Setup validator to return valid result
            _validatorMock.Setup(v => v.ValidateCreateServiceRequestAsync(It.IsAny<CreateServiceRequest>(), It.IsAny<Guid>()))
                .ReturnsAsync(new ValidationResult { IsValid = true });

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                _serviceService.CreateServiceAsync(request, userId, tenantId));
        }

        [Fact]
        public async Task GetServiceByIdAsync_ExistingService_ReturnsService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var providerId = Guid.NewGuid();

            // Add a provider user to the database
            var provider = new User
            {
                Id = providerId,
                Email = "provider@test.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "Provider",
                UserType = UserRole.Provider,
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(provider);

            // Add a category to the database
            var category = new ServiceCategory
            {
                Id = categoryId,
                Name = "Test Category",
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.ServiceCategories.Add(category);

            // Add a service to the database
            var service = new Service
            {
                Id = serviceId,
                Name = "Test Service",
                Description = "Test Description",
                CategoryId = categoryId,
                ProviderId = providerId,
                TenantId = tenantId,
                Duration = 60,
                Price = 100.00m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Services.Add(service);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _serviceService.GetServiceByIdAsync(serviceId, userId, tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(serviceId, result.Id);
            Assert.Equal("Test Service", result.Name);
        }

        [Fact]
        public async Task GetServiceByIdAsync_NonExistentService_ThrowsKeyNotFoundException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => 
                _serviceService.GetServiceByIdAsync(serviceId, userId, tenantId));
        }

        [Fact]
        public async Task UpdateServiceAsync_ValidRequest_UpdatesService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();

            // Add a provider user to the database
            var provider = new User
            {
                Id = userId,
                Email = "provider@test.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "Provider",
                UserType = UserRole.Provider,
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(provider);

            // Add a category to the database
            var category = new ServiceCategory
            {
                Id = categoryId,
                Name = "Test Category",
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.ServiceCategories.Add(category);

            // Add a service to the database
            var service = new Service
            {
                Id = serviceId,
                Name = "Test Service",
                Description = "Test Description",
                CategoryId = categoryId,
                ProviderId = userId,
                TenantId = tenantId,
                Duration = 60,
                Price = 100.00m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Services.Add(service);
            await _dbContext.SaveChangesAsync();

            var request = new UpdateServiceRequest
            {
                Name = "Updated Service",
                Description = "Updated Description",
                Duration = 90,
                Price = 150.00m,
                IsActive = false
            };

            // Setup validator to return valid result
            _validatorMock.Setup(v => v.ValidateUpdateServiceRequestAsync(It.IsAny<UpdateServiceRequest>()))
                .ReturnsAsync(new ValidationResult { IsValid = true });

            // Act
            var result = await _serviceService.UpdateServiceAsync(serviceId, request, userId, tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated Service", result.Name);
            Assert.Equal("Updated Description", result.Description);
            Assert.Equal(90, result.Duration);
            Assert.Equal(150.00m, result.Price);
            Assert.False(result.IsActive);

            // Verify that the event was published
            _publishEndpointMock.Verify(p => p.Publish(It.IsAny<ServiceUpdatedEvent>(), default), Times.Once);
        }

        [Fact]
        public async Task UpdateServiceAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();
            var providerId = Guid.NewGuid();

            // Add a provider user to the database
            var provider = new User
            {
                Id = providerId,
                Email = "provider@test.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "Provider",
                UserType = UserRole.Provider,
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(provider);

            // Add a service to the database owned by a different provider
            var service = new Service
            {
                Id = serviceId,
                Name = "Test Service",
                Description = "Test Description",
                CategoryId = Guid.NewGuid(),
                ProviderId = providerId, // Different provider
                TenantId = tenantId,
                Duration = 60,
                Price = 100.00m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Services.Add(service);
            await _dbContext.SaveChangesAsync();

            var request = new UpdateServiceRequest
            {
                Name = "Updated Service"
            };

            // Setup validator to return valid result
            _validatorMock.Setup(v => v.ValidateUpdateServiceRequestAsync(It.IsAny<UpdateServiceRequest>()))
                .ReturnsAsync(new ValidationResult { IsValid = true });

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
                _serviceService.UpdateServiceAsync(serviceId, request, userId, tenantId));
        }

        [Fact]
        public async Task DeleteServiceAsync_ExistingService_DeletesService()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();

            // Add a provider user to the database
            var provider = new User
            {
                Id = userId,
                Email = "provider@test.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "Provider",
                UserType = UserRole.Provider,
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(provider);

            // Add a service to the database
            var service = new Service
            {
                Id = serviceId,
                Name = "Test Service",
                Description = "Test Description",
                CategoryId = Guid.NewGuid(),
                ProviderId = userId,
                TenantId = tenantId,
                Duration = 60,
                Price = 100.00m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Services.Add(service);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _serviceService.DeleteServiceAsync(serviceId, userId, tenantId);

            // Assert
            Assert.True(result);

            // Verify that the service was deleted
            var deletedService = await _dbContext.Services.FindAsync(serviceId);
            Assert.Null(deletedService);

            // Verify that the event was published
            _publishEndpointMock.Verify(p => p.Publish(It.IsAny<ServiceDeletedEvent>(), default), Times.Once);
        }

        [Fact]
        public async Task GetServicesAsync_ReturnsServices()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            var providerId = Guid.NewGuid();

            // Add a provider user to the database
            var provider = new User
            {
                Id = providerId,
                Email = "provider@test.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "Provider",
                UserType = UserRole.Provider,
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(provider);

            // Add a category to the database
            var category = new ServiceCategory
            {
                Id = categoryId,
                Name = "Test Category",
                TenantId = tenantId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.ServiceCategories.Add(category);

            // Add services to the database
            var service1 = new Service
            {
                Id = Guid.NewGuid(),
                Name = "Test Service 1",
                Description = "Test Description 1",
                CategoryId = categoryId,
                ProviderId = providerId,
                TenantId = tenantId,
                Duration = 60,
                Price = 100.00m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Services.Add(service1);

            var service2 = new Service
            {
                Id = Guid.NewGuid(),
                Name = "Test Service 2",
                Description = "Test Description 2",
                CategoryId = categoryId,
                ProviderId = providerId,
                TenantId = tenantId,
                Duration = 90,
                Price = 150.00m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Services.Add(service2);

            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _serviceService.GetServicesAsync(userId, tenantId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }
    }
}