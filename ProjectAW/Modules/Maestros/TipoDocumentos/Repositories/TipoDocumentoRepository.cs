using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Maestros.TipoDocumentos.Entities;

namespace ProjectAW.Modules.Maestros.TipoDocumentos.Repositories
{
    public class TipoDocumentoRepository : ITipoDocumentoRepository
    {
        private readonly AppDbContext _context;

        public TipoDocumentoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task ActualizarAsync(TipoDocumento dc)
        {
            _context.TipoDocument.Update(dc);
            await _context.SaveChangesAsync();
        }

        public async Task CrearAsync(TipoDocumento dc)
        {
            await _context.TipoDocument.AddAsync(dc);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TipoDocumento>> GetAllAsync()
        {
            return await _context.TipoDocument.AsNoTracking().ToListAsync();
        }

        public async Task<TipoDocumento?> GetByIdAsync(int id)
        {
            return await _context.TipoDocument.AsNoTracking().FirstOrDefaultAsync(t => t.IdTipoDocumento == id);
        }
    }
}
