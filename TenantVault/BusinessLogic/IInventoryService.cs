using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public interface IInventoryService
    {
        public Task AddVehicleAsync(string tenantId, Vehicle vehicle, CancellationToken cancellationToken);
    }
}
