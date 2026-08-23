using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Documents;
using Microsoft.Extensions.Options;
using TenantVault.Models;

namespace TenantVault.DataAccess
{
    public class CosmosBootstrapper(CosmosClient client, IOptions<CosmosOptions> options, IHostEnvironment env) : IHostedService
    {
        private readonly CosmosClient _client = client;
        private readonly IOptions<CosmosOptions> _options = options;
        private readonly IHostEnvironment _env = env;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_env.IsDevelopment()) return;

            var options = _options.Value;
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
