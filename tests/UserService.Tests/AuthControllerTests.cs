using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Contracts;
using Shared.Models;
using UserService.Controllers;
using UserService.DTO;
using Xunit;
using System.Collections.Generic;
using System.Reflection;

namespace UserService.Tests
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthenticationService> _authenticationServiceMock;
        private readonly Mock<UserService.Services.IUserService> _userServiceMock;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly AuthController _authController;

        public AuthControllerTests()
        {
            _authenticationServiceMock = new Mock<IAuthenticationService>();
            _userServiceMock = new Mock<UserService.Services.IUserService>();
            _loggerMock = new Mock<ILogger<AuthController>>();
            _authController = new AuthController(_authenticationServiceMock.Object, _userServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkResult()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "test@test.com",
                Password = "password"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "User"
            };

            _authenticationServiceMock.Setup(x => x.Authenticate(loginRequest.Username, loginRequest.Password))
                .ReturnsAsync((user, (string)null));

            _authenticationServiceMock.Setup(x => x.GenerateToken(user))
                .ReturnsAsync("test_token");

            // Act
            var result = await _authController.Login(loginRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var loginResponse = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.Equal("test_token", loginResponse.AccessToken);
            Assert.Equal(user.Id, loginResponse.User.Id);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "test@test.com",
                Password = "wrongpassword"
            };

            _authenticationServiceMock.Setup(x => x.Authenticate(loginRequest.Username, loginRequest.Password))
                .ReturnsAsync(((User)null, "Invalid username or password"));

            // Act
            var result = await _authController.Login(loginRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid username or password", badRequestResult.Value);
        }

        [Fact]
        public async Task Register_ValidUser_ReturnsCreatedResult()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Email = "newuser@test.com",
                Password = "password",
                FirstName = "New",
                LastName = "User",
                UserType = UserRole.Customer
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = registerRequest.Email,
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                UserType = registerRequest.UserType
            };

            _userServiceMock.Setup(x => x.GetUserByUsername(registerRequest.Email))
                .ReturnsAsync((User)null);

            _userServiceMock.Setup(x => x.CreateUser(It.IsAny<User>()))
                .ReturnsAsync(user);

            // Act
            var result = await _authController.Register(registerRequest);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            var userResponse = Assert.IsType<UserResponse>(createdResult.Value);
            Assert.Equal(user.Id, userResponse.User.Id);
            Assert.Equal(registerRequest.Email, userResponse.User.Email);
        }

        [Fact]
        public async Task Register_UserAlreadyExists_ReturnsBadRequest()
        {
            // Arrange
            var registerRequest = new RegisterRequest
            {
                Email = "existing@test.com",
                Password = "password",
                FirstName = "Existing",
                LastName = "User",
                UserType = UserRole.Customer
            };

            var existingUser = new User
            {
                Id = Guid.NewGuid(),
                Email = registerRequest.Email
            };

            _userServiceMock.Setup(x => x.GetUserByUsername(registerRequest.Email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _authController.Register(registerRequest);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User with this email already exists.", badRequestResult.Value);
        }

        [Fact]
        public async Task Logout_ValidToken_ReturnsOkResult()
        {
            // Arrange
            var logoutRequest = new LogoutRequest
            {
                Token = "valid_token"
            };

            _authenticationServiceMock.Setup(x => x.InvalidateRefreshToken(logoutRequest.Token))
                .ReturnsAsync(true);

            // Act
            var result = await _authController.Logout(logoutRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var messageType = response.GetType();
            var messageProperty = messageType.GetProperty("message");
            Assert.NotNull(messageProperty);
            var messageValue = messageProperty.GetValue(response) as string;
            Assert.Equal("Logged out successfully", messageValue);
        }

        [Fact]
        public async Task Refresh_ValidToken_ReturnsOkResult()
        {
            // Arrange
            var refreshRequest = new RefreshRequest
            {
                RefreshToken = "valid_refresh_token"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com"
            };

            _authenticationServiceMock.Setup(x => x.ValidateRefreshToken(refreshRequest.RefreshToken))
                .ReturnsAsync(true);

            _authenticationServiceMock.Setup(x => x.GetUserFromRefreshToken(refreshRequest.RefreshToken))
                .ReturnsAsync(user);

            _authenticationServiceMock.Setup(x => x.GenerateToken(user))
                .ReturnsAsync("new_access_token");

            _authenticationServiceMock.Setup(x => x.GenerateRefreshToken(user))
                .ReturnsAsync("new_refresh_token");

            // Act
            var result = await _authController.Refresh(refreshRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var refreshResponse = Assert.IsType<RefreshResponse>(okResult.Value);
            Assert.Equal("new_access_token", refreshResponse.AccessToken);
            Assert.Equal("new_refresh_token", refreshResponse.RefreshToken);
        }

        [Fact]
        public async Task GetUser_ValidId_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Email = "test@test.com",
                FirstName = "Test",
                LastName = "User"
            };

            _userServiceMock.Setup(x => x.GetUserById(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _authController.GetUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var userResponse = Assert.IsType<UserResponse>(okResult.Value);
            Assert.Equal(userId, userResponse.User.Id);
            Assert.Equal(user.Email, userResponse.User.Email);
        }

        [Fact]
        public async Task GetUser_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();

            _userServiceMock.Setup(x => x.GetUserById(userId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _authController.GetUser(userId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}