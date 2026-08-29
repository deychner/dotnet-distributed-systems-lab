using Microsoft.AspNetCore.Authorization;

namespace TenantVault.Startup
{
    public class TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<TenantMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context, ISettableTenantContext tenantContext)
        {
            var isAllowAnonymous = context.GetEndpoint()?.Metadata.Any(a => a is AllowAnonymousAttribute) ?? false;
            if (isAllowAnonymous)
            {
                _logger.LogInformation("AllowAnonymous attribute found. Skipping tenant context setup.");
            }
            else
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
