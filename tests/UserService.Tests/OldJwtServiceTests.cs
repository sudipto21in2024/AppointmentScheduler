using Xunit;
using UserService.Processors;
using Microsoft.Extensions.Configuration;
using Moq;

namespace UserService.Tests
{
    public class JwtServiceTests
    {
        private readonly JwtService _jwtService;
        private readonly Mock<IConfiguration> _configurationMock;

        public JwtServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _configurationMock.Setup(c => c["Jwt:SecretKey"]).Returns("MySuperSecretKeyThatIsLongEnoughForHS256Algorithm");
            _configurationMock.Setup(c => c["Jwt:Issuer"]).Returns("Issuer");
            _configurationMock.Setup(c => c["Jwt:Audience"]).Returns("Audience");
            _jwtService = new JwtService(_configurationMock.Object);
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