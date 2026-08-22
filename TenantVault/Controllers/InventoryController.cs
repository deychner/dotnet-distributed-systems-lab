using Microsoft.AspNetCore.Mvc;

namespace TenantVault.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class InventoryController : ControllerBase
    {
        [HttpPost("car")]
        public async Task<IActionResult> AddCarAsync()
        {
            return Ok();
        }
    }
}
