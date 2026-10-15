using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Stocks.DTOs.Requests;
using ProjectAW.Modules.Stocks.Services;

namespace ProjectAW.Modules.Simulacion.Controllers;


/// <summary>
/// API temporal para simular el escaneo del operador mientras se construye el cliente de captura.
/// El conteo se envía completo al finalizar para que el servidor lo valide antes de cambiar el stock.
/// </summary>
[Authorize]
[ApiController]
[Route("api/simulacion/guias")]
public sealed class LecturasApiController : ControllerBase
{
    private readonly IMovimientoStockService _service;

    public LecturasApiController(IMovimientoStockService service) => _service = service;

    /// <summary>
    /// El operador toma una guía pendiente y pasa a ser su responsable hasta terminarla.
    /// </summary>
    [HttpPost("{idGuia:int}/tomar")]
    public async Task<IActionResult> Tomar(int idGuia, CancellationToken ct)
    {
        await _service.TomarGuiaAsync(idGuia, ct);
        return NoContent();
    }

    /// <summary>
    /// Registra el conteo final, genera movimientos, actualiza saldos y atiende la guía.
    /// </summary>
    [HttpPost("{idGuia:int}/finalizar")]
    public async Task<IActionResult> Finalizar(
        int idGuia,
        [FromBody] FinalizarLecturaRequest request,
        CancellationToken ct)
    {
        await _service.FinalizarLecturaAsync(idGuia, request, ct);
        return NoContent();
    }
}
