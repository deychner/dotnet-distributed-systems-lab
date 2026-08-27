using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Net;
using TenantVault.Models;

namespace TenantVault.DataAccess
{
    public class InventoryDataAdapter(CosmosClient cosmosClient, CosmosOptions options, ILogger<InventoryDataAdapter> logger) : IInventoryDataAdapter
    {
        // Resolved once and cached as a field rather than looked up on every call.
        private readonly Container _container = cosmosClient.GetContainer(options.DatabaseName, options.ContainerName);
        private readonly ILogger<InventoryDataAdapter> _logger = logger;

        public async Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            var partitionKey = BuildPartitionKey(vehicle.TenantId, vehicle.WarehouseId);

            var response = await _container.CreateItemAsync(vehicle, partitionKey, cancellationToken: cancellationToken);

            _logger.LogInformation("AddVehicleAsync Request Charge: {charge}", response.RequestCharge);

            // vehicle.Id was already generated client-side before this call, so it's returned
            // directly instead of reading response.Resource.Id - response.Resource is Cosmos
            // echoing the full document back over the wire, a real round trip, not something
            // already sitting in memory, and it's not needed here.
            return vehicle.Id;
        }

        public async Task<Vehicle?> GetVehicleAsync(string tenantId, int warehouseId, Guid vehicleId, CancellationToken cancellationToken)
        {
            var partitionKey = BuildPartitionKey(tenantId, warehouseId);

            try
            {
                var response = await _container.ReadItemAsync<Vehicle>(vehicleId.ToString(), partitionKey, cancellationToken: cancellationToken);

                _logger.LogInformation("GetVehicleAsync Request Charge: {charge}", response.RequestCharge);
                _logger.LogInformation("GetVehicleAsync Record Count: 1");

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Idiomatic Cosmos SDK pattern for a point read: catch the specific 404 and
                // return null instead of letting exception-driven control flow reach the caller.
                _logger.LogInformation("GetVehicleAsync Record Count: 0");
                return null;
            }
        }

        // Full partition key (tenantId + warehouseId) supplied, no WHERE clause needed: Cosmos
        // routes the query directly to the matching logical partition.
        public Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(string tenantId, int warehouseId, CancellationToken cancellationToken)
        {
            QueryDefinition query = new("SELECT * FROM c");

            QueryRequestOptions requestOptions = new()
            {
                PartitionKey = BuildPartitionKey(tenantId, warehouseId)
            };

            return ExecuteQueryAsync(query, requestOptions, nameof(GetVehiclesByWarehouseAsync), cancellationToken);
        }

        // Partial/prefix partition key (tenantId only): Cosmos still scopes the query to just
        // that tenant's logical partitions before the WHERE predicate runs.
        public Task<IEnumerable<Vehicle>> GetVehiclesByTenantAsync(string tenantId, CancellationToken cancellationToken)
        {
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.tenantId = @tenantId")
                .WithParameter("@tenantId", tenantId);

            QueryRequestOptions requestOptions = new()
            {
                PartitionKey = BuildPartitionKey(tenantId)
            };

            return ExecuteQueryAsync(query, requestOptions, nameof(GetVehiclesByTenantAsync), cancellationToken);
        }

        // No partition key at all: year isn't part of the partition key, so this is a true
        // cross-partition fan-out query - the expensive query shape, intentionally only
        // reachable through AdminService rather than the tenant-scoped InventoryController.
        public Task<IEnumerable<Vehicle>> GetVehiclesByYearAsync(int year, CancellationToken cancellationToken)
        {
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.year = @year")
                .WithParameter("@year", year);

            return ExecuteQueryAsync(query, requestOptions: null, nameof(GetVehiclesByYearAsync), cancellationToken);
        }

        // Shared by all three query methods above: iterating a FeedIterator and accumulating
        // charge/count was duplicated near-verbatim across each of them before this was pulled
        // out, so this is the one place that logic (and its RU-charge/record-count logging) lives.
        private async Task<IEnumerable<Vehicle>> ExecuteQueryAsync(
            QueryDefinition query,
            QueryRequestOptions? requestOptions,
            string operationName,
            CancellationToken cancellationToken)
        {
            using var iterator = _container.GetItemQueryIterator<Vehicle>(query, requestOptions: requestOptions);
            double totalRequestCharge = 0D;

            var vehicles = new List<Vehicle>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                vehicles.AddRange(response.Resource);
                totalRequestCharge += response.RequestCharge;
            }

            _logger.LogInformation("{operation} Request Charge: {charge}", operationName, totalRequestCharge);
            _logger.LogInformation("{operation} Record Count: {count}", operationName, vehicles.Count);

            return vehicles;
        }

        // PartitionKeyBuilder composes the hierarchical partition key (tenantId, then
        // warehouseId) matching the container's configured PartitionKeyPaths.
        private static PartitionKey BuildPartitionKey(string? tenantId, int warehouseId)
        {
            return new PartitionKeyBuilder()
                .Add(tenantId)
                .Add(warehouseId)
                .Build();
        }

        private static PartitionKey BuildPartitionKey(string? tenantId)
        {
            return new PartitionKeyBuilder()
                .Add(tenantId)
                .Build();
        }
    }
}
