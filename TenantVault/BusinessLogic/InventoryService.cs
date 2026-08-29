using TenantVault.BusinessLogic.Exceptions;
using TenantVault.DataAccess;
using TenantVault.DataAccess.Models;
using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public class InventoryService(ITenantContext tenantContext, ITenantDataAdapter tenantDataAdapter) : IInventoryService
    {
        private const int MinimumVehicleYear = 1900;

        private readonly string _tenantId = tenantContext.GetTenantId();
        private readonly ITenantDataAdapter _tenantDataAdapter = tenantDataAdapter;

        // Validates business rules the data layer has no way to enforce: Vehicle's [JsonRequired]
        // only guarantees a field was present in the payload, not that its value makes sense.
        public async Task<Guid> AddVehicleAsync(CreateVehicleRequest vehicle, CancellationToken cancellationToken)
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
                () => _tenantDataAdapter.GetVehiclesByWarehouseAsync(vehicle.WarehouseId, cancellationToken));
            if (vehiclesInWarehouse.Any(v => v.SpotId == vehicle.SpotId))
            {
                throw new VehicleValidationException($"Warehouse {vehicle.WarehouseId} spot {vehicle.SpotId} is already occupied.");
            }

            Vehicle cosmosVehicle = new()
            {
                TenantId = _tenantId,
                Make = vehicle.Make,
                Model = vehicle.Model,
                Year = vehicle.Year,
                WarehouseId = vehicle.WarehouseId,
                SpotId = vehicle.SpotId
            };

            return await CosmosOperationRunner.ExecuteAsync(() => _tenantDataAdapter.AddVehicleAsync(cosmosVehicle, cancellationToken));
        }

        // Every adapter call is routed through CosmosOperationRunner so a Cosmos SDK exception
        // (currently: 429 throttling) is translated into a domain exception here, instead of the
        // Cosmos SDK type leaking up to controllers/callers that shouldn't need to know about it.
        public Task<Vehicle?> GetVehicleAsync(int warehouseId, Guid vehicleId, CancellationToken cancellationToken) =>
            CosmosOperationRunner.ExecuteAsync(() => _tenantDataAdapter.GetVehicleAsync(warehouseId, vehicleId, cancellationToken));

        // Plain pass-throughs like this return the adapter's Task directly rather than using
        // async/await to await-and-return a single call, which would just add an unnecessary
        // state machine.
        public Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(int warehouseId, CancellationToken cancellationToken) =>
            CosmosOperationRunner.ExecuteAsync(() => _tenantDataAdapter.GetVehiclesByWarehouseAsync(warehouseId, cancellationToken));

        public Task<IEnumerable<Vehicle>> GetVehiclesByTenantAsync(CancellationToken cancellationToken) =>
            CosmosOperationRunner.ExecuteAsync(() => _tenantDataAdapter.GetVehiclesByTenantAsync(cancellationToken));
    }
}
