using ProjectAW.Modules.Seguridad.Usuarios.Entitites;

namespace ProjectAW.Modules.Seguridad.Security.Repositories
{
    public interface IAuthRepository
    {
        Task<Usuario?> ObtenerPorUsernameAsync(string username, CancellationToken ct = default);
    }
}
