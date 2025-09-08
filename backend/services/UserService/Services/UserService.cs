using Shared.Models;
using Microsoft.Extensions.Logging;
using Shared.Contracts;
using System.Threading.Tasks;
using BCrypt.Net;
using System;


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
        //private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ILogger<UserService> logger)
        {
            //_context = context;
            _logger = logger;
        }
        public async Task<User?> GetUserByUsername(string email)
        {
            //return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            //TODO: Implement this method
            var user = new User {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                FirstName = "Test",
                LastName = "User",
                UserType = UserRole.Customer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            return user;
        }

        public async Task<bool> UpdatePassword(User user, string newPassword)
        {
            //user.Password = newPassword;
            //_context.Users.Update(user);
            //await _context.SaveChangesAsync();
            //TODO: Implement this method
            return true;
        }

        public async Task<User?> GetUserById(Guid id)
        {
            // Implementation would fetch user from DB by ID
            // For now returning a dummy user
            return new User
            {
                Id = id,
                Email = "test@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
                FirstName = "Test",
                LastName = "User",
                UserType = UserRole.Customer,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public async Task<User> CreateUser(User user)
        {
            // Implementation would save user to DB
            // For now just setting IDs and timestamps
            user.Id = Guid.NewGuid();
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            return user;
        }

        public async Task<User?> UpdateUser(User user)
        {
            // Implementation would update user in DB
            // For now just updating timestamp
            user.UpdatedAt = DateTime.UtcNow;
            return user;
        }

        public async Task<bool> DeleteUser(Guid id)
        {
            // Implementation would soft-delete user from DB
            // For now just returning true
            return true;
        }
    }


}