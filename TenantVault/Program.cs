using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;
using TenantVault.BusinessLogic;
using TenantVault.DataAccess;
using TenantVault.Models;

namespace TenantVault
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureCosmos(builder);
            ConfigureLogging(builder);

            // Add services to the container.
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IInventoryService, InventoryService>();
            builder.Services.AddSingleton<IInventoryDataAdapter, InventoryDataAdapter>();

            builder.Services.AddControllers();

            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }

        private static void ConfigureCosmos(WebApplicationBuilder builder)
        {
            builder.Services
                .AddOptions<CosmosOptions>()
                .Bind(builder.Configuration.GetSection(CosmosOptions.SectionName))
                .ValidateOnStart();

            builder.Services.AddSingleton<CosmosClient>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<CosmosOptions>>().Value;

                var clientOptions = new CosmosClientOptions
                {
                    ApplicationName = "TenantVault",
                    UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }
                };

                return new CosmosClient(options.AccountEndpoint, options.AccountKey, clientOptions);
            });

            builder.Services.AddHostedService<CosmosBootstrapper>();
        }

        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            builder.Services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });
        }
    }
}
