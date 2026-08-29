namespace TenantVault.Models
{
    public class TenantContext : ITenantContext, ISettableTenantContext
    {
        private string? _tenantId;

        public string GetTenantId()
        {
            return _tenantId ?? throw new InvalidOperationException("Tenant ID is not set");
        }

        public void SetTenantId(string tenantId)
        {
            _tenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        }
    }
}
