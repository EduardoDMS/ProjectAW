using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Maestros.Ubicaciones.Entities;

namespace ProjectAW.Modules.Maestros.Ubicaciones.Repositories
{
    public class UbicacionRepository : IUbicacionRepository
    {
        private readonly AppDbContext _context;
        public UbicacionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task ActualizarUbicacionAsync(Ubicacion ubicacion)
        {
            _context.Ubicaciones.Update(ubicacion);
            await _context.SaveChangesAsync();
        }

        public async Task CrearUbicacionAsync(Ubicacion ubicacion)
        {
            await _context.Ubicaciones.AddAsync(ubicacion);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Ubicacion>> ListarUbicacionesAsync()
        {
            return await _context.Ubicaciones.ToListAsync();
        }

        public async Task<Ubicacion?> ObtenerUbicacionPorCodigoAsync(string codigo)
        {
            return await _context.Ubicaciones.AsNoTracking().FirstOrDefaultAsync(u => u.Codigo == codigo);
        }

        public async Task<Ubicacion?> ObtenerUbicacionPorIdAsync(int id)
        {
            return await _context.Ubicaciones.AsNoTracking().FirstOrDefaultAsync(u => u.IdUbicacion == id);
        }
    }
}
