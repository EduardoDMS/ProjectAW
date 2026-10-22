using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Guias.Entities;

namespace ProjectAW.Modules.Guias.Repositories;

public sealed class MotivoGuiaRepository(AppDbContext context) : IMotivoGuiaRepository
{
    public async Task<IReadOnlyList<MotivoGuia>> ListarAsync(int? idOperacion, bool soloActivos, CancellationToken ct = default) =>
        await context.MotivosGuia.AsNoTracking().Include(x => x.TipoOperacionGuia).Include(x => x.TipoComportamiento)
            .Where(x => (!idOperacion.HasValue || x.IdTipoOperacionGuia == idOperacion) && (!soloActivos || x.Activo))
            .OrderBy(x => x.TipoOperacionGuia!.Nombre).ThenBy(x => x.NombreMotivo).ToListAsync(ct);

    public Task<MotivoGuia?> ObtenerAsync(int id, CancellationToken ct = default) =>
        context.MotivosGuia.Include(x => x.TipoOperacionGuia).Include(x => x.TipoComportamiento)
            .FirstOrDefaultAsync(x => x.IdMotivoGuia == id, ct);

    public async Task<IReadOnlyList<TipoComportamientoGuia>> ListarComportamientosAsync(CancellationToken ct = default) =>
        await context.TiposComportamientoGuia.AsNoTracking().OrderBy(x => x.IdTipoComportamiento).ToListAsync(ct);

    public Task<bool> ExisteOperacionAsync(int id, CancellationToken ct = default) =>
        context.TiposOperacionesGuia.AnyAsync(x => x.IdTipoOperacionGuia == id, ct);
    public Task<bool> ExisteComportamientoAsync(int id, CancellationToken ct = default) =>
        context.TiposComportamientoGuia.AnyAsync(x => x.IdTipoComportamiento == id, ct);
    public Task<bool> NombreExisteAsync(int idOperacion, string nombre, int? excluirId = null, CancellationToken ct = default) =>
        context.MotivosGuia.AnyAsync(x => x.IdTipoOperacionGuia == idOperacion && x.NombreMotivo == nombre && x.IdMotivoGuia != excluirId, ct);
    public Task<bool> EstaEnUsoAsync(int id, CancellationToken ct = default) =>
        context.CabeceraGuia.AnyAsync(x => x.IdMotivoGuia == id, ct);
    public async Task AgregarAsync(MotivoGuia motivo, CancellationToken ct = default) => await context.MotivosGuia.AddAsync(motivo, ct);
    public void Eliminar(MotivoGuia motivo) => context.MotivosGuia.Remove(motivo);
    public Task GuardarAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
