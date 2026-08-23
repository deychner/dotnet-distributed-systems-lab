using Azure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using System.Net;
using TenantVault.Models;

namespace TenantVault.DataAccess
{
    public class InventoryDataAdapter(CosmosClient cosmosClient, IOptions<CosmosOptions> options, ILogger<InventoryDataAdapter> logger) : IInventoryDataAdapter
    {
        private readonly Container _container = cosmosClient.GetContainer(options.Value.DatabaseName, options.Value.ContainerName);
        private readonly ILogger<InventoryDataAdapter> _logger = logger;

        public async Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            var partitionKey = BuildPartitionKey(vehicle.TenantId, vehicle.WarehouseId);

            var response = await _container.CreateItemAsync(vehicle, partitionKey, cancellationToken: cancellationToken);

            _logger.LogInformation("AddVehicleAsync Request Charge: {charge}", response.RequestCharge);

            return response.Resource.Id;
        }

        public async Task<Vehicle?> GetVehicleAsync(string tenantId, int warehouseId, Guid vehicleId, CancellationToken cancellationToken)
        {
            var partitionKey = BuildPartitionKey(tenantId, warehouseId);

            try
            {
                var response = await _container.ReadItemAsync<Vehicle>(vehicleId.ToString(), partitionKey, cancellationToken: cancellationToken);

                _logger.LogInformation("GetVehicleAsync Request Charge: {charge}", response.RequestCharge);

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
            double totalRequestCharge = 0D;

            var vehicles = new List<Vehicle>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                vehicles.AddRange(response.Resource);
                totalRequestCharge += response.RequestCharge;
            }

            _logger.LogInformation("GetVehiclesByWarehouseAsync Request Charge: {charge}", totalRequestCharge);

            return vehicles;
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
            double totalRequestCharge = 0D;

            var vehicles = new List<Vehicle>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                vehicles.AddRange(response.Resource);
                totalRequestCharge += response.RequestCharge;
            }

            _logger.LogInformation("GetVehiclesByTenantAsync Request Charge: {charge}", totalRequestCharge);

            return vehicles;
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesByYearAsync(int year, CancellationToken cancellationToken)
        {
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.year = @year")
                .WithParameter("@year", year);

            using var iterator = _container.GetItemQueryIterator<Vehicle>(query);
            double totalRequestCharge = 0D;

            var vehicles = new List<Vehicle>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync(cancellationToken);
                vehicles.AddRange(response.Resource);
                totalRequestCharge += response.RequestCharge;
            }

            _logger.LogInformation("GetVehiclesByYearAsync Request Charge: {charge}", totalRequestCharge);

            return vehicles;
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
    }
}
