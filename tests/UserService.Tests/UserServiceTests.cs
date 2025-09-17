using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using UserService.Services;
using Shared.Contracts;
using Shared.Models;
using Shared.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace UserService.Tests
{
    public class UserServiceTests
    {
        private readonly Mock<ILogger<UserService.Services.UserService>> _loggerMock;
        private readonly Mock<IEventStore> _eventStoreMock;
        private readonly UserService.Services.UserService _userService;
        private ApplicationDbContext _dbContext;

        public UserServiceTests()
        {
            _loggerMock = new Mock<ILogger<UserService.Services.UserService>>();
            _eventStoreMock = new Mock<IEventStore>();
            InitializeDbContext();
            _userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);
        }

        private void InitializeDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Use unique name for each test run
                .Options;
            _dbContext = new ApplicationDbContext(options);
            _dbContext.Database.EnsureDeleted(); // Clear previous data
            _dbContext.Database.EnsureCreated(); // Create new
        }

        [Fact]
        public async Task CreateUser_ValidUser_CreatesUserAndPublishesEvent()
        {
            // Arrange
            InitializeDbContext(); // Ensure a fresh context for this test
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);

            var newUser = new User
            {
                Email = "newuser@test.com",
                PasswordHash = "plainpassword", // This will be hashed internally
                FirstName = "New",
                LastName = "User",
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer
            };

            // Act
            var createdUser = await userService.CreateUser(newUser);

            // Assert
            Assert.NotNull(createdUser);
            Assert.NotEqual(Guid.Empty, createdUser.Id);
            Assert.True(BCrypt.Net.BCrypt.Verify("plainpassword", createdUser.PasswordHash));
            Assert.NotNull(createdUser.PasswordSalt);
            Assert.True(createdUser.IsActive);
            Assert.NotEqual(DateTime.MinValue, createdUser.CreatedAt);
            Assert.NotEqual(DateTime.MinValue, createdUser.UpdatedAt);

            // Verify event was published
            _eventStoreMock.Verify(es => es.Publish(It.Is<UserRegisteredEvent>(
                e => e.UserId == createdUser.Id &&
                     e.Email == createdUser.Email &&
                     e.TenantId == createdUser.TenantId)), Times.Once);
        }

        [Fact]
        public async Task UpdatePassword_ValidUserAndPassword_ReturnsTrueAndPublishesEvent()
        {
            // Arrange
            InitializeDbContext(); // Ensure a fresh context for this test
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword", BCrypt.Net.BCrypt.GenerateSalt()),
                PasswordSalt = BCrypt.Net.BCrypt.GenerateSalt(), // Dummy salt for existing user
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            string newPassword = "newpassword";

            // Act
            var result = await userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.True(result);
            Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash));
            _eventStoreMock.Verify(es => es.Publish(It.Is<PasswordChangedEvent>(
                e => e.UserId == user.Id && e.TenantId == user.TenantId)), Times.Once);
        }

        [Fact]
        public async Task UpdatePassword_NullUser_ReturnsFalse()
        {
            // Arrange
            InitializeDbContext(); // Ensure a fresh context for this test
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);
            User user = null!;
            string newPassword = "newpassword";

            // Act
            var result = await userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.False(result);
            _eventStoreMock.Verify(es => es.Publish(It.Any<IEvent>()), Times.Never);
        }

        [Fact]
        public async Task UpdatePassword_InactiveUser_ReturnsFalse()
        {
            // Arrange
            InitializeDbContext(); // Ensure a fresh context for this test
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword", BCrypt.Net.BCrypt.GenerateSalt()),
                PasswordSalt = BCrypt.Net.BCrypt.GenerateSalt(),
                FirstName = "Test",
                LastName = "User",
                IsActive = false, // Inactive user
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            string newPassword = "newpassword";

            // Act
            var result = await userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.False(result);
            _eventStoreMock.Verify(es => es.Publish(It.Any<IEvent>()), Times.Never);
        }

        [Fact]
        public async Task UpdatePassword_NullNewPassword_ReturnsFalse()
        {
            // Arrange
            InitializeDbContext(); // Ensure a fresh context for this test
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword", BCrypt.Net.BCrypt.GenerateSalt()),
                PasswordSalt = BCrypt.Net.BCrypt.GenerateSalt(),
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            string newPassword = null!;

            // Act
            var result = await userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.False(result);
            _eventStoreMock.Verify(es => es.Publish(It.Any<IEvent>()), Times.Never);
        }

        [Fact]
        public async Task UpdatePassword_EmptyNewPassword_ReturnsFalse()
        {
            // Arrange
            InitializeDbContext(); // Ensure a fresh context for this test
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword", BCrypt.Net.BCrypt.GenerateSalt()),
                PasswordSalt = BCrypt.Net.BCrypt.GenerateSalt(),
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            string newPassword = "";

            // Act
            var result = await userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.False(result);
            _eventStoreMock.Verify(es => es.Publish(It.Any<IEvent>()), Times.Never);
        }

        [Fact]
        public async Task GetUserByUsername_ActiveUser_ReturnsUser()
        {
            // Arrange
            InitializeDbContext();
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "active@test.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                FirstName = "Active",
                LastName = "User",
                IsActive = true,
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var foundUser = await userService.GetUserByUsername("active@test.com");

            // Assert
            Assert.NotNull(foundUser);
            Assert.Equal(user.Email, foundUser.Email);
        }

        [Fact]
        public async Task GetUserByUsername_InactiveUser_ReturnsNull()
        {
            // Arrange
            InitializeDbContext();
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "inactive@test.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                FirstName = "Inactive",
                LastName = "User",
                IsActive = false, // Inactive
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var foundUser = await userService.GetUserByUsername("inactive@test.com");

            // Assert
            Assert.Null(foundUser);
        }

        [Fact]
        public async Task GetUserById_ActiveUser_ReturnsUser()
        {
            // Arrange
            InitializeDbContext();
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "idtest@test.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                FirstName = "ID",
                LastName = "User",
                IsActive = true,
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var foundUser = await userService.GetUserById(user.Id);

            // Assert
            Assert.NotNull(foundUser);
            Assert.Equal(user.Id, foundUser.Id);
        }

        [Fact]
        public async Task GetUserById_InactiveUser_ReturnsNull()
        {
            // Arrange
            InitializeDbContext();
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "idtestinactive@test.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                FirstName = "ID",
                LastName = "Inactive",
                IsActive = false, // Inactive
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var foundUser = await userService.GetUserById(user.Id);

            // Assert
            Assert.Null(foundUser);
        }

        [Fact]
        public async Task UpdateUser_ValidUser_UpdatesUserAndPublishesEvent()
        {
            // Arrange
            InitializeDbContext();
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "update@test.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                FirstName = "Old",
                LastName = "Name",
                IsActive = true,
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            user.FirstName = "New";
            user.LastName = "Name";

            // Act
            var updatedUser = await userService.UpdateUser(user);

            // Assert
            Assert.NotNull(updatedUser);
            Assert.Equal("New", updatedUser.FirstName);
            Assert.Equal("Name", updatedUser.LastName);
            Assert.NotEqual(user.CreatedAt, updatedUser.UpdatedAt); // UpdatedAt should change

            _eventStoreMock.Verify(es => es.Publish(It.Is<UserUpdatedEvent>(
                e => e.UserId == user.Id &&
                     e.FirstName == "New" &&
                     e.LastName == "Name")), Times.Once);
        }

        [Fact]
        public async Task UpdateUser_InactiveUser_ReturnsNullAndDoesNotUpdate()
        {
            // Arrange
            InitializeDbContext();
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "inactiveupdate@test.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                FirstName = "Old",
                LastName = "Name",
                IsActive = false, // Inactive
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            user.FirstName = "New"; // Attempt to update

            // Act
            var updatedUser = await userService.UpdateUser(user);

            // Assert
            Assert.Null(updatedUser);
            var originalUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
            Assert.Equal("Old", originalUser?.FirstName); // Should not have updated

            _eventStoreMock.Verify(es => es.Publish(It.Any<IEvent>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUser_ValidUser_SoftDeletesUserAndPublishesEvent()
        {
            // Arrange
            InitializeDbContext();
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "delete@test.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                FirstName = "Delete",
                LastName = "Me",
                IsActive = true,
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await userService.DeleteUser(user.Id);

            // Assert
            Assert.True(result);
            var deletedUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            Assert.NotNull(deletedUser);
            Assert.False(deletedUser.IsActive); // Should be soft deleted

            _eventStoreMock.Verify(es => es.Publish(It.Is<UserDeletedEvent>(
                e => e.UserId == user.Id && e.TenantId == user.TenantId)), Times.Once);

            // Verify user is not returned by GetUserById
            var foundUser = await userService.GetUserById(user.Id);
            Assert.Null(foundUser);
        }

        [Fact]
        public async Task DeleteUser_NonExistentUser_ReturnsFalse()
        {
            // Arrange
            InitializeDbContext();
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = await userService.DeleteUser(nonExistentId);

            // Assert
            Assert.False(result);
            _eventStoreMock.Verify(es => es.Publish(It.Any<IEvent>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUser_AlreadyInactiveUser_ReturnsTrueAndDoesNotModify()
        {
            // Arrange
            InitializeDbContext();
            var userService = new UserService.Services.UserService(_dbContext, _loggerMock.Object, _eventStoreMock.Object);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "alreadyinactive@test.com",
                PasswordHash = "hash",
                PasswordSalt = "salt",
                FirstName = "Inactive",
                LastName = "User",
                IsActive = false, // Already inactive
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await userService.DeleteUser(user.Id);

            // Assert
            Assert.True(result);
            var dbUser = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
            Assert.False(dbUser.IsActive); // Still inactive
            // No new event should be published if already inactive and no change is made
            _eventStoreMock.Verify(es => es.Publish(It.IsAny<IEvent>()), Times.Never);
        }
    }
}