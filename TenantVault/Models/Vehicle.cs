using Newtonsoft.Json;

namespace TenantVault.Models
{
    public class Vehicle
    {
        [JsonProperty("id", Required = Required.Always)]
        public Guid Id = Guid.NewGuid();

        [JsonProperty("make", Required = Required.Always)]
        public string? Make { get; set; }

        [JsonProperty("model", Required = Required.Always)]
        public string? Model { get; set; }

        [JsonProperty("year", Required = Required.Always)]
        public int Year { get; set; }

        [JsonProperty("warehouse_id", Required = Required.Always)]
        public int WarehouseId { get; set; }

        [JsonProperty("spot_id", Required = Required.Always)]
        public int SpotId { get; set; }
    }
}
