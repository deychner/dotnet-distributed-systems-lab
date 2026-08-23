using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using TenantVault.BusinessLogic;
using TenantVault.Models;

namespace TenantVault.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InventoryController(IInventoryService inventoryService) : ControllerBase
    {
        private readonly IInventoryService _inventoryService = inventoryService;

        [HttpPost("vehicle")]
        public async Task<IActionResult> AddVehicleAsync([FromQuery]string tenantId, [FromBody,Required] Vehicle vehicle, CancellationToken cancellationToken)
        {
            await _inventoryService.AddVehicleAsync(tenantId, vehicle, cancellationToken);
            return Ok();
        }
    }
}
