using Shared.Models;
using Shared.Contracts;
using Microsoft.Extensions.Logging;
using Prometheus;
using System.Diagnostics;

namespace UserService.Services;

public class AuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly IAuthenticationService _authenticationService;
    private readonly Processors.JwtService _jwtService;
    private readonly Processors.TokenService _tokenService;
	private readonly Processors.AuthorizationService _authorizationService;
    private static readonly Counter _loginCounter = Metrics.CreateCounter("myapp_login_total", "Total number of logins.");
    private static readonly Histogram _loginDuration = Metrics.CreateHistogram("myapp_login_duration_seconds", "Login duration in seconds.");
    private static readonly ActivitySource _activitySource = new ActivitySource("AuthService");

    public AuthService(IAuthenticationService authenticationService, Processors.JwtService jwtService, Processors.TokenService tokenService, Processors.AuthorizationService authorizationService, ILogger<AuthService> logger)
    {
        _authenticationService = authenticationService;
        _jwtService = jwtService;
        _tokenService = tokenService;
		_authorizationService = authorizationService;
        _logger = logger;
    }

    public string Login(string email, string password)
    {
         using (var activity = _activitySource.StartActivity("Login"))
         {
            _logger.LogInformation("Login attempt for user {Email}", email);
            var timer = _loginDuration.NewTimer();
            try
            {
                _loginCounter.Inc();
                activity?.SetTag("email", email);
                _logger.LogInformation("User {Email} logged in successfully", email);
                return _jwtService.GenerateToken(email);
            }
            finally
            {
                timer.Dispose();
            }
        }
    }

    public bool Register(string email, string password, UserRole role)
    {
        // TODO: Implement user registration logic
  //For now return true
        return true;
    }

 public bool Logout(string token)
 {
  //TODO: Implement logout logic
  //For now return true
  return true;
 }

 public string Refresh(string refreshToken)
 {
  //TODO: Implement refresh token logic
  // For now, just return a dummy token
  return _jwtService.GenerateToken("dummy_user");
 }

    public bool RequestPasswordReset(string email)
    {
        // TODO: Implement password reset request logic
        // For now, just return true
        return true;
    }

    public bool ResetPassword(string email, string token, string newPassword)
    {
        // TODO: Implement reset password logic
  // For now, just return true
        return true;
    }
}