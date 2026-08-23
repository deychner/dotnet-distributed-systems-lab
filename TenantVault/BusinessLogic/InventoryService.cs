using TenantVault.DataAccess;
using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public class InventoryService(IInventoryDataAdapter inventoryDataAdapter) : IInventoryService
    {
        private readonly IInventoryDataAdapter _inventoryDataAdapter = inventoryDataAdapter;

        public Task AddVehicleAsync(string tenantId, Vehicle vehicle, CancellationToken cancellationToken)
        {
            return _inventoryDataAdapter.AddVehicleAsync(tenantId, vehicle, cancellationToken);
        }
    }
}
