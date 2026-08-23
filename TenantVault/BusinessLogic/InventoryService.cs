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

        public async Task<Vehicle?> GetVehicleAsync(string tenantId, string warehouseId, Guid vehicleId, CancellationToken cancellationToken)
        {
            return await _inventoryDataAdapter.GetVehicleAsync(tenantId, warehouseId, vehicleId, cancellationToken);
        }

        public async Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(string tenantId, string warehouseId, CancellationToken cancellationToken)
        {
            return await _inventoryDataAdapter.GetVehiclesByWarehouseAsync(tenantId, warehouseId, cancellationToken);
        }
    }
}
