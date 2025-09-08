using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shared.Models;

namespace UserService.Processors
{
    public interface ITokenService
    {
        string GenerateRefreshToken(User user);
        Task<User?> GetUserFromRefreshToken(string refreshToken);
        Task<bool> InvalidateRefreshToken(string refreshToken);
        bool ValidateRefreshToken(string refreshToken);
    }

    public class TokenService : ITokenService
    {
        private readonly List<RefreshToken> _refreshTokens = new List<RefreshToken>();

        public string GenerateRefreshToken(User user)
        {
            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                UserId = user.Id,
                ExpiryTime = DateTime.UtcNow.AddDays(7)
            };

            _refreshTokens.Add(refreshToken);
            return refreshToken.Token;
        }

        public bool ValidateRefreshToken(string refreshToken)
        {
            var token = _refreshTokens.FirstOrDefault(x => x.Token == refreshToken && x.ExpiryTime > DateTime.UtcNow);
            return token != null;
        }

        public async Task<User?> GetUserFromRefreshToken(string refreshToken)
        {
            var token = _refreshTokens.FirstOrDefault(x => x.Token == refreshToken && x.ExpiryTime > DateTime.UtcNow);
            if (token != null)
            {
                //TODO: Replace with actual user retrieval logic from database
                return new User { Id = token.UserId,  Email = "test@example.com" };
            }
            return null;
        }

        public async Task<bool> InvalidateRefreshToken(string refreshToken)
        {
            var token = _refreshTokens.FirstOrDefault(x => x.Token == refreshToken);
            if (token != null)
            {
                _refreshTokens.Remove(token);
                return true;
            }
            return false;
        }

        private class RefreshToken
        {
            public string Token { get; set; } = null!;
            public Guid UserId { get; set; }
            public DateTime ExpiryTime { get; set; }
        }
    }
}