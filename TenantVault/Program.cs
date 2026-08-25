using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Serilog;
using System.Text.Json;
using TenantVault.BusinessLogic;
using TenantVault.DataAccess;
using TenantVault.Models;
using TenantVault.Startup;

namespace TenantVault
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureCosmos(builder);
            ConfigureLogging(builder);
            ConfigureExceptionHandling(builder);

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

            app.UseExceptionHandler();
            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }

        private static void ConfigureCosmos(WebApplicationBuilder builder)
        {
            // ValidateDataAnnotations + ValidateOnStart make a bad/missing Cosmos setting fail
            // immediately at app startup with a clear message, instead of surfacing later as a
            // raw Cosmos SDK exception the first time a request happens to need the client.
            builder.Services
                .AddOptions<CosmosOptions>()
                .Bind(builder.Configuration.GetSection(CosmosOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            // CosmosClient is expensive to construct and is documented as thread-safe, so it's
            // registered once as a singleton and reused for the app's lifetime.
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

            // Runs the database/container create-if-not-exists check once at startup via
            // IHostedService, instead of repeating it on every request that touches Cosmos.
            builder.Services.AddHostedService<CosmosBootstrapper>();
        }

        // Registers a global IExceptionHandler so a domain exception thrown anywhere in the
        // business logic layer becomes a clean, typed HTTP response (see ApiExceptionHandler),
        // without any controller needing its own try/catch.
        private static void ConfigureExceptionHandling(WebApplicationBuilder builder)
        {
            builder.Services.AddExceptionHandler<ApiExceptionHandler>();
            builder.Services.AddProblemDetails();
        }

        private static void ConfigureLogging(WebApplicationBuilder builder)
        {
            // Serilog reads its own settings from appsettings.json/appsettings.Development.json
            // (see the "Serilog" section) instead of a hardcoded level/sink here, so the log
            // level can differ per environment without a code change.
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);
            });
        }
    }
}
