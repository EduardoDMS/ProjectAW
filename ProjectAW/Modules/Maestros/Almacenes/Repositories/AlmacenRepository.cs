using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Maestros.Almacenes.Entities;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Modules.Maestros.Ubicaciones.Entities;

namespace ProjectAW.Modules.Maestros.Almacenes.Repositories
{
    public class AlmacenRepository : IAlmacenRepository
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

        public async Task<Almacen?> ObtenerAlmacenParaActualizarAsync(int id)
        {
            return await _context.Almacenes.FirstOrDefaultAsync(almacen => almacen.IdAlmacen == id);
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

        public async Task<List<int>> ListarUbicacionesDelAlmacenAsync(
            int idAlmacen, IReadOnlyCollection<int> idsUbicaciones)
        {
            if (idsUbicaciones.Count == 0)
                return [];

            return await _context.Ubicaciones.AsNoTracking()
                .Where(ubicacion => ubicacion.IdAlmacen == idAlmacen &&
                                    idsUbicaciones.Contains(ubicacion.IdUbicacion))
                .Select(ubicacion => ubicacion.IdUbicacion)
                .ToListAsync();
        }

        public Task EliminarUbicacionesDespachoAsync(int idAlmacen)
        {
            return _context.UbicacionesDespacho
                .Where(ubicacion => ubicacion.IdAlmacen == idAlmacen)
                .ExecuteDeleteAsync();
        }

        public async Task AgregarUbicacionesDespachoAsync(
            int idAlmacen, IReadOnlyCollection<int> idsUbicaciones)
        {
            if (idsUbicaciones.Count == 0)
                return;

            var nuevasUbicaciones = idsUbicaciones.Select(id => new UbicacionDespacho
            {
                IdAlmacen = idAlmacen,
                IdUbicacion = id
            }).ToList();

            await _context.UbicacionesDespacho.AddRangeAsync(nuevasUbicaciones);
            await _context.SaveChangesAsync();
        }
    }
}
