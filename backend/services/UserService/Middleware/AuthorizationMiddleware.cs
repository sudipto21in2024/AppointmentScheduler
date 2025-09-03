using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Shared.Contracts;

namespace UserService.Middleware;

public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuthenticationService _authenticationService;
    private readonly Processors.AuthorizationService _authorizationService;

    public AuthorizationMiddleware(RequestDelegate next, IAuthenticationService authenticationService, Processors.AuthorizationService authorizationService)
    {
        _next = next;
        _authenticationService = authenticationService;
        _authorizationService = authorizationService;
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
                // Get the user's role from the token (dummy)
                string userRole = "Customer";

                // Get the resource being accessed (dummy)
                string resource = context.Request.Path;

                // Check if the user is authorized to access the resource
                if (_authorizationService.Authorize("dummy_user_id", userRole, resource))
                {
                    // User is authorized, call the next middleware in the pipeline
                    await _next(context);
                }
                else
                {
                    // User is not authorized, return a forbidden status code
                    context.Response.StatusCode = 403;
                    return;
                }
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