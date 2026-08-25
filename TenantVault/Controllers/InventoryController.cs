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
            return CreatedAtAction(nameof(GetVehicleAsync), new { tenantId = vehicle.TenantId, warehouseId = vehicle.WarehouseId, vehicleId }, vehicleId);
        }

        [HttpGet("vehicle/{tenantId}/{warehouseId:int}/{vehicleId:guid}")]
        public async Task<ActionResult<Vehicle?>> GetVehicleAsync(
            [FromRoute] string tenantId,
            [FromRoute] int warehouseId,
            [FromRoute] Guid vehicleId,
            CancellationToken cancellationToken)
        {
            var vehicle = await _inventoryService.GetVehicleAsync(tenantId, warehouseId, vehicleId, cancellationToken);
            return vehicle is null ? NotFound() : Ok(vehicle);
        }

        [HttpGet("vehicles/{tenantId}/{warehouseId:int}")]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehiclesByWarehouseAsync(
            [FromRoute] string tenantId,
            [FromRoute] int warehouseId,
            CancellationToken cancellationToken)
        {
            var vehicles = await _inventoryService.GetVehiclesByWarehouseAsync(tenantId, warehouseId, cancellationToken);
            return Ok(vehicles);
        }

        [HttpGet("vehicles/{tenantId}")]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehiclesByTenantAsync(
            [FromRoute] string tenantId,
            CancellationToken cancellationToken)
        {
            var vehicles = await _inventoryService.GetVehiclesByTenantAsync(tenantId, cancellationToken);
            return Ok(vehicles);
        }
    }
}
