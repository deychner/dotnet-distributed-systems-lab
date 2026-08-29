using TenantVault.DataAccess.Models;
using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public interface IInventoryService
    {
        public Task<Guid> AddVehicleAsync(CreateVehicleRequest vehicle, CancellationToken cancellationToken);

        public Task<Vehicle?> GetVehicleAsync(int warehouseId, Guid vehicleId, CancellationToken cancellationToken);

        public Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(int warehouseId, CancellationToken cancellationToken);

        public Task<IEnumerable<Vehicle>> GetVehiclesByTenantAsync(CancellationToken cancellationToken);
    }
}
