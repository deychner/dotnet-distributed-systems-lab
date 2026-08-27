using Microsoft.Azure.Cosmos;
using System.Net;
using TenantVault.BusinessLogic.Exceptions;

namespace TenantVault.BusinessLogic
{
    // Single choke point for translating Cosmos SDK exceptions into business-layer exceptions,
    // used by every adapter call in InventoryService/AdminService. Only 429 (throttling) is
    // translated for now, since other Cosmos status codes don't have a specific business
    // meaning yet - they still propagate unhandled rather than having a meaning invented for them.
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
