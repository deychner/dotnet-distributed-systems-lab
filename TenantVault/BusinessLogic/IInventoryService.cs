using Microsoft.AspNetCore.Mvc;
using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public interface IInventoryService
    {
        public Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);

        public Task<Vehicle?> GetVehicleAsync(string tenantId, int warehouseId, Guid vehicleId, CancellationToken cancellationToken);

        public Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(string tenantId, int warehouseId, CancellationToken cancellationToken);

        public Task<IEnumerable<Vehicle>> GetVehiclesByTenantAsync(string tenantId, CancellationToken cancellationToken);
    }
}
