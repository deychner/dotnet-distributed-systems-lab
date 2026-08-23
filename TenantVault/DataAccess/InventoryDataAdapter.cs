using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.ClientModel.Primitives;
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

        public async Task<Vehicle?> GetVehicleAsync(string tenantId, string warehouseId, Guid vehicleId, CancellationToken cancellationToken)
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

        public async Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(string tenantId, string warehouseId, CancellationToken cancellationToken)
        {
            QueryDefinition query = new("SELECT * FROM c");

            QueryRequestOptions requestOptions = new()
            {
                PartitionKey = BuildPartitionKey(tenantId, warehouseId)
            };

            using var iterator = _container.GetItemQueryIterator<Vehicle>(query, requestOptions: requestOptions);

            var vehicles = new List<Vehicle>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                vehicles.AddRange(response.Resource);
            }

            return vehicles;
        }

        private static PartitionKey BuildPartitionKey(string? tenantId, string? warehouseId)
        {
            return new PartitionKeyBuilder()
                .Add(tenantId)
                .Add(warehouseId)
                .Build();
        }
    }
}
