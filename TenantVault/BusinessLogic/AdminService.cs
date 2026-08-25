using TenantVault.DataAccess;
using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public class AdminService(IInventoryDataAdapter inventoryDataAdapter) : IAdminService
    {
        private readonly IInventoryDataAdapter _inventoryDataAdapter = inventoryDataAdapter;

        public Task<IEnumerable<Vehicle>> GetVehiclesByYearAsync(int year, CancellationToken cancellationToken)
        {
            return _inventoryDataAdapter.GetVehiclesByYearAsync(year, cancellationToken);
        }
    }
}
