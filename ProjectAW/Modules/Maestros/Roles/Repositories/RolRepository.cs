using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Maestros.Roles.Entities;

namespace ProjectAW.Modules.Maestros.Roles.Repositories
{
    public class RolRepository : IRolRepository
    {

        private readonly AppDbContext _context;

        public RolRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task ActualizarAsync(Rol rol)
        {
            _context.Roles.Update(rol);
            await _context.SaveChangesAsync();
        }

        public async Task CrearAsync(Rol rol)
        {
            await _context.Roles.AddAsync(rol);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Rol>> GetAllAsync()
        {
            return await _context.Roles.AsNoTracking().ToListAsync();
        }

        public async Task<Rol?> GetByIdAsync(int id)
        {
            return await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.IdRol == id);
        }

        public async Task<Rol?> ObtenerPorNombreAsync(string username)
        {
            return await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Nombre == username);
        }
    }   
}
