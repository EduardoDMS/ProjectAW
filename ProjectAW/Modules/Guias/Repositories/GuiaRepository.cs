using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;

namespace ProjectAW.Modules.Guias.Repositories
{
    public class GuiaRepository : IGuiaRepository
    {
        private readonly AppDbContext _context;
        public GuiaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AgregarAsync(CabeceraGuia guia, CancellationToken ct = default)
                            => await _context.CabeceraGuia.AddAsync(guia, ct);

        public async Task AgregarParAsync(CabeceraGuia primera, CabeceraGuia relacionada, CancellationToken ct = default)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(ct);
            await _context.CabeceraGuia.AddRangeAsync([primera, relacionada], ct);
            await _context.SaveChangesAsync(ct);
            primera.IdGuiaRelacionada = relacionada.IdGuia;
            relacionada.IdGuiaRelacionada = primera.IdGuia;
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }

        public async Task<bool> ExisteAsync<T>(int id, CancellationToken ct = default) where T : class
                            => await _context.Set<T>().FindAsync(new object[] { id }, ct) is not null;

        public Task GuardarCambiosAsync(CancellationToken ct = default)
                                    => _context.SaveChangesAsync(ct);

        public async Task<IReadOnlyList<CabeceraGuia>> ListarAsync(int? idTipoOperacionGuia, int? idEstadoGuia, CancellationToken ct = default)
        {
            var query = _context.CabeceraGuia.AsNoTracking().Include(g => g.Detalles)
                .Include(g => g.Proveedor).Include(g => g.Cliente)
                .Include(g => g.AlmacenOrigen).Include(g => g.AlmacenDestino)
                .Include(g => g.Motivo).ThenInclude(m => m!.TipoComportamiento)
                .Include(g => g.GuiaRelacionada).AsQueryable();

            if (idTipoOperacionGuia.HasValue) query = query.Where(g => g.IdTipoOperacionGuia == idTipoOperacionGuia.Value);
            if (idEstadoGuia.HasValue) query = query.Where(g => g.IdEstadoGuia == idEstadoGuia.Value);

            return await query.OrderByDescending(g => g.IdGuia).ToListAsync(ct);
        }

        public async Task<string> ObtenerSiguienteNumeroAsync(int idTipoOperacionGuia, CancellationToken ct = default)
        {
            var (prefijo, sql) = idTipoOperacionGuia switch
            {
                TipoOperacionGuiaIds.Entrada => ("ENT", "SELECT NEXT VALUE FOR dbo.SeqGuiaEntrada AS [Value]"),
                TipoOperacionGuiaIds.Salida => ("SAL", "SELECT NEXT VALUE FOR dbo.SeqGuiaSalida AS [Value]"),
                TipoOperacionGuiaIds.Traslado => ("TRA", "SELECT NEXT VALUE FOR dbo.SeqGuiaTraslado AS [Value]"),
                _ => throw new ArgumentOutOfRangeException(nameof(idTipoOperacionGuia))
            };

            var valores = await _context.Database.SqlQueryRaw<int>(sql).ToListAsync(ct);
            return $"{prefijo}-{valores[0]:D6}";
        }

        public Task<CabeceraGuia?> ObtenerPorIdAsync(int idGuia, CancellationToken ct = default) =>
        _context.CabeceraGuia.Include(g => g.Detalles)
                                             .Include(g => g.Proveedor).Include(g => g.Cliente)
                                             .Include(g => g.AlmacenOrigen).Include(g => g.AlmacenDestino)
                                             .Include(g => g.Motivo).ThenInclude(m => m!.TipoComportamiento)
                                             .Include(g => g.GuiaRelacionada)
                                             .FirstOrDefaultAsync(g => g.IdGuia == idGuia, ct);
    }
}
