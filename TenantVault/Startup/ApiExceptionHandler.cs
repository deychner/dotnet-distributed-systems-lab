using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TenantVault.BusinessLogic.Exceptions;

namespace TenantVault.Startup
{
    // Single place that maps business-logic exceptions to HTTP responses, so controllers stay
    // free of try/catch and any endpoint that throws one of these gets consistent handling.
    // Anything not listed here falls through to ASP.NET Core's default handling (a 500).
    public class ApiExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var (statusCode, title) = exception switch
            {
                VehicleValidationException => (StatusCodes.Status400BadRequest, "Invalid vehicle data"),
                InventoryUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Inventory temporarily unavailable"),
                UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized access"),
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
