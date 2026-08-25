using Microsoft.AspNetCore.Mvc;
using TenantVault.BusinessLogic;
using TenantVault.Models;

namespace TenantVault.Controllers
{
    // Kept separate from InventoryController specifically because this query is cross-tenant
    // (no tenantId scoping): having it live in its own controller means an authorization policy
    // restricting it to admins can be applied at the controller level later, instead of needing
    // a conditional check inside a shared endpoint.
    [ApiController]
    [Route("[controller]")]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        private readonly IAdminService _adminService = adminService;

        [HttpGet("vehicles")]
        [ProducesResponseType(typeof(IEnumerable<Vehicle>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehiclesAsync(
            [FromQuery] int? year,
            CancellationToken cancellationToken)
        {
            // year is int?, not int: a non-nullable int would silently bind a missing query
            // string to 0 and run a nonsensical query instead of failing with a clear 400.
            if (year is null)
            {
                return BadRequest("Year parameter is required.");
            }

            // 200 even with zero matches, for the same reason as the Inventory collection
            // endpoints: a valid year with no results isn't a missing resource.
            var vehicles = await _adminService.GetVehiclesByYearAsync(year.Value, cancellationToken);
            return Ok(vehicles);
        }
    }
}
