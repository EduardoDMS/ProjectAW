using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Maestros.Almacenes.Entities;

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
            _context.Almacenes.Update(almacen);
            return _context.SaveChangesAsync();
        }

        public async Task CrearAlmacenAsync(Almacen almacen)
        {
            await _context.Almacenes.AddAsync(almacen);
            await _context.SaveChangesAsync();
        }

        public async Task<Almacen?> ObtenerAlmacenPorCodigoAsync(string codigo)
        {
            return await _context.Almacenes.AsNoTracking().FirstOrDefaultAsync(a => a.Codigo == codigo);
        }

        public async Task<Almacen?> ObtenerAlmacenPorIdAsync(int id)
        {
            return await _context.Almacenes.AsNoTracking().FirstOrDefaultAsync(a => a.IdAlmacen == id);
        }

        public async Task<List<Almacen>> ObtenerAlmacenesAsync()
        {
            return await _context.Almacenes.AsNoTracking().ToListAsync();        
        }
    }
}
