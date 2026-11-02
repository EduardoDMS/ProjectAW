using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Seguridad.Usuarios.Entitites;

namespace ProjectAW.Modules.Seguridad.Security.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Usuario?> ObtenerPorUsernameAsync(string username, CancellationToken ct = default)
        {
            return await _context.Usuarios.AsNoTracking()
                .Include(usuario => usuario.rol)
                .FirstOrDefaultAsync(usuario => usuario.Username == username, ct);
        }
    }
}
