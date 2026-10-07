using ProjectAW.Modules.Maestros.Usuarios.Entitites;

namespace ProjectAW.Modules.Maestros.Usuarios.Repositories
{
    public interface IUsuarioRepository
    {
        Task<List<Usuario>> GetAllAsync();
        Task<Usuario?> GetByIdAsync(int id);
        Task<Usuario?> ObtenerPorUsernameAsync(string username);
        Task CrearAsync(Usuario usuario);
        Task ActualizarAsync(Usuario usuario);
    }
}
