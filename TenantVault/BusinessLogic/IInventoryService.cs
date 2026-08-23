using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public interface IInventoryService
    {
        public Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);

        public Task<Vehicle?> GetVehicleAsync(string tenantId, string warehouseId, Guid vehicleId, CancellationToken cancellationToken);
    }
}
