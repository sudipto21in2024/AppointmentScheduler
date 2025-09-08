using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Shared.Contracts;
using System;
using System.Diagnostics;
using UserService.Utils;

namespace UserService.Middleware;

public class AuthorizationMiddleware
{
    private static readonly ActivitySource ActivitySource = new ActivitySource("UserService.AuthorizationMiddleware");
    private readonly RequestDelegate _next;
    private readonly Shared.Contracts.IAuthenticationService _authenticationService;

    public AuthorizationMiddleware(RequestDelegate next, Shared.Contracts.IAuthenticationService authenticationService)
    {
        _next = next;
        _authenticationService = authenticationService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var activity = ActivitySource.StartActivity("AuthorizationMiddleware.InvokeAsync");
        
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

            string? token = context.Request.Headers["Authorization"];
            
            if (string.IsNullOrEmpty(token))
            {
                context.Response.StatusCode = 401;
                activity?.SetStatus(ActivityStatusCode.Error);
                return;
            }

            // Validate the token
            if (!await _authenticationService.ValidateJwtToken(token))
            {
                context.Response.StatusCode = 401;
                activity?.SetStatus(ActivityStatusCode.Error);
                return;
            }

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