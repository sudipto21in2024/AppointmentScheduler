using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace UserService.Processors
{
    public class JwtService
    {
        private readonly string _secretKey;

        public JwtService(string secretKey)
        {
            _secretKey = secretKey;
        }

        public string GenerateToken(string userId)
        {
            // Dummy implementation
            return "dummy_token";
        }

        public bool ValidateToken(string token)
        {
            // Dummy implementation
            return true;
        }
    }
}