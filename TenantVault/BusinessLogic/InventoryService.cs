using TenantVault.DataAccess;
using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public class InventoryService(IInventoryDataAdapter inventoryDataAdapter) : IInventoryService
    {
        private readonly IInventoryDataAdapter _inventoryDataAdapter = inventoryDataAdapter;

        public async Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            return await _inventoryDataAdapter.AddVehicleAsync(vehicle, cancellationToken);
        }

        public async Task<Vehicle?> GetVehicleAsync(string tenantId, int warehouseId, Guid vehicleId, CancellationToken cancellationToken)
        {
            return await _inventoryDataAdapter.GetVehicleAsync(tenantId, warehouseId, vehicleId, cancellationToken);
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(string tenantId, int warehouseId, CancellationToken cancellationToken)
        {
            return await _inventoryDataAdapter.GetVehiclesByWarehouseAsync(tenantId, warehouseId, cancellationToken);
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesByTenantAsync(string tenantId, CancellationToken cancellationToken)
        {
            return await _inventoryDataAdapter.GetVehiclesByTenantAsync(tenantId, cancellationToken);
        }

        public Task<IEnumerable<Vehicle>> GetVehiclesByYearAsync(int year, CancellationToken cancellationToken)
        {
            return _inventoryDataAdapter.GetVehiclesByYearAsync(year, cancellationToken);
        }
    }
}
