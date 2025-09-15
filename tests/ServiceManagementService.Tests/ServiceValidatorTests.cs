using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ServiceManagementService.Validators;
using Shared.Data;
using Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ServiceManagementService.Tests
{
    public class ServiceValidatorTests
    {
        private readonly Mock<ILogger<ServiceValidator>> _loggerMock;
        private readonly ServiceValidator _validator;
        private readonly ApplicationDbContext _dbContext;

        public ServiceValidatorTests()
        {
            // Create a simple in-memory database context for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _dbContext = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<ServiceValidator>>();
            _validator = new ServiceValidator(_dbContext);
        }

        [Fact]
        public async Task ValidateCreateServiceRequestAsync_ValidRequest_ReturnsValidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            
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

            // Act
            var result = await _validator.ValidateCreateServiceRequestAsync(request, tenantId);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateCreateServiceRequestAsync_MissingName_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var request = new CreateServiceRequest
            {
                Name = "",
                Description = "Test Description",
                CategoryId = Guid.NewGuid(),
                Duration = 60,
                Price = 100.00m,
                Currency = "USD",
                IsActive = true,
                IsFeatured = false,
                MaxBookingsPerDay = 5
            };

            // Act
            var result = await _validator.ValidateCreateServiceRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Service name is required", result.Errors);
        }

        [Fact]
        public async Task ValidateCreateServiceRequestAsync_InvalidCategoryId_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var request = new CreateServiceRequest
            {
                Name = "Test Service",
                Description = "Test Description",
                CategoryId = Guid.NewGuid(), // Non-existent category
                Duration = 60,
                Price = 100.00m,
                Currency = "USD",
                IsActive = true,
                IsFeatured = false,
                MaxBookingsPerDay = 5
            };

            // Act
            var result = await _validator.ValidateCreateServiceRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Invalid category ID", result.Errors);
        }

        [Fact]
        public async Task ValidateCreateServiceRequestAsync_DuplicateName_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var categoryId = Guid.NewGuid();
            
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
            
            // Add an existing service with the same name
            var existingService = new Service
            {
                Id = Guid.NewGuid(),
                Name = "Test Service",
                Description = "Existing Service",
                CategoryId = categoryId,
                ProviderId = Guid.NewGuid(),
                TenantId = tenantId,
                Duration = 30,
                Price = 50.00m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Services.Add(existingService);
            await _dbContext.SaveChangesAsync();

            var request = new CreateServiceRequest
            {
                Name = "Test Service", // Same name as existing service
                Description = "Test Description",
                CategoryId = categoryId,
                Duration = 60,
                Price = 100.00m,
                Currency = "USD",
                IsActive = true,
                IsFeatured = false,
                MaxBookingsPerDay = 5
            };

            // Act
            var result = await _validator.ValidateCreateServiceRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Service name must be unique within tenant", result.Errors);
        }

        [Fact]
        public async Task ValidateUpdateServiceRequestAsync_ValidRequest_ReturnsValidResult()
        {
            // Arrange
            var request = new UpdateServiceRequest
            {
                Name = "Updated Service",
                Description = "Updated Description",
                CategoryId = Guid.NewGuid(),
                Duration = 90,
                Price = 150.00m,
                Currency = "EUR",
                IsActive = false,
                IsFeatured = true,
                MaxBookingsPerDay = 10
            };

            // Act
            var result = await _validator.ValidateUpdateServiceRequestAsync(request);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateUpdateServiceRequestAsync_InvalidDuration_ReturnsInvalidResult()
        {
            // Arrange
            var request = new UpdateServiceRequest
            {
                Duration = -10 // Invalid duration
            };

            // Act
            var result = await _validator.ValidateUpdateServiceRequestAsync(request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Service duration must be greater than 0", result.Errors);
        }

        [Fact]
        public async Task ValidateCreateCategoryRequestAsync_ValidRequest_ReturnsValidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var request = new CreateCategoryRequest
            {
                Name = "Test Category",
                Description = "Test Description",
                ParentCategoryId = null,
                IconUrl = "http://example.com/icon.png",
                SortOrder = 1,
                IsActive = true
            };

            // Act
            var result = await _validator.ValidateCreateCategoryRequestAsync(request, tenantId);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateCreateCategoryRequestAsync_MissingName_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var request = new CreateCategoryRequest
            {
                Name = "", // Missing name
                Description = "Test Description",
                ParentCategoryId = null,
                IconUrl = "http://example.com/icon.png",
                SortOrder = 1,
                IsActive = true
            };

            // Act
            var result = await _validator.ValidateCreateCategoryRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Category name is required", result.Errors);
        }

        [Fact]
        public async Task ValidateUpdateCategoryRequestAsync_ValidRequest_ReturnsValidResult()
        {
            // Arrange
            var request = new UpdateCategoryRequest
            {
                Name = "Updated Category",
                Description = "Updated Description",
                ParentCategoryId = Guid.NewGuid(),
                IconUrl = "http://example.com/updated-icon.png",
                SortOrder = 2,
                IsActive = false
            };

            // Act
            var result = await _validator.ValidateUpdateCategoryRequestAsync(request);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }
    }
}