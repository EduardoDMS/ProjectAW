using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Stocks.Services;

namespace ProjectAW.Modules.Stocks.Controllers;

[ApiController]
[Route("api/movimientos-stock")]
public sealed class MovimientosApiController : ControllerBase
{
    private readonly IMovimientoStockService _service;

    public MovimientosApiController(IMovimientoStockService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? idGuia,
        [FromQuery] int? idAlmacen,
        [FromQuery] int? idOperacion,
        CancellationToken ct) =>
        Ok(await _service.ListarAsync(idGuia, idAlmacen, idOperacion, ct));
}
