using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TenantVault.BusinessLogic.Exceptions;

namespace TenantVault.Startup
{
    public class ApiExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (statusCode, title) = exception switch
            {
                VehicleValidationException => (StatusCodes.Status400BadRequest, "Invalid vehicle data"),
                InventoryUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Inventory temporarily unavailable"),
                _ => (0, string.Empty)
            };

            if (statusCode == 0)
            {
                return false;
            }

            httpContext.Response.StatusCode = statusCode;

            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            }, cancellationToken);

            return true;
        }
    }
}
