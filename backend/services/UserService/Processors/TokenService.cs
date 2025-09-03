namespace UserService.Processors
{
    public class TokenService
    {
        public string GenerateRefreshToken()
        {
            // Dummy implementation
            return "dummy_refresh_token";
        }

        public bool ValidateRefreshToken(string token)
        {
            // Dummy implementation
            return true;
        }
    }
}