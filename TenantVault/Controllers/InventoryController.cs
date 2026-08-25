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
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Guid>> AddVehicleAsync(
            [FromBody, Required] Vehicle vehicle,
            CancellationToken cancellationToken)
        {
            var vehicleId = await _inventoryService.AddVehicleAsync(vehicle, cancellationToken);
            return CreatedAtAction(nameof(GetVehicleAsync), new { tenantId = vehicle.TenantId, warehouseId = vehicle.WarehouseId, vehicleId }, vehicleId);
        }

        [HttpGet("vehicle/{tenantId}/{warehouseId:int}/{vehicleId:guid}")]
        [ProducesResponseType(typeof(Vehicle), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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
        [ProducesResponseType(typeof(IEnumerable<Vehicle>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehiclesByWarehouseAsync(
            [FromRoute] string tenantId,
            [FromRoute] int warehouseId,
            CancellationToken cancellationToken)
        {
            var vehicles = await _inventoryService.GetVehiclesByWarehouseAsync(tenantId, warehouseId, cancellationToken);
            return Ok(vehicles);
        }

        [HttpGet("vehicles/{tenantId}")]
        [ProducesResponseType(typeof(IEnumerable<Vehicle>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehiclesByTenantAsync(
            [FromRoute] string tenantId,
            CancellationToken cancellationToken)
        {
            var vehicles = await _inventoryService.GetVehiclesByTenantAsync(tenantId, cancellationToken);
            return Ok(vehicles);
        }
    }
}
