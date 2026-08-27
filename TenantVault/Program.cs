using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Serilog;
using System.ComponentModel.DataAnnotations;
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

            ConfigureLogging(builder);
            ConfigureDataAccess(builder);
            ConfigureExceptionHandling(builder);

            // Add services to the container.
            builder.Services.AddScoped<IAdminService, AdminService>();
            builder.Services.AddScoped<IInventoryService, InventoryService>();

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

        private static void ConfigureDataAccess(WebApplicationBuilder builder)
        {
            // Validator.ValidateObject makes a bad/missing Cosmos setting fail immediately
            // at app startup with a clear message, instead of surfacing later as a raw
            // Cosmos SDK exception the first time a request happens to need the client.
            var cosmosOptions = new CosmosOptions();
            builder.Configuration.GetSection(CosmosOptions.SectionName).Bind(cosmosOptions);
            Validator.ValidateObject(cosmosOptions, new ValidationContext(cosmosOptions), validateAllProperties: true);

            var clientOptions = new CosmosClientOptions
            {
                ApplicationName = "TenantVault",
                UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }
            };

            // Make a single CosmosClient instance and wrap it up in the InventoryDataAdapter,
            // which is registered as a scoped service.
            var cosmosClient = new CosmosClient(cosmosOptions.AccountEndpoint, cosmosOptions.AccountKey, clientOptions);

            builder.Services.AddScoped<IInventoryDataAdapter, InventoryDataAdapter>(provider => {
                var logger = provider.GetRequiredService<ILogger<InventoryDataAdapter>>();
                return new InventoryDataAdapter(cosmosClient, cosmosOptions, logger);
            });
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
