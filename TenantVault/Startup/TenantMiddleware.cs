namespace TenantVault.Startup
{
    public class TenantMiddleware(ILogger<TenantMiddleware> logger)
    {
        private readonly ILogger<TenantMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next, ISettableTenantContext tenantContext)
        {
            var tenantId = context.User?.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
            if (tenantId is null)
            {
                _logger.LogError("Tenant context is not available.");
                throw new UnauthorizedAccessException("Tenant context is not available.");
            }
            else
            {
                tenantContext.SetTenantId(tenantId);
            }

            await next.Invoke(context);
        }
    }
}
