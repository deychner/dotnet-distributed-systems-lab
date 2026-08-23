using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using TenantVault.Models;

namespace TenantVault.DataAccess
{
    public class InventoryDataAdapter(CosmosClient cosmosClient, IOptions<CosmosOptions> options) : IInventoryDataAdapter
    {
        private readonly Container _container = cosmosClient.GetContainer(options.Value.DatabaseName, options.Value.ContainerName);

        public Task AddCarAsync()
        {
            throw new NotImplementedException();
        }
    }
}
