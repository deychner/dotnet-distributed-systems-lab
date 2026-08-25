using System.ComponentModel.DataAnnotations;

namespace TenantVault.Models
{
    public class CosmosOptions
    {
        public const string SectionName = "Cosmos";

        public bool AutoProvision { get; set; } = false;

        [Required]
        public string AccountEndpoint { get; set; } = default!;

        [Required]
        public string AccountKey { get; set; } = default!;

        [Required]
        public string DatabaseName { get; set; } = default!;

        [Required]
        public string ContainerName { get; set; } = default!;

        [Required]
        public IList<string> PartitionKeyPaths { get; set; } = [];

        [Range(400, int.MaxValue)]
        public int Throughput { get; set; }

        public bool UseAutoscale { get; set; }
    }
}
