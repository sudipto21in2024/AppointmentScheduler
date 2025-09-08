namespace Shared.Contracts
{
    public interface IAuthenticationService
    {
        Task<(Shared.Models.User?, string?)> Authenticate(string username, string password);
        Task<string> GenerateToken(Shared.Models.User user);
        Task<string> GenerateRefreshToken(Shared.Models.User user);
        Task<bool> ValidateRefreshToken(string refreshToken);
        Task<Shared.Models.User?> GetUserFromRefreshToken(string refreshToken);
        Task<bool> InvalidateRefreshToken(string refreshToken);
        Task<bool> ValidateJwtToken(string token);
        Task<bool> ChangePassword(Shared.Models.User user, string newPassword);
    }
}