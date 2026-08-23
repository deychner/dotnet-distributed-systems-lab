using TenantVault.DataAccess;
using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public class InventoryService(IInventoryDataAdapter inventoryDataAdapter) : IInventoryService
    {
        private readonly IInventoryDataAdapter _inventoryDataAdapter = inventoryDataAdapter;

        public async Task<Guid> AddVehicleAsync(string tenantId, Vehicle vehicle, CancellationToken cancellationToken)
        {
            return await _inventoryDataAdapter.AddVehicleAsync(tenantId, vehicle, cancellationToken);
        }
    }
}
