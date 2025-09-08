using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shared.Models;
using System.Diagnostics;
using UserService.Utils;
using Microsoft.Extensions.Logging;

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
        private static readonly ActivitySource ActivitySource = new ActivitySource("UserService.TokenService");
        private readonly List<RefreshToken> _refreshTokens = new List<RefreshToken>();

        public string GenerateRefreshToken(User user)
        {
            using var activity = ActivitySource.StartActivity("TokenService.GenerateRefreshToken");
            activity?.SetTag("user.id", user?.Id.ToString());
            
            LoggingExtensions.AddTraceIdToLogContext();
            
            try
            {
                var refreshToken = new RefreshToken
                {
                    Token = Guid.NewGuid().ToString(),
                    UserId = user.Id,
                    ExpiryTime = DateTime.UtcNow.AddDays(7)
                };

                _refreshTokens.Add(refreshToken);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return refreshToken.Token;
            }
            catch (Exception ex)
            {
                // Note: In a real implementation, we would inject ILogger<TokenService>
                // For now, we'll just rethrow the exception
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
        }

        public bool ValidateRefreshToken(string refreshToken)
        {
            using var activity = ActivitySource.StartActivity("TokenService.ValidateRefreshToken");
            activity?.SetTag("token.present", !string.IsNullOrWhiteSpace(refreshToken));
            
            LoggingExtensions.AddTraceIdToLogContext();
            
            try
            {
                var token = _refreshTokens.FirstOrDefault(x => x.Token == refreshToken && x.ExpiryTime > DateTime.UtcNow);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return token != null;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
        }

        public async Task<User?> GetUserFromRefreshToken(string refreshToken)
        {
            using var activity = ActivitySource.StartActivity("TokenService.GetUserFromRefreshToken");
            activity?.SetTag("token.present", !string.IsNullOrWhiteSpace(refreshToken));
            
            LoggingExtensions.AddTraceIdToLogContext();
            
            try
            {
                var token = _refreshTokens.FirstOrDefault(x => x.Token == refreshToken && x.ExpiryTime > DateTime.UtcNow);
                if (token != null)
                {
                    //TODO: Replace with actual user retrieval logic from database
                    var user = new User { Id = token.UserId, Email = "test@example.com" };
                    activity?.SetTag("user.id", user.Id.ToString());
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return user;
                }
                activity?.SetStatus(ActivityStatusCode.Error);
                return null;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
        }

        public async Task<bool> InvalidateRefreshToken(string refreshToken)
        {
            using var activity = ActivitySource.StartActivity("TokenService.InvalidateRefreshToken");
            activity?.SetTag("token.present", !string.IsNullOrWhiteSpace(refreshToken));
            
            LoggingExtensions.AddTraceIdToLogContext();
            
            try
            {
                var token = _refreshTokens.FirstOrDefault(x => x.Token == refreshToken);
                if (token != null)
                {
                    _refreshTokens.Remove(token);
                    activity?.SetStatus(ActivityStatusCode.Ok);
                    return true;
                }
                activity?.SetStatus(ActivityStatusCode.Error);
                return false;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
        }

        private class RefreshToken
        {
            public string Token { get; set; } = null!;
            public Guid UserId { get; set; }
            public DateTime ExpiryTime { get; set; }
        }
    }
}