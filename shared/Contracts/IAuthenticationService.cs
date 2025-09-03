namespace Shared.Contracts
{
    public interface IAuthenticationService
    {
        string GenerateJwtToken(string userId);
        bool ValidateJwtToken(string token);
    }
}