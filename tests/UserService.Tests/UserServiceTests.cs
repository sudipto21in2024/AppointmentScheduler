using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using UserService.Services;
using Shared.Contracts;
using Shared.Models;

namespace UserService.Tests
{
    public class UserServiceTests
    {
        private readonly Mock<ILogger<UserService.Services.UserService>> _loggerMock;
        private readonly UserService.Services.UserService _userService;

        public UserServiceTests()
        {
            _loggerMock = new Mock<ILogger<UserService.Services.UserService>>();
            _userService = new UserService.Services.UserService(_loggerMock.Object);
        }

        [Fact]
        public async Task GetUserByUsername_ValidEmail_ReturnsUser()
        {
            // Arrange
            string email = "test@test.com";
            var expectedUser = new User { Email = email };
            // Remove the problematic logging setup

            // Act
            var user = await _userService.GetUserByUsername(email);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(email, user.Email);
        }

        [Fact]
        public async Task GetUserByUsername_NullEmail_ReturnsUser()
        {
            // Arrange
            string email = null!;

            // Act
            var user = await _userService.GetUserByUsername(email);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(email, user.Email);
        }

        [Fact]
        public async Task GetUserByUsername_EmptyEmail_ReturnsUser()
        {
            // Arrange
            string email = "";

            // Act
            var user = await _userService.GetUserByUsername(email);

            // Assert
            Assert.NotNull(user);
            Assert.Equal(email, user.Email);
        }

        [Fact]
        public async Task UpdatePassword_ValidUserAndPassword_ReturnsTrue()
        {
            // Arrange
            var user = new User { Email = "test@test.com" };
            string newPassword = "newpassword";

            // Act
            var result = await _userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdatePassword_NullUser_ReturnsTrue()
        {
            // Arrange
            User user = null!;
            string newPassword = "newpassword";

            // Act
            var result = await _userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdatePassword_NullNewPassword_ReturnsTrue()
        {
            // Arrange
            var user = new User { Email = "test@test.com" };
            string newPassword = null!;

            // Act
            var result = await _userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdatePassword_EmptyNewPassword_ReturnsTrue()
        {
            // Arrange
            var user = new User { Email = "test@test.com" };
            string newPassword = "";

            // Act
            var result = await _userService.UpdatePassword(user, newPassword);

            // Assert
            Assert.True(result);
        }
    }
}