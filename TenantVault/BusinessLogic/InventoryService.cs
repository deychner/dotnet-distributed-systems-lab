using TenantVault.DataAccess;
using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public class InventoryService(IInventoryDataAdapter inventoryDataAdapter) : IInventoryService
    {
        private readonly IInventoryDataAdapter _inventoryDataAdapter = inventoryDataAdapter;

        public async Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            return await _inventoryDataAdapter.AddVehicleAsync(vehicle, cancellationToken);
        }
    }
}
