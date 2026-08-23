using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Net;
using TenantVault.Models;

namespace TenantVault.DataAccess
{
    public class InventoryDataAdapter(CosmosClient cosmosClient, IOptions<CosmosOptions> options) : IInventoryDataAdapter
    {
        private readonly Container _container = cosmosClient.GetContainer(options.Value.DatabaseName, options.Value.ContainerName);

        public async Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            var partitionKey = BuildPartitionKey(vehicle.TenantId, vehicle.WarehouseId);

            var response = await _container.CreateItemAsync(vehicle, partitionKey, cancellationToken: cancellationToken);
            return response.Resource.Id;
        }

        public async Task<Vehicle?> GetVehicleAsync(string tenantId, int warehouseId, Guid vehicleId, CancellationToken cancellationToken)
        {
            var partitionKey = BuildPartitionKey(tenantId, warehouseId);

            try
            {
                var response = await _container.ReadItemAsync<Vehicle>(vehicleId.ToString(), partitionKey, cancellationToken: cancellationToken);
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(string tenantId, int warehouseId, CancellationToken cancellationToken)
        {
            QueryDefinition query = new("SELECT * FROM c");

            QueryRequestOptions requestOptions = new()
            {
                PartitionKey = BuildPartitionKey(tenantId, warehouseId)
            };

            using var iterator = _container.GetItemQueryIterator<Vehicle>(query, requestOptions: requestOptions);

            return await GetFromFeed(iterator, cancellationToken);
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesByTenantAsync(string tenantId, CancellationToken cancellationToken)
        {
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.tenantId = @tenantId")
                .WithParameter("@tenantId", tenantId);

            QueryRequestOptions requestOptions = new()
            {
                PartitionKey = BuildPartitionKey(tenantId)
            };

            using var iterator = _container.GetItemQueryIterator<Vehicle>(query, requestOptions: requestOptions);

            return await GetFromFeed(iterator, cancellationToken);
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesByYearAsync(int year, CancellationToken cancellationToken)
        {
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.year = @year")
                .WithParameter("@year", year);

            using var iterator = _container.GetItemQueryIterator<Vehicle>(query);

            return await GetFromFeed(iterator, cancellationToken);
        }

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

        private static async Task<IEnumerable<T>> GetFromFeed<T>(FeedIterator<T> iterator, CancellationToken cancellationToken)
        {
            var items = new List<T>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                items.AddRange(response.Resource);
            }

            return items;
        }
    }
}
