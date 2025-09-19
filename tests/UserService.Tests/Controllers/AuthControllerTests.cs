using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using UserService.Controllers;
using UserService.DTO;
using UserService.Services;
using UserService.Utils;
using Xunit;
using Shared.Contracts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Collections.Generic;

namespace UserService.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthenticationService> _mockAuthenticationService;
        private readonly Mock<UserService.Services.IUserService> _mockUserService; // Fully qualify to avoid ambiguity
        private readonly Mock<ITenantResolutionService> _mockTenantResolutionService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly Mock<IValidator<LoginRequest>> _mockLoginRequestValidator;
        private readonly Mock<IValidator<RegisterRequest>> _mockRegisterRequestValidator;
        private readonly Mock<IValidator<RefreshRequest>> _mockRefreshRequestValidator;
        private readonly Mock<IValidator<PasswordResetRequest>> _mockPasswordResetRequestValidator;
        private readonly Mock<IValidator<ResetPasswordRequest>> _mockResetPasswordRequestValidator;
        private readonly AuthController _authController;

        public AuthControllerTests()
        {
            _mockAuthenticationService = new Mock<IAuthenticationService>();
            _mockUserService = new Mock<UserService.Services.IUserService>();
            _mockTenantResolutionService = new Mock<ITenantResolutionService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _mockLoginRequestValidator = new Mock<IValidator<LoginRequest>>();
            _mockRegisterRequestValidator = new Mock<IValidator<RegisterRequest>>();
            _mockRefreshRequestValidator = new Mock<IValidator<RefreshRequest>>();
            _mockPasswordResetRequestValidator = new Mock<IValidator<PasswordResetRequest>>();
            _mockResetPasswordRequestValidator = new Mock<IValidator<ResetPasswordRequest>>();

            // Setup default validation results to be valid
            _mockLoginRequestValidator.Setup(v => v.ValidateAsync(It.IsAny<LoginRequest>(), default))
                .ReturnsAsync(new ValidationResult());
            _mockRegisterRequestValidator.Setup(v => v.ValidateAsync(It.IsAny<RegisterRequest>(), default))
                .ReturnsAsync(new ValidationResult());
            _mockRefreshRequestValidator.Setup(v => v.ValidateAsync(It.IsAny<RefreshRequest>(), default))
                .ReturnsAsync(new ValidationResult());
            _mockPasswordResetRequestValidator.Setup(v => v.ValidateAsync(It.IsAny<PasswordResetRequest>(), default))
                .ReturnsAsync(new ValidationResult());
            _mockResetPasswordRequestValidator.Setup(v => v.ValidateAsync(It.IsAny<ResetPasswordRequest>(), default))
                .ReturnsAsync(new ValidationResult());

            _authController = new AuthController(
                _mockAuthenticationService.Object,
                _mockUserService.Object,
                _mockTenantResolutionService.Object,
                _mockLogger.Object,
                Mock.Of<MassTransit.IPublishEndpoint>(), // Not used in Login tests
                _mockLoginRequestValidator.Object,
                _mockRegisterRequestValidator.Object,
                _mockRefreshRequestValidator.Object,
                _mockPasswordResetRequestValidator.Object,
                _mockResetPasswordRequestValidator.Object
            );
        }

        [Fact]
        public async Task Login_ShouldReturnOkResult_WithTokenAndUser_WhenSuperAdminCredentialsAreValid()
        {
            // Arrange
            var request = new LoginRequest { Username = "superadmin@admin.com", Password = "password123" };
            
            var tenantResolutionResult = new TenantResolutionResult
            {
                IsSuperAdmin = true,
                IsResolved = true
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Username,
                FirstName = "Super",
                LastName = "Admin",
                UserType = UserRole.SuperAdmin,
                TenantId = Guid.Empty
            };

            var accessToken = "jwt-access-token";
            var refreshToken = "refresh-token";

            _mockTenantResolutionService.Setup(s => s.ResolveTenantAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(tenantResolutionResult);

            _mockAuthenticationService.Setup(s => s.Authenticate(request.Username, request.Password, true, null))
                .ReturnsAsync((user, (string)null)); // No error message

            _mockAuthenticationService.Setup(s => s.GenerateToken(user)).Returns(accessToken);
            _mockAuthenticationService.Setup(s => s.GenerateRefreshToken(user)).Returns(refreshToken);

            // Act
            var result = await _authController.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var loginResponse = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.Equal(accessToken, loginResponse.AccessToken);
            Assert.Same(user, loginResponse.User);
            
            _mockTenantResolutionService.Verify(s => s.ResolveTenantAsync(It.IsAny<HttpContext>()), Times.Once);
            _mockAuthenticationService.Verify(s => s.Authenticate(request.Username, request.Password, true, null), Times.Once);
            _mockAuthenticationService.Verify(s => s.GenerateToken(user), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldReturnOkResult_WithTokenAndUser_WhenTenantUserCredentialsAreValid()
        {
            // Arrange
            var request = new LoginRequest { Username = "user@tenant.com", Password = "password123" };
            var tenantId = Guid.NewGuid();
            
            var tenantResolutionResult = new TenantResolutionResult
            {
                IsSuperAdmin = false,
                TenantId = tenantId,
                IsResolved = true
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Username,
                FirstName = "Tenant",
                LastName = "User",
                UserType = UserRole.Customer,
                TenantId = tenantId
            };

            var accessToken = "jwt-access-token";
            var refreshToken = "refresh-token";

            _mockTenantResolutionService.Setup(s => s.ResolveTenantAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(tenantResolutionResult);

            _mockAuthenticationService.Setup(s => s.Authenticate(request.Username, request.Password, false, tenantId))
                .ReturnsAsync((user, (string)null)); // No error message

            _mockAuthenticationService.Setup(s => s.GenerateToken(user)).Returns(accessToken);
            _mockAuthenticationService.Setup(s => s.GenerateRefreshToken(user)).Returns(refreshToken);

            // Act
            var result = await _authController.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var loginResponse = Assert.IsType<LoginResponse>(okResult.Value);
            Assert.Equal(accessToken, loginResponse.AccessToken);
            Assert.Same(user, loginResponse.User);
            Assert.Equal(tenantId, loginResponse.User.TenantId);
            
            _mockTenantResolutionService.Verify(s => s.ResolveTenantAsync(It.IsAny<HttpContext>()), Times.Once);
            _mockAuthenticationService.Verify(s => s.Authenticate(request.Username, request.Password, false, tenantId), Times.Once);
            _mockAuthenticationService.Verify(s => s.GenerateToken(user), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldReturnBadRequest_WhenTenantCannotBeResolved()
        {
            // Arrange
            var request = new LoginRequest { Username = "user@unknown.com", Password = "password123" };
            
            var tenantResolutionResult = new TenantResolutionResult
            {
                IsResolved = false // Indicates failure to resolve
            };

            _mockTenantResolutionService.Setup(s => s.ResolveTenantAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(tenantResolutionResult);

            // Act
            var result = await _authController.Login(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<string>(badRequestResult.Value); // Should be a string error message
            // Optionally, check the content of the message if needed.
            
            _mockTenantResolutionService.Verify(s => s.ResolveTenantAsync(It.IsAny<HttpContext>()), Times.Once);
            _mockAuthenticationService.Verify(s => s.Authenticate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid?>()), Times.Never);
        }

        [Fact]
        public async Task Login_ShouldReturnBadRequest_WhenAuthenticationFails()
        {
            // Arrange
            var request = new LoginRequest { Username = "user@tenant.com", Password = "wrongpassword" };
            var tenantId = Guid.NewGuid();
            
            var tenantResolutionResult = new TenantResolutionResult
            {
                IsSuperAdmin = false,
                TenantId = tenantId,
                IsResolved = true
            };

            var errorMessage = "Invalid username or password.";

            _mockTenantResolutionService.Setup(s => s.ResolveTenantAsync(It.IsAny<HttpContext>()))
                .ReturnsAsync(tenantResolutionResult);

            _mockAuthenticationService.Setup(s => s.Authenticate(request.Username, request.Password, false, tenantId))
                .ReturnsAsync(((User)null, errorMessage)); // Error message returned

            // Act
            var result = await _authController.Login(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(errorMessage, badRequestResult.Value);
            
            _mockTenantResolutionService.Verify(s => s.ResolveTenantAsync(It.IsAny<HttpContext>()), Times.Once);
            _mockAuthenticationService.Verify(s => s.Authenticate(request.Username, request.Password, false, tenantId), Times.Once);
            _mockAuthenticationService.Verify(s => s.GenerateToken(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Login_ShouldReturnBadRequest_WhenModelStateIsInvalid()
        {
            // Arrange
            var request = new LoginRequest { Username = "", Password = "" }; // Invalid model
            
            var validationFailure = new ValidationFailure("Username", "Username is required.");
            var validationResult = new ValidationResult(new List<ValidationFailure> { validationFailure });
            
            _mockLoginRequestValidator.Setup(v => v.ValidateAsync(It.IsAny<LoginRequest>(), default))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _authController.Login(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            // The value should be a collection of anonymous objects with Field and Message properties.
            // We can assert the type, but checking the exact content might be more complex.
            Assert.NotNull(badRequestResult.Value);
            
            _mockTenantResolutionService.Verify(s => s.ResolveTenantAsync(It.IsAny<HttpContext>()), Times.Never);
            _mockAuthenticationService.Verify(s => s.Authenticate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid?>()), Times.Never);
        }
    }
}