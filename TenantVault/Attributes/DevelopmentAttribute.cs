using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TenantVault.Attributes
{
    public class DevelopmentAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Resolve the environment service from the HttpContext
            var env = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

            if (!env.IsDevelopment())
            {
                // Short-circuit the request and return a 404
                context.Result = new NotFoundResult();
            }

            base.OnActionExecuting(context);
        }
    }
}