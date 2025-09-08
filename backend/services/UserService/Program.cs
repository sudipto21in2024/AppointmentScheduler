using Microsoft.Extensions.DependencyInjection;
using MediatR;
using MassTransit;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Prometheus;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
// using Shared.Contracts;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog.Formatting.Compact;
using Serilog.Events;
using Microsoft.Extensions.Configuration;
using UserService.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Data;

namespace UserService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // Health checks
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => new HealthCheckResult(HealthStatus.Healthy, "A healthy check result."));

        // MediatR
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

        // MassTransit
        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumer<Consumers.UserRegisteredConsumer>();
            
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host("localhost", "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                
                cfg.ReceiveEndpoint("user-events", e =>
                {
                    e.ConfigureConsumer<Consumers.UserRegisteredConsumer>(context);
                });
            });
        });

        // Register dependencies with proper service lifetimes
        builder.Services.AddScoped<Processors.JwtService>(sp =>
            new Processors.JwtService(sp.GetRequiredService<IConfiguration>()));
        builder.Services.AddScoped<Processors.TokenService>();
        builder.Services.AddScoped<IUserService, UserService.Services.UserService>();
        builder.Services.AddScoped<Shared.Contracts.IAuthenticationService, UserService.Services.AuthenticationService>();
        
        // Register the shared ApplicationDbContext
        builder.Services.AddDbContext<Shared.Data.ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Serilog configuration
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId();
            
            configuration
                .WriteTo.Console()
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(context.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200"))
                {
                    AutoRegisterTemplate = true,
                    IndexFormat = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name?.ToLower().Replace(".", "-")}-{context.HostingEnvironment.EnvironmentName?.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}"
                });
            configuration.ReadFrom.Configuration(context.Configuration);
        });
    
    
            // OpenTelemetry Tracing (updated API)
            builder.Services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService("UserService"))
                .WithTracing(tracingBuilder =>
                {
                    tracingBuilder
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddJaegerExporter(); // Replace Jaeger with Console or use OTLP exporter
                        // For Jaeger, use: .AddJaegerExporter() and configure via environment variables or appsettings
                });
    
            var app = builder.Build();

        // Prometheus metrics
        app.UseMetricServer();
        app.UseHttpMetrics();

        // Configure the HTTP request pipeline.
        app.UseRouting();

        // Authentication and Authorization middleware should be in correct order
        app.UseMiddleware<Middleware.AuthenticationMiddleware>();
        app.UseAuthorization();
        //app.UseMiddleware<Middleware.AuthorizationMiddleware>();

        app.MapControllers();
        app.MapHealthChecks("/healthz");

        app.Run();
    }
}