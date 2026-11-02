using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ProjectAW.Modules.Guias.DTOs.Request;
using ProjectAW.Modules.Guias.Services;
using ProjectAW.Modules.Seguridad.Security.HttpAuth;

namespace ProjectAW.Modules.Guias.Controllers;

[ApiController]
[Route("api/guias/motivos")]
[Authorize(AuthenticationSchemes = AuthCookieSchemes.Admin)]
[EnableCors("PwaRead")]
public sealed class MotivosGuiaApiController : ControllerBase
{
    private readonly IMotivoGuiaService _service;

    public MotivosGuiaApiController(IMotivoGuiaService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] int? idTipoOperacionGuia,
        [FromQuery] bool soloActivos = false,
        CancellationToken ct = default)
    {
        var motivos = await _service.ListarAsync(idTipoOperacionGuia, soloActivos, ct);
        return Ok(motivos);
    }

    [HttpGet("comportamientos")]
    public async Task<IActionResult> Comportamientos(CancellationToken ct)
    {
        var comportamientos = await _service.ListarComportamientosAsync(ct);
        return Ok(comportamientos);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct)
    {
        var motivo = await _service.ObtenerAsync(id, ct);
        return Ok(motivo);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarMotivoGuiaRequest request, CancellationToken ct)
    {
        var result = await _service.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = result.IdMotivoGuia }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarMotivoGuiaRequest request, CancellationToken ct)
    {
        var motivo = await _service.ActualizarAsync(id, request, ct);
        return Ok(motivo);
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromQuery] bool activo, CancellationToken ct)
    {
        await _service.CambiarEstadoAsync(id, activo, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await _service.EliminarAsync(id, ct); return NoContent();
    }
}
