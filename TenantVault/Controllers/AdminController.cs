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

        [HttpGet("vehicle")]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehiclesAsync(
            [FromQuery] int year,
            CancellationToken cancellationToken)
        {
            var vehicles = await _adminService.GetVehiclesByYearAsync(year, cancellationToken);
            return vehicles.Any() ? Ok(vehicles) : NotFound();
        }
    }
}
