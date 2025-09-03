using Shared.Contracts;

namespace Shared
{
    public class AuthenticationService : IAuthenticationService
    {
        public string GenerateJwtToken(string userId)
        {
            // Dummy implementation - replace with actual JWT generation
            return $"dummy_jwt_token_for_user_{userId}";
        }

        public bool ValidateJwtToken(string token)
        {
            // Dummy implementation - replace with actual JWT validation
            return token.StartsWith("dummy_jwt_token");
        }
    }
}