using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using Shared.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;


namespace UserService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly Shared.Contracts.IAuthenticationService _authenticationService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(Shared.Contracts.IAuthenticationService authenticationService, ILogger<AuthController> logger)
        {
            _authenticationService = authenticationService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            (User? user, string? message) = await _authenticationService.Authenticate(request.Username, request.Password);
            if (user is null)
            {
                _logger.LogWarning($"Login failed for user {request.Username}: {message}");
                return BadRequest(message);
            }
            _logger.LogInformation($"User {request.Username} logged in successfully.");
            var token = await _authenticationService.GenerateToken(user);
            return Ok(new LoginResponse { AccessToken = token, User = user });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // For now we'll simulate user creation with basic validation
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest("Email, password, first name, and last name are required.");
            }

            _logger.LogInformation($"Registration requested for user {request.Email} with role {request.UserType}");
            // In a real implementation, this would create a new user in the database
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                UserType = request.UserType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TenantId = request.TenantId ?? Guid.Empty // Default to empty guid for demo purposes
            };
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserResponse { User = user });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            // In a real implementation, this would invalidate the token
            _logger.LogInformation("Logout requested");
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            // In a real implementation, this would validate and refresh the token
            _logger.LogInformation("Token refresh requested");
            return Ok(new RefreshResponse { AccessToken = "new_access_token", RefreshToken = "new_refresh_token" });
        }

        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            // In a real implementation, this would send a password reset email
            _logger.LogInformation($"Password reset requested for email: {request.Email}");
            return Ok(new { message = "Password reset instructions sent if email exists" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            // In a real implementation, this would validate the reset token and update the password
            _logger.LogInformation($"Password reset requested for email: {request.Email}");
            return Ok(new { message = "Password reset successfully" });
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            // In a real implementation, this would fetch a user from the database
            var user = new User
            {
                Id = id,
                Email = "test@example.com",
                PasswordHash = "hashed_password",
                FirstName = "Test",
                LastName = "User",
                PhoneNumber = "123-456-7890",
                UserType = UserRole.Customer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TenantId = Guid.NewGuid()
            };
            return Ok(new UserResponse { User = user });
        }
    }

    // Request DTOs for the API endpoints
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }

    public class LoginResponse
    {
        public string AccessToken { get; set; } = null!;
        public User User { get; set; } = null!;
    }

    public class RegisterRequest
    {
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
        [Required]
        public string FirstName { get; set; } = null!;
        [Required]
        public string LastName { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        [Required]
        public UserRole UserType { get; set; }
        public Guid? TenantId { get; set; }
    }

    public class LogoutRequest
    {
        public string Token { get; set; } = null!;
    }

    public class RefreshRequest
    {
        public string RefreshToken { get; set; } = null!;
    }

    public class RefreshResponse
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }

    public class PasswordResetRequest
    {
        [Required]
        public string Email { get; set; } = null!;
    }

    public class ResetPasswordRequest
    {
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string Token { get; set; } = null!;
        [Required]
        public string NewPassword { get; set; } = null!;
    }

    public class UserResponse
    {
        public User User { get; set; } = null!;
    }
}