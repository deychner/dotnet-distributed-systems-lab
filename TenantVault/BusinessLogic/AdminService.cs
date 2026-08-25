using TenantVault.DataAccess;
using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public class AdminService(IInventoryDataAdapter inventoryDataAdapter) : IAdminService
    {
        private readonly IInventoryDataAdapter _inventoryDataAdapter = inventoryDataAdapter;

        // See CosmosOperationRunner for why this goes through ExecuteAsync rather than calling
        // the adapter directly.
        public Task<IEnumerable<Vehicle>> GetVehiclesByYearAsync(int year, CancellationToken cancellationToken) =>
            CosmosOperationRunner.ExecuteAsync(() => _inventoryDataAdapter.GetVehiclesByYearAsync(year, cancellationToken));
    }
}
