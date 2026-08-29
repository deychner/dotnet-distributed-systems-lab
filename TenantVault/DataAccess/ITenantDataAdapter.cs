using TenantVault.DataAccess.Models;

namespace TenantVault.DataAccess
{
    public interface ITenantDataAdapter
    {
        public Task<Guid> AddVehicleAsync(Vehicle vehicle, CancellationToken cancellationToken);

        public Task<Vehicle?> GetVehicleAsync(int warehouseId, Guid vehicleId, CancellationToken cancellationToken);

        public Task<IEnumerable<Vehicle>> GetVehiclesByWarehouseAsync(int warehouseId, CancellationToken cancellationToken);

        public Task<IEnumerable<Vehicle>> GetVehiclesByTenantAsync(CancellationToken cancellationToken);

        public Task<IEnumerable<Vehicle>> GetVehiclesByYearAsync(int year, CancellationToken cancellationToken);
    }
}
