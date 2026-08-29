using System.Text.Json.Serialization;

namespace TenantVault.Models
{
    public class CreateVehicleRequest
    {
        [JsonRequired]
        public string? Make { get; set; }

        [JsonRequired]
        public string? Model { get; set; }

        [JsonRequired]
        public int Year { get; set; }

        [JsonRequired]
        public int WarehouseId { get; set; }

        [JsonRequired]
        public int SpotId { get; set; }
    }
}
