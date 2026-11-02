using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Guias.Entities;

namespace ProjectAW.Modules.Guias.Repositories;

public sealed class MotivoGuiaRepository : IMotivoGuiaRepository
{
    private readonly AppDbContext _context;

    public MotivoGuiaRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<MotivoGuia>> ListarAsync(
        int? idOperacion, bool soloActivos, CancellationToken ct = default)
    {
        var query = ConsultaMotivoCompleto().AsNoTracking();

        if (idOperacion.HasValue)
            query = query.Where(motivo => motivo.IdTipoOperacionGuia == idOperacion.Value);
        if (soloActivos)
            query = query.Where(motivo => motivo.Activo);
        ///espera devolver una lista de motivos de guía, ordenada primero por el nombre de la operación y luego por el nombre del motivo.
        return await query
            .OrderBy(motivo => motivo.TipoOperacionGuia!.Nombre)
            .ThenBy(motivo => motivo.NombreMotivo)
            .ToListAsync(ct);
    }

    public async Task<MotivoGuia?> ObtenerAsync(int id, CancellationToken ct = default)
    {
        return await ConsultaMotivoCompleto()
            .FirstOrDefaultAsync(motivo => motivo.IdMotivoGuia == id, ct);
    }

    public async Task<IReadOnlyList<TipoComportamientoGuia>> ListarComportamientosAsync(
        CancellationToken ct = default)
    {
        return await _context.TiposComportamientoGuia.AsNoTracking()
            .OrderBy(comportamiento => comportamiento.IdTipoComportamiento)
            .ToListAsync(ct);
    }

    public async Task<bool> ExisteOperacionAsync(int id, CancellationToken ct = default)
    {
        return await _context.TiposOperacionesGuia.AnyAsync(operacion => operacion.IdTipoOperacionGuia == id, ct);
    }

    public async Task<bool> ExisteComportamientoAsync(int id, CancellationToken ct = default)
    {
        return await _context.TiposComportamientoGuia.AnyAsync(comportamiento => comportamiento.IdTipoComportamiento == id, ct);
    }

    public async Task<bool> NombreExisteAsync(
        int idOperacion, string nombre, int? excluirId = null, CancellationToken ct = default)
    {
        return await _context.MotivosGuia.AnyAsync(motivo =>
            motivo.IdTipoOperacionGuia == idOperacion &&
            motivo.NombreMotivo == nombre &&
            motivo.IdMotivoGuia != excluirId, ct);
    }

    public async Task<bool> EstaEnUsoAsync(int id, CancellationToken ct = default)
    {
        return await _context.CabeceraGuia.AnyAsync(guia => guia.IdMotivoGuia == id, ct);
    }

    public async Task AgregarAsync(MotivoGuia motivo, CancellationToken ct = default)
    {
        await _context.MotivosGuia.AddAsync(motivo, ct);
    }

    public void Eliminar(MotivoGuia motivo)
    {
        _context.MotivosGuia.Remove(motivo);
    }

    public async Task GuardarAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }

    /// <summary>Incluye los catálogos necesarios para validar motivos y mapear sus respuestas.</summary>
    private IQueryable<MotivoGuia> ConsultaMotivoCompleto() => _context.MotivosGuia
        .Include(motivo => motivo.TipoOperacionGuia)
        .Include(motivo => motivo.TipoComportamiento);
}
