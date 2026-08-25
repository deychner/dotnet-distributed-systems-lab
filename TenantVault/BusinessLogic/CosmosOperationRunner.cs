using System.Net;
using Microsoft.Azure.Cosmos;
using TenantVault.BusinessLogic.Exceptions;

namespace TenantVault.BusinessLogic
{
    internal static class CosmosOperationRunner
    {
        public static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                return await operation();
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new InventoryUnavailableException("The inventory store is temporarily unavailable. Please try again shortly.", ex);
            }
        }
    }
}
