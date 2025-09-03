using Shared.Models;
using Shared.Contracts;

namespace UserService.Services;

public class AuthService
{
    private readonly IAuthenticationService _authenticationService;
    private readonly Processors.JwtService _jwtService;
    private readonly Processors.TokenService _tokenService;
	private readonly Processors.AuthorizationService _authorizationService;

    public AuthService(IAuthenticationService authenticationService, Processors.JwtService jwtService, Processors.TokenService tokenService, Processors.AuthorizationService authorizationService)
    {
        _authenticationService = authenticationService;
        _jwtService = jwtService;
        _tokenService = tokenService;
		_authorizationService = authorizationService;
    }

    public string Login(string email, string password)
    {
        // TODO: Implement user authentication and validation logic
        // For now, just generate a dummy token
        return _jwtService.GenerateToken(email);
    }

    public bool Register(string email, string password, UserRole role)
    {
        // TODO: Implement user registration logic
        return true;
    }

    public bool Logout(string token)
    {
        //TODO: Implement logout logic
        return true;
    }

    public string Refresh(string refreshToken)
    {
        //TODO: Implement refresh token logic
        return "dummy_token";
    }

    public bool RequestPasswordReset(string email)
    {
        // TODO: Implement password reset request logic
        return true;
    }

    public bool ResetPassword(string email, string token, string newPassword)
    {
        // TODO: Implement reset password logic
        return true;
    }
}