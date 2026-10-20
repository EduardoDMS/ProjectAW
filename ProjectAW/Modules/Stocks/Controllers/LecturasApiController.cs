using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Stocks.DTOs.Requests;
using ProjectAW.Modules.Stocks.Services;

namespace ProjectAW.Modules.Stocks.Controllers;

/// <summary>
/// API compartida de captura operativa. Las reglas específicas de inventario
/// se resuelven en el procesador de Recepción, Despacho o Traslado.
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

    /// <summary>Asigna al operador autenticado una guía que siga pendiente.</summary>
    [HttpPost("{idGuia:int}/tomar")]
    public async Task<IActionResult> Tomar(int idGuia, CancellationToken ct)
    {
        await _service.TomarGuiaAsync(idGuia, ct);
        return NoContent();
    }

    /// <summary>Guarda una captura individual sin cerrar la guía ni modificar inventario.</summary>
    [HttpPost("{idGuia:int}/lecturas")]
    public async Task<IActionResult> RegistrarLectura(
        int idGuia, [FromBody] RegistrarLecturaRequest request, CancellationToken ct) =>
        Ok(await _service.RegistrarLecturaAsync(idGuia, request, ct));

    /// <summary>Devuelve el historial persistido de la guía del operador autenticado.</summary>
    [HttpGet("{idGuia:int}/lecturas")]
    public async Task<IActionResult> Historial(int idGuia, CancellationToken ct) =>
        Ok(await _service.ListarLecturasAsync(idGuia, ct));

    /// <summary>Quita una lectura pendiente para corregir una asignación antes de finalizar la guía.</summary>
    [HttpDelete("{idGuia:int}/lecturas/{idLecturaGuia:long}")]
    public async Task<IActionResult> EliminarLectura(int idGuia, long idLecturaGuia, CancellationToken ct)
    {
        if (!await _service.EliminarLecturaAsync(idGuia, idLecturaGuia, ct))
            return NotFound();
        return NoContent();
    }

    /// <summary>Aplica el historial en una transacción, genera movimientos y atiende la guía.</summary>
    [HttpPost("{idGuia:int}/finalizar")]
    public async Task<IActionResult> Finalizar(int idGuia, CancellationToken ct)
    {
        await _service.FinalizarLecturaAsync(idGuia, ct);
        return NoContent();
    }
}
