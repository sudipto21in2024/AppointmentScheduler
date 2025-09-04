using Microsoft.Extensions.DependencyInjection;
using MediatR;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace UserService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("A healthy check result."));

        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

        builder.Services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("localhost", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
            });
        });

        builder.Services.AddSingleton<Services.AuthService>();
        builder.Services.AddSingleton<Processors.JwtService>(x => new Processors.JwtService("MySuperSecretKey"));
        builder.Services.AddSingleton<Processors.TokenService>();
        builder.Services.AddSingleton<Processors.AuthorizationService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.

        app.UseRouting();

        app.UseAuthorization();

        app.UseMiddleware<Middleware.AuthenticationMiddleware>();
        app.UseMiddleware<Middleware.AuthorizationMiddleware>();

        app.MapControllers();

  app.MapHealthChecks("/healthz");

        app.Run();
    }
}