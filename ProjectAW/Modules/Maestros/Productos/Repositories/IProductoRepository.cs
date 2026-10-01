using ProjectAW.Modules.Maestros.Productos.Entities;

namespace ProjectAW.Modules.Maestros.Productos.Repositories;

public interface IProductoRepository
{
    Task<List<Producto>> ObtenerTodosAsync();
    Task<Producto?> ObtenerPorIdAsync(int id);
    Task<Producto?> ObtenerPorCodigoAsync(string codigo);
    Task CrearAsync(Producto producto);
    Task ActualizarAsync(Producto producto);
}
