using TenantVault.DataAccess;

namespace TenantVault.BusinessLogic
{
    public class InventoryService(IInventoryDataAdapter inventoryDataAdapter) : IInventoryService
    {
        private readonly IInventoryDataAdapter _inventoryDataAdapter = inventoryDataAdapter;

        public Task AddVehicleAsync()
        {
            return _inventoryDataAdapter.AddVehicleAsync();
        }
    }
}
