using System.ComponentModel.DataAnnotations;

namespace TenantVault.Models
{
    public class CosmosOptions
    {
        public const string SectionName = "Cosmos";

        // Explicit, config-driven replacement for an IHostEnvironment.IsDevelopment() check:
        // defaults to false everywhere, and only Development's appsettings turns it on. This
        // keeps "should this app auto-provision the database/container" a deliberate setting
        // instead of an assumption baked into an environment name.
        public bool AutoProvision { get; set; } = false;

        // [Required] here makes a missing/blank value fail at startup (via ValidateDataAnnotations
        // in Program.cs) rather than surfacing later as a raw Cosmos SDK error.
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

        // 400 RU/s is Cosmos's actual minimum, so this also catches "forgot to set it" (which
        // would otherwise silently bind to 0 and fail later inside the Cosmos SDK).
        [Range(400, int.MaxValue)]
        public int Throughput { get; set; }

        // No [Required]/[Range] on the bools below: RequiredAttribute never fails on a
        // non-nullable value type (it only checks for null), so it would be a no-op here.
        public bool UseAutoscale { get; set; }
    }
}
