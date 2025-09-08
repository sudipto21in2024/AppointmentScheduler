using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using Shared.Models;
using Shared.Events;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using BCrypt.Net;
using System;
using UserService.DTO;
using MassTransit;


namespace UserService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly Shared.Contracts.IAuthenticationService _authenticationService;
        private readonly UserService.Services.IUserService _userService;
        private readonly ILogger<AuthController> _logger;
        private readonly IPublishEndpoint _publishEndpoint;

        public AuthController(Shared.Contracts.IAuthenticationService authenticationService, UserService.Services.IUserService userService, ILogger<AuthController> logger, IPublishEndpoint publishEndpoint)
        {
            _authenticationService = authenticationService;
            _userService = userService;
            _logger = logger;
            _publishEndpoint = publishEndpoint;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            (Shared.Models.User? user, string? message) = await _authenticationService.Authenticate(request.Username, request.Password);
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
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest("Email, password, first name, and last name are required.");
            }

            // Check if user already exists
            var existingUser = await _userService.GetUserByUsername(request.Email);
            if (existingUser != null)
            {
                return BadRequest("User with this email already exists.");
            }

            _logger.LogInformation($"Registration requested for user {request.Email} with role {request.UserType}");
            
            // Create new user
            var user = new Shared.Models.User
            {
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                UserType = request.UserType,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TenantId = request.TenantId ?? Guid.NewGuid() // Generate a new tenant ID if not provided
            };
            
            // Save user to database
            var createdUser = await _userService.CreateUser(user);
            
            // Publish UserRegisteredEvent
            var userRegisteredEvent = new UserRegisteredEvent
            {
                UserId = createdUser.Id,
                Email = createdUser.Email,
                FirstName = createdUser.FirstName,
                LastName = createdUser.LastName,
                UserType = createdUser.UserType.ToString(),
                TenantId = createdUser.TenantId,
                RegisteredAt = DateTime.UtcNow
            };
            
            await _publishEndpoint.Publish(userRegisteredEvent);
            
            _logger.LogInformation($"User {createdUser.Email} registered successfully and UserRegisteredEvent published.");
            
            return CreatedAtAction(nameof(GetUser), new { id = createdUser.Id }, new UserResponse { User = createdUser });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                return BadRequest("Token is required.");
            }
            
            var result = await _authenticationService.InvalidateRefreshToken(request.Token);
            if (result)
            {
                _logger.LogInformation("User logged out successfully");
                return Ok(new { message = "Logged out successfully" });
            }
            
            _logger.LogWarning("Failed to logout user");
            return BadRequest("Failed to logout");
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return BadRequest("Refresh token is required.");
            }
            
            // Validate the refresh token
            var isValid = await _authenticationService.ValidateRefreshToken(request.RefreshToken);
            if (!isValid)
            {
                return BadRequest("Invalid refresh token.");
            }
            
            // Get user from refresh token
            var user = await _authenticationService.GetUserFromRefreshToken(request.RefreshToken);
            if (user == null)
            {
                return BadRequest("Unable to retrieve user from refresh token.");
            }
            
            // Generate new access token
            var newAccessToken = await _authenticationService.GenerateToken(user);
            
            // Generate new refresh token
            var newRefreshToken = await _authenticationService.GenerateRefreshToken(user);
            
            // Invalidate the old refresh token
            await _authenticationService.InvalidateRefreshToken(request.RefreshToken);
            
            _logger.LogInformation($"Token refreshed for user {user.Email}");
            return Ok(new RefreshResponse { AccessToken = newAccessToken, RefreshToken = newRefreshToken });
        }

        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest("Email is required.");
            }
            
            // Check if user exists
            var user = await _userService.GetUserByUsername(request.Email);
            if (user == null)
            {
                // We don't want to reveal if the email exists or not for security reasons
                return Ok(new { message = "Password reset instructions sent if email exists" });
            }
            
            // In a real implementation, this would send a password reset email with a token
            // For now, we'll just log that a reset was requested
            _logger.LogInformation($"Password reset requested for email: {request.Email}");
            return Ok(new { message = "Password reset instructions sent if email exists" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest("Email, token, and new password are required.");
            }
            
            // Check if user exists
            var user = await _userService.GetUserByUsername(request.Email);
            if (user == null)
            {
                return BadRequest("Invalid request.");
            }
            
            // In a real implementation, this would validate the reset token
            // For now, we'll just update the password directly
            var result = await _authenticationService.ChangePassword(user, request.NewPassword);
            if (result)
            {
                _logger.LogInformation($"Password reset successfully for email: {request.Email}");
                return Ok(new { message = "Password reset successfully" });
            }
            
            _logger.LogWarning($"Failed to reset password for email: {request.Email}");
            return BadRequest("Failed to reset password");
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound($"User with ID {id} not found.");
            }
            
            return Ok(new UserResponse { User = user });
        }
    }
}