using TenantVault.DataAccess;
using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public class InventoryService(IInventoryDataAdapter inventoryDataAdapter) : IInventoryService
    {
        private readonly IInventoryDataAdapter _inventoryDataAdapter = inventoryDataAdapter;

        public Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken) =>
            _inventoryDataAdapter.AddVehicleAsync(vehicle, cancellationToken);

        public Task<Vehicle?> GetVehicleAsync(string tenantId, int warehouseId, Guid vehicleId, CancellationToken cancellationToken) =>
            _inventoryDataAdapter.GetVehicleAsync(tenantId, warehouseId, vehicleId, cancellationToken);

        public Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(string tenantId, int warehouseId, CancellationToken cancellationToken) =>
            _inventoryDataAdapter.GetVehiclesByWarehouseAsync(tenantId, warehouseId, cancellationToken);

        public Task<IEnumerable<Vehicle>> GetVehiclesByTenantAsync(string tenantId, CancellationToken cancellationToken) =>
            _inventoryDataAdapter.GetVehiclesByTenantAsync(tenantId, cancellationToken);
    }
}
