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
        private readonly UserService.Services.UserService _userService;

        public UserServiceTests()
        {
            // Create a simple in-memory database context for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            var dbContext = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<UserService.Services.UserService>>();
            _userService = new UserService.Services.UserService(dbContext, _loggerMock.Object);
        }

        [Fact]
        public async Task UpdatePassword_ValidUserAndPassword_ReturnsTrue()
        {
            // Arrange
            var user = new User {
                Email = "test@test.com",
                PasswordHash = "oldhash",
                FirstName = "Test",
                LastName = "User"
            };
            string newPassword = "newpassword";

            // Act
            var result = await _userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdatePassword_NullUser_ReturnsFalse()
        {
            // Arrange
            User user = null!;
            string newPassword = "newpassword";

            // Act
            var result = await _userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdatePassword_NullNewPassword_ReturnsFalse()
        {
            // Arrange
            var user = new User {
                Email = "test@test.com",
                PasswordHash = "oldhash",
                FirstName = "Test",
                LastName = "User"
            };
            string newPassword = null!;

            // Act
            var result = await _userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdatePassword_EmptyNewPassword_ReturnsFalse()
        {
            // Arrange
            var user = new User {
                Email = "test@test.com",
                PasswordHash = "oldhash",
                FirstName = "Test",
                LastName = "User"
            };
            string newPassword = "";

            // Act
            var result = await _userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.False(result);
        }
    }
}