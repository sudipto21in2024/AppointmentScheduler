using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Shared.Contracts;

namespace UserService.Middleware;

public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Shared.Contracts.IAuthenticationService _authenticationService;

    public AuthorizationMiddleware(RequestDelegate next, Shared.Contracts.IAuthenticationService authenticationService)
    {
        _next = next;
        _authenticationService = authenticationService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the request has an authorization header
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Response.StatusCode = 401;
            return;
        }

        string? token = context.Request.Headers["Authorization"];
		
		if (string.IsNullOrEmpty(token))
		{
			context.Response.StatusCode = 401;
			return;
		}

		      // Validate the token
		      if (!await _authenticationService.ValidateJwtToken(token))
		      {
		          context.Response.StatusCode = 401;
		          return;
		      }

        await _next(context);
    }
}