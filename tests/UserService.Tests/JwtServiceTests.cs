using Xunit;
using UserService.Processors;

namespace UserService.Tests
{
    public class JwtServiceTests
    {
        private readonly JwtService _jwtService;

        public JwtServiceTests()
        {
            _jwtService = new JwtService("MySuperSecretKey");
        }

        [Fact]
        public void GenerateToken_ShouldReturnNonEmptyString()
        {
            var token = _jwtService.GenerateToken("testuser");
            Assert.NotEmpty(token);
        }

        [Fact]
        public void ValidateToken_ShouldReturnTrueForValidToken()
        {
            var token = _jwtService.GenerateToken("testuser");
            var isValid = _jwtService.ValidateToken(token);
            Assert.True(isValid);
        }
    }
}