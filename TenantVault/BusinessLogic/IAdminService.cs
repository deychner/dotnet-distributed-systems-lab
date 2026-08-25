using TenantVault.Models;

namespace TenantVault.BusinessLogic
{
    public interface IAdminService
    {
        public Task<IEnumerable<Vehicle>> GetVehiclesByYearAsync(int year, CancellationToken cancellationToken);
    }
}
