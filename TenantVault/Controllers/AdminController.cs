using Microsoft.AspNetCore.Mvc;
using TenantVault.BusinessLogic;
using TenantVault.Models;

namespace TenantVault.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdminController(IAdminService adminService) : ControllerBase
    {
        private readonly IAdminService _adminService = adminService;

        [HttpGet("vehicles")]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehiclesAsync(
            [FromQuery] int? year,
            CancellationToken cancellationToken)
        {
            if (year is null)
            {
                return BadRequest("Year parameter is required.");
            }

            var vehicles = await _adminService.GetVehiclesByYearAsync(year.Value, cancellationToken);
            return Ok(vehicles);
        }
    }
}
