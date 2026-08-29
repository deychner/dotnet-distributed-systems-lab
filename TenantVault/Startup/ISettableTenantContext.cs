namespace TenantVault.Startup
{
    public interface ISettableTenantContext
    {
        void SetTenantId(string tenantId);
    }
}
