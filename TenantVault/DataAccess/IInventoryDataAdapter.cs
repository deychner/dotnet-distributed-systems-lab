using TenantVault.Models;

namespace TenantVault.DataAccess
{
    public interface IInventoryDataAdapter
    {
        public Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);
    }
}
