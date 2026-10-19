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

        public Task<Usuario?> ObtenerPorUsernameAsync(string username, CancellationToken ct = default) =>
        _context.Usuarios.AsNoTracking().Include(u => u.rol).FirstOrDefaultAsync(u => u.Username == username, ct);
    }
}
