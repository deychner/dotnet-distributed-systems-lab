using TenantVault.BusinessLogic.Exceptions;
using TenantVault.DataAccess;
using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public class InventoryService(IInventoryDataAdapter inventoryDataAdapter) : IInventoryService
    {
        private const int MinimumVehicleYear = 1900;

        private readonly IInventoryDataAdapter _inventoryDataAdapter = inventoryDataAdapter;

        public Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken) =>
            CosmosOperationRunner.ExecuteAsync(() => AddValidatedVehicleAsync(vehicle, cancellationToken));

        public Task<Vehicle?> GetVehicleAsync(string tenantId, int warehouseId, Guid vehicleId, CancellationToken cancellationToken) =>
            CosmosOperationRunner.ExecuteAsync(() => _inventoryDataAdapter.GetVehicleAsync(tenantId, warehouseId, vehicleId, cancellationToken));

        public Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(string tenantId, int warehouseId, CancellationToken cancellationToken) =>
            CosmosOperationRunner.ExecuteAsync(() => _inventoryDataAdapter.GetVehiclesByWarehouseAsync(tenantId, warehouseId, cancellationToken));

        public Task<IEnumerable<Vehicle>> GetVehiclesByTenantAsync(string tenantId, CancellationToken cancellationToken) =>
            CosmosOperationRunner.ExecuteAsync(() => _inventoryDataAdapter.GetVehiclesByTenantAsync(tenantId, cancellationToken));

        private async Task<Guid> AddValidatedVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            ValidateVehicle(vehicle);

            var vehiclesInWarehouse = await _inventoryDataAdapter.GetVehiclesByWarehouseAsync(vehicle.TenantId!, vehicle.WarehouseId, cancellationToken);
            if (vehiclesInWarehouse.Any(v => v.SpotId == vehicle.SpotId))
            {
                throw new VehicleValidationException($"Warehouse {vehicle.WarehouseId} spot {vehicle.SpotId} is already occupied.");
            }

            return await _inventoryDataAdapter.AddVehicleAsync(vehicle, cancellationToken);
        }

        private static void ValidateVehicle(Vehicle vehicle)
        {
            if (vehicle.Year < MinimumVehicleYear || vehicle.Year > DateTime.UtcNow.Year + 1)
            {
                throw new VehicleValidationException($"Year {vehicle.Year} is not a valid vehicle year.");
            }

            if (vehicle.WarehouseId <= 0)
            {
                throw new VehicleValidationException($"WarehouseId {vehicle.WarehouseId} must be a positive number.");
            }

            if (vehicle.SpotId <= 0)
            {
                throw new VehicleValidationException($"SpotId {vehicle.SpotId} must be a positive number.");
            }
        }
    }
}
