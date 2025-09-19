using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.Data;
using Shared.Models;
using UserService.DTO;
using UserService.Services;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace UserService.Tests.Services
{
    public class TenantServiceTests : IDisposable
    {
        private readonly TenantService _tenantService;
        private readonly ApplicationDbContext _dbContext;
        private readonly List<Tenant> _tenants;

        public TenantServiceTests()
        {
            _tenants = new List<Tenant>
            {
                new Tenant { Id = Guid.NewGuid(), Name = "Tenant A", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                new Tenant { Id = Guid.NewGuid(), Name = "Tenant B", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
            };

            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name for each test
                .Options;
            
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _dbContext = new ApplicationDbContext(options, mockHttpContextAccessor.Object);
            _dbContext.Database.EnsureDeleted(); // Ensure clean state for each test
            _dbContext.Database.EnsureCreated(); // Create the schema

            // Seed data
            _dbContext.Tenants.AddRange(_tenants);
            _dbContext.SaveChanges();

            _tenantService = new TenantService(_dbContext);
        }

        [Fact]
        public async Task CreateTenantAsync_ShouldAddTenantToDatabase()
        {
            // Arrange
            var request = new CreateTenantRequest { Name = "New Tenant", Description = "Test Description", Domain = "test.com", ContactEmail = "test@test.com" };

            // Act
            var newTenant = await _tenantService.CreateTenantAsync(request);

            // Assert
            Assert.NotNull(newTenant);
            Assert.Equal(request.Name, newTenant.Name);
            Assert.Contains(newTenant, _dbContext.Tenants);
        }

        [Fact]
        public async Task GetTenantByIdAsync_ShouldReturnTenant_WhenTenantExists()
        {
            // Arrange
            var existingTenant = _tenants.First();

            // Act
            var result = await _tenantService.GetTenantByIdAsync(existingTenant.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingTenant.Id, result.Id);
        }

        [Fact]
        public async Task GetTenantByIdAsync_ShouldReturnNull_WhenTenantDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _tenantService.GetTenantByIdAsync(nonExistentId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllTenantsAsync_ShouldReturnAllTenants()
        {
            // Act
            var result = await _tenantService.GetAllTenantsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(_tenants.Count, result.Count());
        }

        [Fact]
        public async Task UpdateTenantAsync_ShouldUpdateTenant_WhenTenantExists()
        {
            // Arrange
            var existingTenant = _tenants.First();
            var updateRequest = new UpdateTenantRequest { Name = "Updated Tenant Name", Description = "Updated Description" };

            // Act
            var updatedTenant = await _tenantService.UpdateTenantAsync(existingTenant.Id, updateRequest);

            // Assert
            Assert.NotNull(updatedTenant);
            Assert.Equal(updateRequest.Name, updatedTenant.Name);
            Assert.Equal(updateRequest.Description, updatedTenant.Description);
            Assert.Equal(updatedTenant.Name, _dbContext.Tenants.Find(existingTenant.Id)?.Name);
        }

        [Fact]
        public async Task UpdateTenantAsync_ShouldReturnNull_WhenTenantDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();
            var updateRequest = new UpdateTenantRequest { Name = "Updated Tenant Name" };

            // Act
            var result = await _tenantService.UpdateTenantAsync(nonExistentId, updateRequest);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteTenantAsync_ShouldRemoveTenant_WhenTenantExists()
        {
            // Arrange
            var existingTenant = _tenants.First();

            // Act
            var result = await _tenantService.DeleteTenantAsync(existingTenant.Id);

            // Assert
            Assert.True(result);
            Assert.DoesNotContain(existingTenant, _dbContext.Tenants);
        }

        [Fact]
        public async Task DeleteTenantAsync_ShouldReturnFalse_WhenTenantDoesNotExist()
        {
            // Arrange
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await _tenantService.DeleteTenantAsync(nonExistentId);

            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}