using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ServiceManagementService.Validators;
using Shared.Data;
using Shared.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Shared.DTOs;

namespace ServiceManagementService.Tests
{
    public class SlotValidatorTests
    {
        private readonly Mock<ILogger<SlotValidator>> _loggerMock;
        private readonly SlotValidator _validator;
        private readonly ApplicationDbContext _dbContext;

        public SlotValidatorTests()
        {
            // Create a simple in-memory database context for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase_SlotValidator")
                .Options;
            _dbContext = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<SlotValidator>>();
            _validator = new SlotValidator(_dbContext);
        }

        [Fact]
        public async Task ValidateCreateSlotRequestAsync_ValidRequest_ReturnsValidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();
            
            // Add a service to the database
            var service = new Service
            {
                Id = serviceId,
                Name = "Test Service",
                Description = "Test Description",
                CategoryId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
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

            var request = new CreateSlotRequest
            {
                ServiceId = serviceId,
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                MaxBookings = 5,
                IsAvailable = true,
                IsRecurring = false
            };

            // Act
            var result = await _validator.ValidateCreateSlotRequestAsync(request, tenantId);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateCreateSlotRequestAsync_MissingServiceId_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var request = new CreateSlotRequest
            {
                ServiceId = Guid.Empty, // Missing service ID
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                MaxBookings = 5,
                IsAvailable = true,
                IsRecurring = false
            };

            // Act
            var result = await _validator.ValidateCreateSlotRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Service ID is required", result.Errors);
        }

        [Fact]
        public async Task ValidateCreateSlotRequestAsync_InvalidServiceId_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var request = new CreateSlotRequest
            {
                ServiceId = Guid.NewGuid(), // Non-existent service
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                MaxBookings = 5,
                IsAvailable = true,
                IsRecurring = false
            };

            // Act
            var result = await _validator.ValidateCreateSlotRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Invalid service ID", result.Errors);
        }

        [Fact]
        public async Task ValidateCreateSlotRequestAsync_StartAfterEnd_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();
            
            // Add a service to the database
            var service = new Service
            {
                Id = serviceId,
                Name = "Test Service",
                Description = "Test Description",
                CategoryId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
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

            var request = new CreateSlotRequest
            {
                ServiceId = serviceId,
                StartDateTime = DateTime.UtcNow.AddHours(2), // Start after end
                EndDateTime = DateTime.UtcNow.AddHours(1),   // End before start
                MaxBookings = 5,
                IsAvailable = true,
                IsRecurring = false
            };

            // Act
            var result = await _validator.ValidateCreateSlotRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Start date/time must be before end date/time", result.Errors);
        }

        [Fact]
        public async Task ValidateCreateSlotRequestAsync_SlotConflict_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();
            var existingSlotStart = DateTime.UtcNow.AddHours(1);
            var existingSlotEnd = DateTime.UtcNow.AddHours(2);
            
            // Add a service to the database
            var service = new Service
            {
                Id = serviceId,
                Name = "Test Service",
                Description = "Test Description",
                CategoryId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
                TenantId = tenantId,
                Duration = 60,
                Price = 100.00m,
                Currency = "USD",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Services.Add(service);
            
            // Add an existing slot that conflicts
            var existingSlot = new Slot
            {
                Id = Guid.NewGuid(),
                ServiceId = serviceId,
                StartDateTime = existingSlotStart,
                EndDateTime = existingSlotEnd,
                MaxBookings = 5,
                AvailableBookings = 5,
                IsAvailable = true,
                TenantId = tenantId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Slots.Add(existingSlot);
            await _dbContext.SaveChangesAsync();

            var request = new CreateSlotRequest
            {
                ServiceId = serviceId,
                StartDateTime = existingSlotStart.AddMinutes(30), // Conflicts with existing slot
                EndDateTime = existingSlotEnd.AddMinutes(30),     // Conflicts with existing slot
                MaxBookings = 5,
                IsAvailable = true,
                IsRecurring = false
            };

            // Act
            var result = await _validator.ValidateCreateSlotRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Slot conflicts with existing slot for this service", result.Errors);
        }

        [Fact]
        public async Task ValidateUpdateSlotRequestAsync_ValidRequest_ReturnsValidResult()
        {
            // Arrange
            var request = new UpdateSlotRequest
            {
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                MaxBookings = 10,
                IsAvailable = false
            };

            // Act
            var result = await _validator.ValidateUpdateSlotRequestAsync(request);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateUpdateSlotRequestAsync_StartAfterEnd_ReturnsInvalidResult()
        {
            // Arrange
            var request = new UpdateSlotRequest
            {
                StartDateTime = DateTime.UtcNow.AddHours(2), // Start after end
                EndDateTime = DateTime.UtcNow.AddHours(1)    // End before start
            };

            // Act
            var result = await _validator.ValidateUpdateSlotRequestAsync(request);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Start date/time must be before end date/time", result.Errors);
        }

        [Fact]
        public async Task ValidateRecurringSlotRequestAsync_ValidRequest_ReturnsValidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var serviceId = Guid.NewGuid();
            
            // Add a service to the database
            var service = new Service
            {
                Id = serviceId,
                Name = "Test Service",
                Description = "Test Description",
                CategoryId = Guid.NewGuid(),
                ProviderId = Guid.NewGuid(),
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

            var request = new CreateRecurringSlotsRequest
            {
                ServiceId = serviceId,
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                MaxBookings = 5,
                Pattern = RecurrencePattern.Daily,
                Interval = 1,
                Occurrences = 10,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            // Act
            var result = await _validator.ValidateRecurringSlotRequestAsync(request, tenantId);

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ValidateRecurringSlotRequestAsync_InvalidInterval_ReturnsInvalidResult()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var request = new CreateRecurringSlotsRequest
            {
                ServiceId = Guid.NewGuid(),
                StartDateTime = DateTime.UtcNow.AddHours(1),
                EndDateTime = DateTime.UtcNow.AddHours(2),
                MaxBookings = 5,
                Pattern = RecurrencePattern.Daily,
                Interval = 0, // Invalid interval
                Occurrences = 10,
                EndDate = DateTime.UtcNow.AddDays(30)
            };

            // Act
            var result = await _validator.ValidateRecurringSlotRequestAsync(request, tenantId);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Interval must be greater than 0", result.Errors);
        }
    }
}