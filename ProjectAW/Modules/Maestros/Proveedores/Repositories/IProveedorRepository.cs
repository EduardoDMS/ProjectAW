using ProjectAW.Modules.Maestros.Proveedores.Entities;

namespace ProjectAW.Modules.Maestros.Proveedores.Repositories
{
    public interface IProveedorRepository
    {
        Task<List<Proveedor>> ObtenerTodosAsync();
        Task<Proveedor?> ObtenerPorIdAsync(int id);
        Task<Proveedor?> ObtenerPorNumeroDocumentoAsync(string numeroDocumento);
        Task CrearAsync(Proveedor proveedor);
        Task ActualizarAsync(Proveedor proveedor);
    }
}
