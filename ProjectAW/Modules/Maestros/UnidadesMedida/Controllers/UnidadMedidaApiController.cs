using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Maestros.UnidadesMedida.Entities;

namespace ProjectAW.Modules.Maestros.UnidadesMedida.Controllers;

[ApiController]
[Route("api/unidades-medida")]
public class UnidadMedidaApiController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public Task<List<UnidadMedida>> Listar(CancellationToken ct) => db.UnidadesMedida.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(ct);

    [HttpPost]
    public async Task<IActionResult> Crear(UnidadMedida request, CancellationToken ct)
    {
        request.IdUnidadMedida = 0; request.Nombre = request.Nombre.Trim(); request.Codigo = request.Codigo.Trim().ToUpperInvariant(); request.Activo = true;
        if (string.IsNullOrWhiteSpace(request.Nombre) || string.IsNullOrWhiteSpace(request.Codigo)) return BadRequest(new { mensaje = "Nombre y código son obligatorios." });
        if (await db.UnidadesMedida.AnyAsync(x => x.Nombre == request.Nombre || x.Codigo == request.Codigo, ct)) return Conflict(new { mensaje = "Ya existe una unidad con ese nombre o código." });
        db.UnidadesMedida.Add(request); await db.SaveChangesAsync(ct); return CreatedAtAction(nameof(Listar), new { id = request.IdUnidadMedida }, request);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, UnidadMedida request, CancellationToken ct)
    {
        var item = await db.UnidadesMedida.FindAsync([id], ct); if (item is null) return NotFound();
        var nombre = request.Nombre.Trim(); var codigo = request.Codigo.Trim().ToUpperInvariant();
        if (await db.UnidadesMedida.AnyAsync(x => x.IdUnidadMedida != id && (x.Nombre == nombre || x.Codigo == codigo), ct)) return Conflict(new { mensaje = "Ya existe otra unidad con ese nombre o código." });
        item.Nombre = nombre; item.Codigo = codigo; item.Activo = request.Activo; await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> Estado(int id, [FromBody] bool activo, CancellationToken ct)
    { var item = await db.UnidadesMedida.FindAsync([id], ct); if (item is null) return NotFound(); item.Activo = activo; await db.SaveChangesAsync(ct); return NoContent(); }
}
