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
        public async Task<ActionResult<Guid>> AddVehicleAsync([FromBody,Required] Vehicle vehicle, CancellationToken cancellationToken)
        {
            var vehicleId = await _inventoryService.AddVehicleAsync(vehicle, cancellationToken);
            return Ok(vehicleId);
        }
    }
}
