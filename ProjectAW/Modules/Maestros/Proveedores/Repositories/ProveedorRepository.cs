using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Maestros.Proveedores.Entities;

namespace ProjectAW.Modules.Maestros.Proveedores.Repositories
{
    public class ProveedorRepository : IProveedorRepository
    {
        private readonly AppDbContext _context;

        public ProveedorRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Proveedor>> ObtenerTodosAsync()
        {
            return await _context.Proveedores.AsNoTracking().ToListAsync();
        }

        public async Task<Proveedor?> ObtenerPorIdAsync(int id)
        {
            return await _context.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.IdProveedor == id);
        }

        public async Task<Proveedor?> ObtenerPorNumeroDocumentoAsync(string numeroDocumento)
        {
            return await _context.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.NumeroDocumento == numeroDocumento);
        }

        public async Task CrearAsync(Proveedor proveedor)
        {
            await _context.Proveedores.AddAsync(proveedor);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarAsync(Proveedor proveedor)
        {
            _context.Proveedores.Update(proveedor);
            await _context.SaveChangesAsync();
        } 
    }
}
