using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TenantVault.Models;

namespace TenantVault.Startup
{
    public class CosmosBootstrapper(CosmosClient client, IOptions<CosmosOptions> options) : IHostedService
    {
        private readonly CosmosClient _client = client;
        private readonly IOptions<CosmosOptions> _options = options;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var options = _options.Value;
            if (!options.AutoProvision) return;

            var database = await _client.CreateDatabaseIfNotExistsAsync(options.DatabaseName, cancellationToken: cancellationToken);

            var containerProperties = new ContainerProperties
            {
                Id = options.ContainerName,
                PartitionKeyPaths = options.PartitionKeyPaths.AsReadOnly(),
            };

            ThroughputProperties throughputProperties = options.UseAutoscale
                ? ThroughputProperties.CreateAutoscaleThroughput(options.Throughput)
                : ThroughputProperties.CreateManualThroughput(options.Throughput);

            await database.Database.CreateContainerIfNotExistsAsync(containerProperties, throughputProperties, cancellationToken: cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
