using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
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

            // Add Cosmos to the container
            builder.Services
                .AddOptions<CosmosOptions>()
                .Bind(builder.Configuration.GetSection(CosmosOptions.SectionName))
                .ValidateOnStart();

            builder.Services.AddSingleton<CosmosClient>(provider =>
            {
                var options = provider.GetRequiredService<IOptions<CosmosOptions>>().Value;
                return new CosmosClient(options.AccountEndpoint, options.AccountKey);
            });

            builder.Services.AddHostedService<CosmosBootstrapper>();

            // Add services to the container.
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
    }
}
