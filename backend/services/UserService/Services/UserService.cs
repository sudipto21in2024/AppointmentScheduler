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

        public UserService(Shared.Data.ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<User?> GetUserByUsername(string email)
        {
            using var activity = ActivitySource.StartActivity("UserService.GetUserByUsername");
            activity?.SetTag("user.email", email);
            
            LoggingExtensions.AddTraceIdToLogContext();
            
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
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
                // Handle null user case
                if (user == null)
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    return false;
                }
                
                // Handle null/new empty password case
                if (string.IsNullOrEmpty(newPassword))
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    return false;
                }
                    
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
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
                var user = await _context.Users.FindAsync(id);
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
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
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
                if (user == null)
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    return null;
                }
                    
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
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
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    activity?.SetStatus(ActivityStatusCode.Error);
                    return false;
                }
                    
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
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