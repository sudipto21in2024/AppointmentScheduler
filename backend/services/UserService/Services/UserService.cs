using Shared.Models;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using System.Threading.Tasks;
using BCrypt.Net;
using System;
using Shared.Data;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using UserService.Utils;
using Shared.Events; // Added for event types

namespace UserService.Services
{
    public interface IUserService
    {
        Task<User?> GetUserByUsername(string email);
        Task<bool> UpdatePassword(User user, string newPassword);
        Task<User?> GetUserById(Guid id);
        Task<User> CreateUser(User user);
        Task<User?> UpdateUser(User user);
        Task<bool> DeleteUser(Guid id);
    }

    public class UserService : IUserService
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource("UserService.UserService");
        private readonly Shared.Data.ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IEventStore _eventStore;

        public UserService(Shared.Data.ApplicationDbContext context, ILogger<UserService> logger, IEventStore eventStore)
        {
            _context = context;
            _logger = logger;
            _eventStore = eventStore;
        }
        
        public async Task<User?> GetUserByUsername(string email)
        {
            using var activity = ActivitySource.StartActivity("UserService.GetUserByUsername");
            activity?.SetTag("user.email", email);
            
            LoggingExtensions.AddTraceIdToLogContext();
            
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by email {Email}", email);
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
        }

        public async Task<bool> UpdatePassword(User user, string newPassword)
        {
            using var activity = ActivitySource.StartActivity("UserService.UpdatePassword");
            activity?.SetTag("user.id", user?.Id.ToString());
            
            LoggingExtensions.AddTraceIdToLogContext();
            
            try
            {
                if (user == null || !user.IsActive)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, "User is null or inactive.");
                    _logger.LogWarning("Attempted to update password for null or inactive user.");
                    return false;
                }
                
                if (string.IsNullOrEmpty(newPassword))
                {
                    activity?.SetStatus(ActivityStatusCode.Error, "New password cannot be null or empty.");
                    _logger.LogWarning("Attempted to update password with null or empty newPassword for user {UserId}.", user.Id);
                    return false;
                }

                string salt = BCrypt.Net.BCrypt.GenerateSalt();
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, salt);
                user.PasswordSalt = salt;
                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                
                await _eventStore.Append((Shared.Contracts.IEvent)new PasswordChangedEvent
                {
                    UserId = user.Id,
                    TenantId = user.TenantId,
                    ChangedAt = user.UpdatedAt
                });

                activity?.SetStatus(ActivityStatusCode.Ok);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating password for user {UserId}", user?.Id);
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
        }

        public async Task<User?> GetUserById(Guid id)
        {
            using var activity = ActivitySource.StartActivity("UserService.GetUserById");
            activity?.SetTag("user.id", id.ToString());
            
            LoggingExtensions.AddTraceIdToLogContext();
            
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID {UserId}", id);
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
        }

        public async Task<User> CreateUser(User user)
        {
            using var activity = ActivitySource.StartActivity("UserService.CreateUser");
            activity?.SetTag("user.email", user.Email);
            activity?.SetTag("user.id", user.Id.ToString());
            
            LoggingExtensions.AddTraceIdToLogContext();
            
            try
            {
                user.Id = Guid.NewGuid();
                string salt = BCrypt.Net.BCrypt.GenerateSalt();
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash, salt); // Assuming PasswordHash initially holds the plain password
                user.PasswordSalt = salt;
                user.IsActive = true;
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await _eventStore.Append((Shared.Contracts.IEvent)new UserRegisteredEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserType = user.UserType.ToString(),
                    TenantId = user.TenantId,
                    RegisteredAt = user.CreatedAt
                });

                activity?.SetStatus(ActivityStatusCode.Ok);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Email}", user.Email);
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
        }

        public async Task<User?> UpdateUser(User user)
        {
            using var activity = ActivitySource.StartActivity("UserService.UpdateUser");
            activity?.SetTag("user.id", user?.Id.ToString());
            
            LoggingExtensions.AddTraceIdToLogContext();
            
            try
            {
                if (user == null || !user.IsActive)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, "User is null or inactive.");
                    _logger.LogWarning("Attempted to update null or inactive user {UserId}.", user?.Id);
                    return null;
                }

                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                await _eventStore.Append((Shared.Contracts.IEvent)new UserUpdatedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    TenantId = user.TenantId,
                    UpdatedAt = user.UpdatedAt
                });

                activity?.SetStatus(ActivityStatusCode.Ok);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", user?.Id);
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
        }

        public async Task<bool> DeleteUser(Guid id)
        {
            using var activity = ActivitySource.StartActivity("UserService.DeleteUser");
            activity?.SetTag("user.id", id.ToString());
            
            LoggingExtensions.AddTraceIdToLogContext();
            
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
                if (user == null)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, "User not found or already inactive.");
                    _logger.LogWarning("Attempted to delete (soft) non-existent or inactive user {UserId}.", id);
                    return false;
                }

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                await _eventStore.Append((Shared.Contracts.IEvent)new UserDeletedEvent
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserType = user.UserType.ToString(),
                    TenantId = user.TenantId,
                    DeletedAt = user.UpdatedAt
                });

                activity?.SetStatus(ActivityStatusCode.Ok);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
        }
    }
}