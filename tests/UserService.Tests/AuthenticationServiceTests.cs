using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using Shared.Models;
using Shared.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using AuthenticationService.Services;
using System;
using BCrypt.Net;
using System.Linq;

namespace UserService.Tests
{
    public class AuthenticationServiceTests
    {
        private readonly Mock<ILogger<AuthenticationService.Services.AuthenticationService>> _loggerMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private ApplicationDbContext _dbContext;
        private AuthenticationService.Services.AuthenticationService _authenticationService;

        public AuthenticationServiceTests()
        {
            _loggerMock = new Mock<ILogger<AuthenticationService.Services.AuthenticationService>>();
            _userServiceMock = new Mock<IUserService>();
            _configurationMock = new Mock<IConfiguration>();

            // Setup configuration mocks for JWT and Refresh Token settings
            _configurationMock.Setup(c => c["Jwt:Secret"]).Returns("thisisasecretkeythatissupposedtobelongerthan32characters");
            _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("test-issuer");
            _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("test-audience");
            _configurationMock.Setup(c => c["Jwt:AccessTokenValidityInMinutes"]).Returns("15");
            _configurationMock.Setup(c => c["Jwt:RefreshTokenValidityInDays"]).Returns("7");

            InitializeDbContext();
            _authenticationService = new AuthenticationService.Services.AuthenticationService(
                _loggerMock.Object,
                _userServiceMock.Object,
                _configurationMock.Object,
                _dbContext
            );
        }

        private void InitializeDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);
            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();
        }

        [Fact]
        public async Task Authenticate_ValidCredentials_ReturnsUserAndNullMessage()
        {
            // Arrange
            InitializeDbContext();
            var email = "test@example.com";
            var password = "Password123!";
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                PasswordSalt = BCrypt.Net.BCrypt.GenerateSalt(), // Not strictly needed for BCrypt.Verify but good practice
                IsActive = true,
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _userServiceMock.Setup(s => s.GetUserByUsername(email)).ReturnsAsync(user);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var (authenticatedUser, message) = await _authenticationService.Authenticate(email, password);

            // Assert
            Assert.NotNull(authenticatedUser);
            Assert.Null(message);
            Assert.Equal(user.Id, authenticatedUser.Id);
            Assert.NotNull(authenticatedUser.LastLoginAt); // Should be updated
        }

        [Fact]
        public async Task Authenticate_InvalidPassword_ReturnsNullAndInvalidCredentialsMessage()
        {
            // Arrange
            InitializeDbContext();
            var email = "test@example.com";
            var correctPassword = "Password123!";
            var wrongPassword = "WrongPassword!";
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(correctPassword),
                PasswordSalt = BCrypt.Net.BCrypt.GenerateSalt(),
                IsActive = true,
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _userServiceMock.Setup(s => s.GetUserByUsername(email)).ReturnsAsync(user);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var (authenticatedUser, message) = await _authenticationService.Authenticate(email, wrongPassword);

            // Assert
            Assert.Null(authenticatedUser);
            Assert.Equal("Invalid credentials", message);
        }

        [Fact]
        public async Task Authenticate_UserNotFound_ReturnsNullAndInvalidCredentialsMessage()
        {
            // Arrange
            InitializeDbContext();
            var email = "nonexistent@example.com";
            var password = "Password123!";
            _userServiceMock.Setup(s => s.GetUserByUsername(email)).ReturnsAsync((User?)null);

            // Act
            var (authenticatedUser, message) = await _authenticationService.Authenticate(email, password);

            // Assert
            Assert.Null(authenticatedUser);
            Assert.Equal("Invalid credentials", message);
        }

        [Fact]
        public async Task Authenticate_InactiveUser_ReturnsNullAndInvalidCredentialsMessage()
        {
            // Arrange
            InitializeDbContext();
            var email = "inactive@example.com";
            var password = "Password123!";
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                PasswordSalt = BCrypt.Net.BCrypt.GenerateSalt(),
                IsActive = false, // Inactive user
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _userServiceMock.Setup(s => s.GetUserByUsername(email)).ReturnsAsync(user);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            // Act
            var (authenticatedUser, message) = await _authenticationService.Authenticate(email, password);

            // Assert
            Assert.Null(authenticatedUser);
            Assert.Equal("Invalid credentials", message);
        }

        [Fact]
        public void GenerateToken_ValidUser_ReturnsValidJwtToken()
        {
            // Arrange
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "token@example.com",
                UserType = UserRole.Customer,
                TenantId = Guid.NewGuid()
            };

            // Act
            var token = _authenticationService.GenerateToken(user);

            // Assert
            Assert.False(string.IsNullOrEmpty(token));
            // Further validation could involve decoding and checking claims, expiration, etc.
        }

        [Fact]
        public async Task GenerateRefreshToken_ValidUserAndIp_ReturnsRefreshTokenAndStoresInDb()
        {
            // Arrange
            InitializeDbContext();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "refresh@example.com",
                IsActive = true,
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            var ipAddress = "127.0.0.1";

            // Act
            var refreshToken = await _authenticationService.GenerateRefreshToken(user, ipAddress);

            // Assert
            Assert.NotNull(refreshToken);
            Assert.False(string.IsNullOrEmpty(refreshToken.Token));
            Assert.Equal(user.Id, refreshToken.UserId);
            Assert.Equal(ipAddress, refreshToken.CreatedByIp);
            Assert.NotEqual(DateTime.MinValue, refreshToken.Created);
            Assert.True(refreshToken.Expires > DateTime.UtcNow);

            var storedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken.Token);
            Assert.NotNull(storedToken);
            Assert.Equal(refreshToken.Token, storedToken.Token);
        }

        [Fact]
        public async Task ValidateRefreshToken_ValidActiveToken_ReturnsTrue()
        {
            // Arrange
            InitializeDbContext();
            var user = new User { Id = Guid.NewGuid(), IsActive = true, TenantId = Guid.NewGuid(), Email = "validrt@example.com" };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "validactivetoken",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1",
                UserId = user.Id
            };
            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            // Act
            var isValid = await _authenticationService.ValidateRefreshToken(refreshToken.Token);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public async Task ValidateRefreshToken_ExpiredToken_ReturnsFalse()
        {
            // Arrange
            InitializeDbContext();
            var user = new User { Id = Guid.NewGuid(), IsActive = true, TenantId = Guid.NewGuid(), Email = "expiredrt@example.com" };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "expiredtoken",
                Expires = DateTime.UtcNow.AddDays(-1), // Expired
                Created = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1",
                UserId = user.Id
            };
            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            // Act
            var isValid = await _authenticationService.ValidateRefreshToken(refreshToken.Token);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task ValidateRefreshToken_RevokedToken_ReturnsFalse()
        {
            // Arrange
            InitializeDbContext();
            var user = new User { Id = Guid.NewGuid(), IsActive = true, TenantId = Guid.NewGuid(), Email = "revokedrt@example.com" };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "revokedtoken",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1",
                Revoked = DateTime.UtcNow, // Revoked
                UserId = user.Id
            };
            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            // Act
            var isValid = await _authenticationService.ValidateRefreshToken(refreshToken.Token);

            // Assert
            Assert.False(isValid);
        }

        [Fact]
        public async Task GetUserFromRefreshToken_ValidActiveToken_ReturnsUser()
        {
            // Arrange
            InitializeDbContext();
            var user = new User { Id = Guid.NewGuid(), IsActive = true, TenantId = Guid.NewGuid(), Email = "getuserfromrt@example.com" };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "validactiveusertoken",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1",
                UserId = user.Id
            };
            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            // Act
            var foundUser = await _authenticationService.GetUserFromRefreshToken(refreshToken.Token);

            // Assert
            Assert.NotNull(foundUser);
            Assert.Equal(user.Id, foundUser.Id);
        }

        [Fact]
        public async Task GetUserFromRefreshToken_InvalidOrInactiveToken_ReturnsNull()
        {
            // Arrange
            InitializeDbContext();
            var user = new User { Id = Guid.NewGuid(), IsActive = true, TenantId = Guid.NewGuid(), Email = "getuserfromrtinvalid@example.com" };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var expiredToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "expiredusertoken",
                Expires = DateTime.UtcNow.AddDays(-1),
                Created = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1",
                UserId = user.Id
            };
            _dbContext.RefreshTokens.Add(expiredToken);
            await _dbContext.SaveChangesAsync();

            // Act
            var foundUserExpired = await _authenticationService.GetUserFromRefreshToken(expiredToken.Token);
            var foundUserNonExistent = await _authenticationService.GetUserFromRefreshToken("nonexistenttoken");

            // Assert
            Assert.Null(foundUserExpired);
            Assert.Null(foundUserNonExistent);
        }

        [Fact]
        public async Task InvalidateRefreshToken_ValidActiveToken_RevokesToken()
        {
            // Arrange
            InitializeDbContext();
            var user = new User { Id = Guid.NewGuid(), IsActive = true, TenantId = Guid.NewGuid(), Email = "invalidatert@example.com" };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "tokenToInvalidate",
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1",
                UserId = user.Id
            };
            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();
            var ipAddress = "127.0.0.1";

            // Act
            var result = await _authenticationService.InvalidateRefreshToken(refreshToken.Token, ipAddress);

            // Assert
            Assert.True(result);
            var revokedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken.Token);
            Assert.NotNull(revokedToken);
            Assert.NotNull(revokedToken.Revoked);
            Assert.Equal(ipAddress, revokedToken.RevokedByIp);
            Assert.False(revokedToken.IsActive);
        }

        [Fact]
        public async Task InvalidateRefreshToken_InvalidOrInactiveToken_ReturnsFalse()
        {
            // Arrange
            InitializeDbContext();
            var user = new User { Id = Guid.NewGuid(), IsActive = true, TenantId = Guid.NewGuid(), Email = "invalidatertfail@example.com" };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var expiredToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = "expiredtokenToInvalidate",
                Expires = DateTime.UtcNow.AddDays(-1),
                Created = DateTime.UtcNow,
                CreatedByIp = "127.0.0.1",
                UserId = user.Id
            };
            _dbContext.RefreshTokens.Add(expiredToken);
            await _dbContext.SaveChangesAsync();
            var ipAddress = "127.0.0.1";

            // Act
            var resultExpired = await _authenticationService.InvalidateRefreshToken(expiredToken.Token, ipAddress);
            var resultNonExistent = await _authenticationService.InvalidateRefreshToken("nonexistenttoken", ipAddress);

            // Assert
            Assert.False(resultExpired);
            Assert.False(resultNonExistent);
        }

        [Fact]
        public async Task ChangePassword_ValidUser_ChangesPasswordAndInvalidatesRefreshTokens()
        {
            // Arrange
            InitializeDbContext();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "changepw@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword"),
                PasswordSalt = BCrypt.Net.BCrypt.GenerateSalt(),
                IsActive = true,
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var refreshToken1 = await _authenticationService.GenerateRefreshToken(user, "192.168.1.1");
            var refreshToken2 = await _authenticationService.GenerateRefreshToken(user, "192.168.1.2");

            _userServiceMock.Setup(s => s.UpdatePassword(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(true);

            var newPassword = "NewPassword123!";

            // Act
            var result = await _authenticationService.ChangePassword(user, newPassword);

            // Assert
            Assert.True(result);
            _userServiceMock.Verify(s => s.UpdatePassword(user, newPassword), Times.Once);

            var revokedTokens = await _dbContext.RefreshTokens.Where(rt => rt.UserId == user.Id).ToListAsync();
            Assert.All(revokedTokens, rt => Assert.NotNull(rt.Revoked));
            Assert.All(revokedTokens, rt => Assert.False(rt.IsActive));
        }

        [Fact]
        public async Task ChangePassword_InactiveUser_ReturnsFalseAndDoesNotChangePassword()
        {
            // Arrange
            InitializeDbContext();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "changepw-inactive@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpassword"),
                PasswordSalt = BCrypt.Net.BCrypt.GenerateSalt(),
                IsActive = false, // Inactive
                TenantId = Guid.NewGuid(),
                UserType = UserRole.Customer,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var newPassword = "NewPassword123!";

            // Act
            var result = await _authenticationService.ChangePassword(user, newPassword);

            // Assert
            Assert.False(result);
            _userServiceMock.Verify(s => s.UpdatePassword(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
            var refreshTokens = await _dbContext.RefreshTokens.Where(rt => rt.UserId == user.Id).ToListAsync();
            Assert.Empty(refreshTokens); // No refresh tokens should have been generated for inactive user
        }
    }
}