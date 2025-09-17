using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using PaymentService.Extensions;

namespace PaymentService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Health checks
            builder.Services.AddHealthChecks();

            // MassTransit
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

            // Register the shared ApplicationDbContext
            builder.Services.AddDbContext<Shared.Data.ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register payment services
            builder.Services.AddPaymentServices();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            app.UseRouting();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHealthChecks("/healthz");

            app.Run();
        }
    }
}