namespace TenantVault.Models
{
    public class CosmosOptions
    {
        public const string SectionName = "Cosmos";

        public string AccountEndpoint { get; set; } = default!;
        public string AccountKey { get; set; } = default!;
        public string DatabaseName { get; set; } = default!;
        public string ContainerName { get; set; } = default!;

        public IList<string> PartitionKeyPaths { get; set; } = [];

        public int Throughput { get; set; }
        public bool UseAutoscale { get; set; }
    }
}
