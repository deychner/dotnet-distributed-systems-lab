using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TenantVault.Models;

namespace TenantVault.Startup
{
    // Implemented as an IHostedService so the create-if-not-exists work runs exactly once,
    // during app startup, rather than being repeated every time a request touches Cosmos.
    public class CosmosBootstrapper(CosmosClient client, IOptions<CosmosOptions> options) : IHostedService
    {
        private readonly CosmosClient _client = client;
        private readonly IOptions<CosmosOptions> _options = options;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var options = _options.Value;

            // Gated by the AutoProvision config flag (see CosmosOptions) instead of an
            // IsDevelopment() check, so the behavior is explicit and can be enabled for other
            // environments (e.g. CI) without adding more environment-name checks here.
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
