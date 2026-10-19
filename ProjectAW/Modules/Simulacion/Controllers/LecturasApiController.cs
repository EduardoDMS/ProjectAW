using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Stocks.DTOs.Requests;
using ProjectAW.Modules.Stocks.Services;

namespace ProjectAW.Modules.Simulacion.Controllers;


/// <summary>
/// API de captura de lecturas por operador. Las capturas quedan en historial y el stock
/// se aplica de forma transaccional únicamente al finalizar la guía.
/// </summary>
[Authorize]
[ApiController]
[Route("api/simulacion/guias")]
[EnableCors("PwaOperations")]
public sealed class LecturasApiController : ControllerBase
{
    private readonly IMovimientoStockService _service;

    public LecturasApiController(IMovimientoStockService service) => _service = service;

    /// <summary>Lista las guías en proceso o atendidas que pertenecen al operador autenticado.</summary>
    [HttpGet("mias")]
    public async Task<IActionResult> MisGuias([FromQuery] int? idEstadoGuia, CancellationToken ct) =>
        Ok(await _service.ListarMisGuiasAsync(idEstadoGuia, ct));

    /// <summary>Obtiene el detalle de una guía asignada al operador autenticado.</summary>
    [HttpGet("mias/{idGuia:int}")]
    public async Task<IActionResult> MiGuia(int idGuia, CancellationToken ct) =>
        Ok(await _service.ObtenerMiGuiaAsync(idGuia, ct));

    /// <summary>
    /// El operador toma una guía pendiente y pasa a ser su responsable hasta terminarla.
    /// </summary>
    [HttpPost("{idGuia:int}/tomar")]
    public async Task<IActionResult> Tomar(int idGuia, CancellationToken ct)
    {
        await _service.TomarGuiaAsync(idGuia, ct);
        return NoContent();
    }

    /// <summary>Guarda una captura individual sin cerrar la guía ni modificar el inventario.</summary>
    [HttpPost("{idGuia:int}/lecturas")]
    public async Task<IActionResult> RegistrarLectura(int idGuia, [FromBody] RegistrarLecturaRequest request, CancellationToken ct) =>
        Ok(await _service.RegistrarLecturaAsync(idGuia, request, ct));

    /// <summary>Devuelve el historial persistido de la guía del operador autenticado.</summary>
    [HttpGet("{idGuia:int}/lecturas")]
    public async Task<IActionResult> Historial(int idGuia, CancellationToken ct) =>
        Ok(await _service.ListarLecturasAsync(idGuia, ct));

    /// <summary>
    /// Aplica el historial acumulado en una transacción, genera movimientos y atiende la guía.
    /// </summary>
    [HttpPost("{idGuia:int}/finalizar")]
    public async Task<IActionResult> Finalizar(int idGuia, CancellationToken ct)
    {
        await _service.FinalizarLecturaAsync(idGuia, ct);
        return NoContent();
    }
}
