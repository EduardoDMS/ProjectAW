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
        {
            await _context.CabeceraGuia.AddAsync(guia, ct);
        }

        /// <summary>
        /// Se utiliza una tarea que devuelva un booleano para verificar si existe un registro en la base de datos 
        /// Usando el método FindAsync de Entity Framework Core, que busca un registro por su clave primaria.
        /// Usando la T para indicar que es un método genérico que puede trabajar con cualquier tipo de entidad.
        /// por ellos se envia un new Object[] { id } para que se pueda buscar el registro por su id.
        /// </summary>
        public async Task<bool> ExisteAsync<T>(int id, CancellationToken ct = default) where T : class
        {
            return await _context.Set<T>().FindAsync(new object[] { id }, ct) is not null;
        }

        public async Task GuardarCambiosAsync(CancellationToken ct = default)
        {
            await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        ///      
        /// </summary>
        public async Task<IReadOnlyList<CabeceraGuia>> ListarAsync(int? idTipoOperacionGuia, int? idEstadoGuia, CancellationToken ct = default)
        {
            var query = ConsultaGuiaCompleta().AsNoTracking();

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

            // SingleAsync compone una consulta envolvente; SQL Server no permite
            // NEXT VALUE FOR dentro de una subconsulta.
            var numeros = await _context.Database.SqlQueryRaw<int>(sql).ToListAsync(ct);
            var siguienteNumero = numeros.Single();
            return $"{prefijo}-{siguienteNumero:D6}";
        }

        public async Task<CabeceraGuia?> ObtenerPorIdAsync(int idGuia, CancellationToken ct = default)
        {
            return await ConsultaGuiaCompleta()
                .FirstOrDefaultAsync(guia => guia.IdGuia == idGuia, ct);
        }

        /// <summary>Relaciones compartidas por las consultas de lista y detalle.</summary>
        private IQueryable<CabeceraGuia> ConsultaGuiaCompleta() => _context.CabeceraGuia
            .Include(guia => guia.Detalles)
            .Include(guia => guia.Proveedor)
            .Include(guia => guia.Cliente)
            .Include(guia => guia.AlmacenOrigen)
            .Include(guia => guia.AlmacenDestino)
            .Include(guia => guia.Motivo).ThenInclude(motivo => motivo!.TipoComportamiento)
            .Include(guia => guia.GuiaRelacionada);
    }
}
