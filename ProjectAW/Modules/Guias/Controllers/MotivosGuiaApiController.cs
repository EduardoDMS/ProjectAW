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
public sealed class MotivosGuiaApiController(IMotivoGuiaService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] int? idTipoOperacionGuia, [FromQuery] bool soloActivos = false, CancellationToken ct = default) =>
        Ok(await service.ListarAsync(idTipoOperacionGuia, soloActivos, ct));

    [HttpGet("comportamientos")]
    public async Task<IActionResult> Comportamientos(CancellationToken ct) => Ok(await service.ListarComportamientosAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken ct) => Ok(await service.ObtenerAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] GuardarMotivoGuiaRequest request, CancellationToken ct)
    {
        var result = await service.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = result.IdMotivoGuia }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarMotivoGuiaRequest request, CancellationToken ct) =>
        Ok(await service.ActualizarAsync(id, request, ct));

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromQuery] bool activo, CancellationToken ct)
    {
        await service.CambiarEstadoAsync(id, activo, ct); return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await service.EliminarAsync(id, ct); return NoContent();
    }
}
