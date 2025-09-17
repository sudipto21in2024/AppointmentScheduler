using UserService.Processors;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Models;
using Microsoft.Extensions.Configuration;
using BCrypt.Net;

namespace UserService.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly Mock<UserService.Services.IUserService> _userServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<AuthenticationService>> _loggerMock;
        private readonly AuthenticationService _authService;

        public AuthServiceTests()
        {
            _jwtServiceMock = new Mock<IJwtService>();
            _tokenServiceMock = new Mock<ITokenService>();
            _userServiceMock = new Mock<UserService.Services.IUserService>();
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<AuthenticationService>>();
            _authService = new AuthenticationService(_jwtServiceMock.Object, _tokenServiceMock.Object, _userServiceMock.Object, _configurationMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Authenticate_ValidCredentials_ReturnsUser()
        {
            // Arrange
            string email = "test@test.com";
            string password = "password";
            var user = new User { Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password) };

            _userServiceMock.Setup(x => x.GetUserByUsername(email)).ReturnsAsync(user);

            // Act
            var (authenticatedUser, message) = await _authService.Authenticate(email, password);

            // Assert
            Assert.NotNull(authenticatedUser);
            Assert.Null(message);
        }

        [Fact]
        public async Task Authenticate_InvalidCredentials_ReturnsNull()
        {
            // Arrange
            string email = "test@test.com";
            string password = "wrongpassword";
            var user = new User { Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword("password") };

            _userServiceMock.Setup(x => x.GetUserByUsername(email)).ReturnsAsync(user);

            // Act
            var (authenticatedUser, message) = await _authService.Authenticate(email, password);

            // Assert
            Assert.Null(authenticatedUser);
            Assert.NotNull(message);
        }

        [Fact]
        public async Task GenerateToken_ValidUser_ReturnsToken()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid() };
            string expectedToken = "testtoken";

            _jwtServiceMock.Setup(x => x.GenerateToken(user.Id.ToString())).Returns(expectedToken);

            // Act
            var token = await _authService.GenerateToken(user);

            // Assert
            Assert.Equal(expectedToken, token);
        }

        [Fact]
        public async Task ValidateJwtToken_ValidToken_ReturnsTrue()
        {
            // Arrange
            string token = "validtoken";
            _jwtServiceMock.Setup(x => x.ValidateToken(token)).Returns(true);

            // Act
            var result = await _authService.ValidateJwtToken(token);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateJwtToken_InvalidToken_ReturnsFalse()
        {
            // Arrange
            string token = "invalidtoken";
            _jwtServiceMock.Setup(x => x.ValidateToken(token)).Returns(false);

            // Act
            var result = await _authService.ValidateJwtToken(token);

            // Assert
            Assert.False(result);
        }
    }
}
