using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Maestros.Almacenes.Entities;
using ProjectAW.Infrastructure.Errors;

namespace ProjectAW.Modules.Maestros.Almacenes.Repositories
{
    public class AlmacenRepository:IAlmacenRepository
    {
        private readonly AppDbContext _context;

        public AlmacenRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task ActualizarAlmacenAsync(Almacen almacen)
        {
            _context.Entry(almacen).State = EntityState.Modified;
            return _context.SaveChangesAsync();
        }

        public async Task<bool> ActualizarAlmacenAsync(Almacen almacen, IReadOnlyCollection<int> idUbicacionesDespacho)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var actual = await _context.Almacenes.FirstOrDefaultAsync(x => x.IdAlmacen == almacen.IdAlmacen);
            if (actual is null) return false;

            var seleccionadas = idUbicacionesDespacho.Distinct().ToHashSet();
            var ubicacionIds = await _context.Ubicaciones.Where(x => x.IdAlmacen == actual.IdAlmacen).Select(x => x.IdUbicacion).ToListAsync();
            if (seleccionadas.Except(ubicacionIds).Any())
                throw new ReglaNegocioException("Selecciona únicamente ubicaciones pertenecientes a este almacén.");

            actual.Codigo = almacen.Codigo;
            actual.Descripcion = almacen.Descripcion;
            actual.Activo = almacen.Activo;
            actual.FchModificacion = almacen.FchModificacion;
            var actuales = await _context.UbicacionesDespacho.Where(x => x.IdAlmacen == actual.IdAlmacen).ToListAsync();
            _context.UbicacionesDespacho.RemoveRange(actuales);
            await _context.UbicacionesDespacho.AddRangeAsync(seleccionadas.Select(id => new ProjectAW.Modules.Maestros.Ubicaciones.Entities.UbicacionDespacho { IdAlmacen = actual.IdAlmacen, IdUbicacion = id }));

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }

        public async Task CrearAlmacenAsync(Almacen almacen)
        {
            await _context.Almacenes.AddAsync(almacen);
            await _context.SaveChangesAsync();
        }

        public async Task<Almacen?> ObtenerAlmacenPorCodigoAsync(string codigo)
        {
            return await _context.Almacenes.AsNoTracking().Include(a => a.UbicacionesDespacho).FirstOrDefaultAsync(a => a.Codigo == codigo);
        }

        public async Task<Almacen?> ObtenerAlmacenPorIdAsync(int id)
        {
            return await _context.Almacenes.AsNoTracking().Include(a => a.UbicacionesDespacho).FirstOrDefaultAsync(a => a.IdAlmacen == id);
        }

        public async Task<List<Almacen>> ObtenerAlmacenesAsync()
        {
            return await _context.Almacenes.AsNoTracking().Include(a => a.UbicacionesDespacho).ToListAsync();
        }
    }
}
