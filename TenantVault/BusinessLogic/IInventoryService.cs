using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public interface IInventoryService
    {
        public Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);
    }
}
