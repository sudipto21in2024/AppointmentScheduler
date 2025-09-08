using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using UserService.Utils;

namespace UserService.Middleware;

public class AuthenticationMiddleware
{
    private static readonly ActivitySource ActivitySource = new ActivitySource("UserService.AuthenticationMiddleware");
    private readonly RequestDelegate _next;

    public AuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var activity = ActivitySource.StartActivity("AuthenticationMiddleware.InvokeAsync");
        
        LoggingExtensions.AddTraceIdToLogContext();
        
        try
        {
            // Check if the request has an authorization header
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                context.Response.StatusCode = 401;
                activity?.SetStatus(ActivityStatusCode.Error);
                return;
            }

            // Token is valid, call the next middleware in the pipeline
            await _next(context);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }
}