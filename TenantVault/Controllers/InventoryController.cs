using Microsoft.AspNetCore.Mvc;
using TenantVault.BusinessLogic;

namespace TenantVault.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InventoryController(IInventoryService inventoryService) : ControllerBase
    {
        private readonly IInventoryService _inventoryService = inventoryService;

        [HttpPost("car")]
        public async Task<IActionResult> AddCarAsync()
        {
            await _inventoryService.AddCarAsync();
            return Ok();
        }
    }
}
