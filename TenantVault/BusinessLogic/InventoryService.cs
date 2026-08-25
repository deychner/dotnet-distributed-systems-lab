using TenantVault.BusinessLogic.Exceptions;
using TenantVault.DataAccess;
using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public class InventoryService(IInventoryDataAdapter inventoryDataAdapter) : IInventoryService
    {
        private const int MinimumVehicleYear = 1900;

        private readonly IInventoryDataAdapter _inventoryDataAdapter = inventoryDataAdapter;

        // Validates business rules the data layer has no way to enforce: Vehicle's [JsonRequired]
        // only guarantees a field was present in the payload, not that its value makes sense.
        public async Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
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

            // Duplicate-spot check: read-then-write, not atomic, so two concurrent creates for
            // the same spot could both pass this check. Left as-is since it hasn't come up yet;
            // closing the gap for real would need a Cosmos-level constraint (e.g. a deterministic
            // document ID derived from warehouse+spot).
            var vehiclesInWarehouse = await CosmosOperationRunner.ExecuteAsync(
                () => _inventoryDataAdapter.GetVehiclesByWarehouseAsync(vehicle.TenantId!, vehicle.WarehouseId, cancellationToken));
            if (vehiclesInWarehouse.Any(v => v.SpotId == vehicle.SpotId))
            {
                throw new VehicleValidationException($"Warehouse {vehicle.WarehouseId} spot {vehicle.SpotId} is already occupied.");
            }

            return await CosmosOperationRunner.ExecuteAsync(() => _inventoryDataAdapter.AddVehicleAsync(vehicle, cancellationToken));
        }

        // Every adapter call is routed through CosmosOperationRunner so a Cosmos SDK exception
        // (currently: 429 throttling) is translated into a domain exception here, instead of the
        // Cosmos SDK type leaking up to controllers/callers that shouldn't need to know about it.
        public Task<Vehicle?> GetVehicleAsync(string tenantId, int warehouseId, Guid vehicleId, CancellationToken cancellationToken) =>
            CosmosOperationRunner.ExecuteAsync(() => _inventoryDataAdapter.GetVehicleAsync(tenantId, warehouseId, vehicleId, cancellationToken));

        // Plain pass-throughs like this return the adapter's Task directly rather than using
        // async/await to await-and-return a single call, which would just add an unnecessary
        // state machine.
        public Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(string tenantId, int warehouseId, CancellationToken cancellationToken) =>
            CosmosOperationRunner.ExecuteAsync(() => _inventoryDataAdapter.GetVehiclesByWarehouseAsync(tenantId, warehouseId, cancellationToken));

        public Task<IEnumerable<Vehicle>> GetVehiclesByTenantAsync(string tenantId, CancellationToken cancellationToken) =>
            CosmosOperationRunner.ExecuteAsync(() => _inventoryDataAdapter.GetVehiclesByTenantAsync(tenantId, cancellationToken));
    }
}
