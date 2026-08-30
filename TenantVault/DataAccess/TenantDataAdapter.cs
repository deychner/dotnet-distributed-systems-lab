using Microsoft.Azure.Cosmos;
using System.Net;
using TenantVault.DataAccess.Models;
using TenantVault.Startup;

namespace TenantVault.DataAccess
{
    public partial class TenantDataAdapter(
        CosmosClient cosmosClient,
        CosmosOptions options,
        ITenantContext tenantContext,
        ILogger<TenantDataAdapter> logger) : ITenantDataAdapter
    {
        // Resolved once and cached as a field rather than looked up on every call.
        private readonly Container _container = cosmosClient.GetContainer(options.DatabaseName, options.ContainerName);
        private readonly string _tenantId = tenantContext.GetTenantId();
        private readonly ILogger<TenantDataAdapter> _logger = logger;

        public async Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            // Do not trust the caller to set the tenantId on the vehicle, since this is a multi-tenant service
            // and the caller could be malicious or buggy. Always pull from the ITenantContext.
            vehicle.TenantId = _tenantId;

            var partitionKey = BuildPartitionKey(vehicle.TenantId, vehicle.WarehouseId);

            var response = await _container.CreateItemAsync(vehicle, partitionKey, cancellationToken: cancellationToken);

            LogRequestCharge(nameof(AddVehicleAsync), response.RequestCharge);

            // vehicle.Id was already generated client-side before this call, so it's returned
            // directly instead of reading response.Resource.Id - response.Resource is Cosmos
            // echoing the full document back over the wire, a real round trip, not something
            // already sitting in memory, and it's not needed here.
            return vehicle.Id;
        }

        public async Task<Vehicle?> GetVehicleAsync(int warehouseId, Guid vehicleId, CancellationToken cancellationToken)
        {
            var partitionKey = BuildPartitionKey(_tenantId, warehouseId);

            try
            {
                var response = await _container.ReadItemAsync<Vehicle>(vehicleId.ToString(), partitionKey, cancellationToken: cancellationToken);

                LogRequestCharge(nameof(GetVehicleAsync), response.RequestCharge);
                LogRecordCount(nameof(GetVehicleAsync), 1);

                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Idiomatic Cosmos SDK pattern for a point read: catch the specific 404 and
                // return null instead of letting exception-driven control flow reach the caller.
                LogRecordCount(nameof(GetVehicleAsync), 0);
                return null;
            }
        }

        // Full partition key (tenantId + warehouseId) supplied, no WHERE clause needed: Cosmos
        // routes the query directly to the matching logical partition.
        public Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(int warehouseId, CancellationToken cancellationToken)
        {
            QueryDefinition query = new("SELECT * FROM c");

            QueryRequestOptions requestOptions = new()
            {
                PartitionKey = BuildPartitionKey(_tenantId, warehouseId)
            };

            return ExecuteQueryAsync(query, requestOptions, nameof(GetVehiclesByWarehouseAsync), cancellationToken);
        }

        // Partial/prefix partition key (tenantId only): Cosmos still scopes the query to just
        // that tenant's logical partitions before the WHERE predicate runs.
        public Task<IEnumerable<Vehicle>> GetVehiclesByTenantAsync(CancellationToken cancellationToken)
        {
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.tenantId = @tenantId")
                .WithParameter("@tenantId", _tenantId);

            QueryRequestOptions requestOptions = new()
            {
                PartitionKey = BuildPartitionKey(_tenantId)
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

            LogRequestCharge(operationName, totalRequestCharge);
            LogRecordCount(operationName, vehicles.Count);

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

        [LoggerMessage(Level = LogLevel.Information, Message = "{operation} Request Charge: {charge}")]
        private partial void LogRequestCharge(string operation, double charge);

        [LoggerMessage(Level = LogLevel.Information, Message = "{operation} Record Count: {count}")]
        private partial void LogRecordCount(string operation, int count);
    }
}
