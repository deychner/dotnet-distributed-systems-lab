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
        public async Task<ActionResult<Guid>> AddVehicleAsync(
            [FromBody, Required] Vehicle vehicle,
            CancellationToken cancellationToken)
        {
            var vehicleId = await _inventoryService.AddVehicleAsync(vehicle, cancellationToken);
            return Ok(vehicleId);
        }

        [HttpGet("vehicle/{tenantId}/{warehouseId}/{vehicleId:guid}")]
        public async Task<ActionResult<Vehicle?>> GetVehicleAsync(
            [FromRoute] string tenantId,
            [FromRoute] string warehouseId,
            [FromRoute] Guid vehicleId,
            CancellationToken cancellationToken)
        {
            var vehicle = await _inventoryService.GetVehicleAsync(tenantId, warehouseId, vehicleId, cancellationToken);
            return vehicle is null ? NotFound() : Ok(vehicle);
        }

        [HttpGet("vehicle/{tenantId}/{warehouseId}")]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehiclesByWarehouseAsync(
            [FromRoute] string tenantId,
            [FromRoute] string warehouseId,
            CancellationToken cancellationToken)
        {
            var vehicles = await _inventoryService.GetVehiclesByWarehouseAsync(tenantId, warehouseId, cancellationToken);
            return vehicles.Any() ? Ok(vehicles) : NotFound();
        }
    }
}
