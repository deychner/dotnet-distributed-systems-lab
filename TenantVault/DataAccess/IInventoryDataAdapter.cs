using TenantVault.Models;

namespace TenantVault.DataAccess
{
    public interface IInventoryDataAdapter
    {
        public Task AddVehicleAsync(string tenantId, Vehicle vehicle, CancellationToken cancellationToken);
    }
}
