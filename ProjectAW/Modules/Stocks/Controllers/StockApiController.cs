using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Stocks.Services;

namespace ProjectAW.Modules.Stocks.Controllers;

[ApiController]
[Route("api/stocks")]
public sealed class StockApiController : ControllerBase
{
    private readonly IStockService _service;

    public StockApiController(IStockService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? idAlmacen,
        [FromQuery] int? idProducto,
        CancellationToken ct) =>
        Ok(await _service.ListarAsync(idAlmacen, idProducto, ct));
}
