using ProjectAW.Modules.Maestros.Clientes.Entities;

namespace ProjectAW.Modules.Maestros.Clientes.Repositories
{
    public interface IClienteRepository
    {
        Task<List<Cliente>> ObtenerTodosAsync();
        Task<Cliente?> ObtenerPorIdAsync(int id);
        Task<Cliente?> ObtenerPorNumeroDocumentoAsync(string numeroDocumento);
        Task CrearAsync(Cliente cliente);
        Task ActualizarAsync(Cliente cliente);
    }
}
