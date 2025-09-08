using Shared.Models;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using System.Threading.Tasks;
using BCrypt.Net;
using System;
using UserService.Models;
using Microsoft.EntityFrameworkCore;

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
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task<User?> GetUserByUsername(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<bool> UpdatePassword(User user, string newPassword)
        {
            // Handle null user case
            if (user == null)
                return false;
            
            // Handle null/new empty password case
            if (string.IsNullOrEmpty(newPassword))
                return false;
                
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> GetUserById(Guid id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User> CreateUser(User user)
        {
            user.Id = Guid.NewGuid();
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User?> UpdateUser(User user)
        {
            if (user == null)
                return null;
                
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return false;
                
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}