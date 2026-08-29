using TenantVault.DataAccess;
using TenantVault.DataAccess.Models;

namespace TenantVault.BusinessLogic
{
    public class AdminService(ITenantDataAdapter tenantDataAdapter) : IAdminService
    {
        private readonly ITenantDataAdapter _tenantDataAdapter = tenantDataAdapter;

        // See CosmosOperationRunner for why this goes through ExecuteAsync rather than calling
        // the adapter directly.
        public Task<IEnumerable<Vehicle>> GetVehiclesByYearAsync(int year, CancellationToken cancellationToken) =>
            CosmosOperationRunner.ExecuteAsync(() => _tenantDataAdapter.GetVehiclesByYearAsync(year, cancellationToken));
    }
}
