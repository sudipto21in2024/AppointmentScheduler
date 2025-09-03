using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Shared.Contracts;

namespace UserService.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
	private readonly IAuthenticationService _authenticationService;

    public AuthenticationMiddleware(RequestDelegate next, IAuthenticationService authenticationService)
    {
        _next = next;
		_authenticationService = authenticationService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Dummy implementation
        // Check if the request has an authorization header
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            string token = context.Request.Headers["Authorization"];
            // Validate the token
            if (_authenticationService.ValidateJwtToken(token))
            {
                // Token is valid, call the next middleware in the pipeline
                await _next(context);
            }
            else
            {
                // Token is invalid, return an unauthorized status code
                context.Response.StatusCode = 401;
                return;
            }
        }
        else
        {
            // No authorization header, return an unauthorized status code
            context.Response.StatusCode = 401;
            return;
        }
    }
}