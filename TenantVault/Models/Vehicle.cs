using System.Text.Json.Serialization;

namespace TenantVault.Models
{
    public class Vehicle
    {
        [JsonRequired]
        public string? TenantId { get; set; }

        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonRequired]
        public string? Make { get; set; }

        [JsonRequired]
        public string? Model { get; set; }

        [JsonRequired]
        public int Year { get; set; }

        [JsonRequired]
        public string? WarehouseId { get; set; }

        [JsonRequired]
        public string? SpotId { get; set; }
    }
}
