using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TenantVault.Models;
using System.Net;

namespace TenantVault.DataAccess
{
    public class InventoryDataAdapter(CosmosClient cosmosClient, IOptions<CosmosOptions> options) : IInventoryDataAdapter
    {
        private readonly Container _container = cosmosClient.GetContainer(options.Value.DatabaseName, options.Value.ContainerName);

        public async Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            var partitionKey = new PartitionKeyBuilder()
                .Add(vehicle.TenantId)
                .Add(vehicle.WarehouseId)
                .Build();

            var response = await _container.CreateItemAsync(vehicle, partitionKey, cancellationToken: cancellationToken);
            return response.Resource.Id;
        }

        public async Task<Vehicle?> GetVehicleAsync(string tenantId, string warehouseId, Guid vehicleId, CancellationToken cancellationToken)
        {
            var partitionKey = new PartitionKeyBuilder()
                .Add(tenantId)
                .Add(warehouseId)
                .Build();

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
    }
}
