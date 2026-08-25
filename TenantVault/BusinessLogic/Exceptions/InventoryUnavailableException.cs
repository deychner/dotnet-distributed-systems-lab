namespace TenantVault.BusinessLogic.Exceptions
{
    public class InventoryUnavailableException(string message, Exception innerException) : Exception(message, innerException)
    {
    }
}
