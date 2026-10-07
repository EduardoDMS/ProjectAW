using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Maestros.Clientes.Entities;

namespace ProjectAW.Modules.Maestros.Clientes.Repositories
{
    public class ClienteRepository:IClienteRepository
    {
        private readonly AppDbContext _context;

        public ClienteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Cliente>> ObtenerTodosAsync()
        {
            return await _context.Clientes.AsNoTracking().ToListAsync();
        }

        public async Task<Cliente?> ObtenerPorIdAsync(int id)
        {
            return await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c=>c.IdCliente==id);
        }

        public async Task<Cliente?> ObtenerPorNumeroDocumentoAsync(string numeroDocumento)
        {
            return await _context.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.NumeroDocumento == numeroDocumento);
        }

        public async Task CrearAsync(Cliente cliente)
        {
            await _context.Clientes.AddAsync(cliente);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarAsync(Cliente cliente)
        {
            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();
        }
    }
}
