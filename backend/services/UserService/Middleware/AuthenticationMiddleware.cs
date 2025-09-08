using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace UserService.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the request has an authorization header
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Response.StatusCode = 401;
        }

        // Token is valid, call the next middleware in the pipeline
        await _next(context);
    }
}