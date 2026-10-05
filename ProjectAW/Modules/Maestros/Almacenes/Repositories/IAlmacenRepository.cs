using ProjectAW.Modules.Maestros.Almacenes.Entities;

namespace ProjectAW.Modules.Maestros.Almacenes.Repositories
{
    public interface IAlmacenRepository
    {
        Task<List<Almacen>> ObtenerAlmacenesAsync();
        Task<Almacen?> ObtenerAlmacenPorIdAsync(int id);
        Task<Almacen?> ObtenerAlmacenPorCodigoAsync(string codigo);
        Task CrearAlmacenAsync(Almacen almacen);
        Task ActualizarAlmacenAsync(Almacen almacen);
    }
}
