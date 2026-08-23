using TenantVault.Models;

namespace TenantVault.DataAccess
{
    public interface IInventoryDataAdapter
    {
        public Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);

        public Task<Vehicle?> GetVehicleAsync(string tenantId, string warehouseId, Guid vehicleId, CancellationToken cancellationToken);

        public Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(string tenantId, string warehouseId, CancellationToken cancellationToken);
    }
}
