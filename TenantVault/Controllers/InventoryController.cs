using Microsoft.AspNetCore.Mvc;
using TenantVault.BusinessLogic;

namespace TenantVault.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InventoryController(IInventoryService inventoryService) : ControllerBase
    {
        private readonly IInventoryService _inventoryService = inventoryService;

        [HttpPost("vehicle")]
        public async Task<IActionResult> AddVehicleAsync()
        {
            await _inventoryService.AddVehicleAsync();
            return Ok();
        }
    }
}
