using Microsoft.AspNetCore.Mvc;
using Tutorial9.Model;
using Tutorial9.Services;

namespace Tutorial9.Controllers;

[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IDbService _dbService;

    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost]
    public async Task<IActionResult> AddProductToWarehouse(Request request)
    {
        try
        {
            var result = await _dbService.AddProductToWarehouseAsync(request);
            return Ok(new { IdProductWarehouse = result });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception)
        {
            return StatusCode(500, "Error");
        }
    }
    [HttpPost("proc")]
    public async Task<IActionResult> AddProductToWarehouseWithProc([FromBody] Request request)
    {
        try
        {
            var id = await _dbService.AddProductToWarehouseWithProcAsync(request);
            return Ok(new { IdProductWarehouse = id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Internal server error");
        }
    }

}


