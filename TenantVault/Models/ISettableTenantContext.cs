namespace TenantVault.Models
{
    public interface ISettableTenantContext
    {
        void SetTenantId(string tenantId);
    }
}
