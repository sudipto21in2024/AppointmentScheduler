using Microsoft.Extensions.DependencyInjection;
using MediatR;
using MassTransit;
using Microsoft.Extensions.Hosting;

namespace UserService;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

                services.AddMassTransit(x =>
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

                // Placeholder for authentication service registration
                services.AddSingleton<Services.AuthService>();
                services.AddSingleton<Processors.JwtService>(x => new Processors.JwtService("MySuperSecretKey"));
                services.AddSingleton<Processors.TokenService>();
                services.AddSingleton<Processors.AuthorizationService>();
    services.AddControllers();
            })
            .Build();

        Console.WriteLine("UserService started.");
        await host.RunAsync();
    }
}