using Microsoft.AspNetCore.Authorization;

namespace TenantVault.Startup
{
    public class TenantMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task InvokeAsync(HttpContext context, ISettableTenantContext tenantContext)
        {
            var isAllowAnonymous = context.GetEndpoint()?.Metadata.Any(a => a is AllowAnonymousAttribute) ?? false;
            if (!isAllowAnonymous)
            {
                var tenantId = context.User?.Claims.FirstOrDefault(c => c.Type == "tenant_id")?.Value;
                if (tenantId is null)
                {
                    throw new UnauthorizedAccessException("Tenant context is not available.");
                }
                else
                {
                    tenantContext.SetTenantId(tenantId);
                }
            }

            await _next.Invoke(context);
        }
    }
}
