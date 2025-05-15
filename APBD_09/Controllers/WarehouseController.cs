


using APBD_09.Models;
using APBD_09.Services;
using Microsoft.AspNetCore.Mvc;

namespace APBD_09.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;
        
        public WarehouseController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddProduct(AddProdRequest request)
        {
            try
            {
                var id = await _warehouseService.AddProduct(request);
                return Ok(new { ProductWarehouseId = id });
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }
            catch (InvalidOperationException e)
            {
                return Conflict(e.Message);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpPost("add/sp")]
        public async Task<IActionResult> AddProductWithProcedure(AddProdRequest request)
        {
            try
            {
                var id = await _warehouseService.AddProductWithProcedure(request);
                return Ok(new { ProductWarehouseId = id });
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }
    }
}
