using ProjectAW.Modules.Maestros.Ubicaciones.Entities;

namespace ProjectAW.Modules.Maestros.Ubicaciones.Repositories
{
    public interface IUbicacionRepository
    {
        Task<List<Ubicacion>> ListarUbicacionesAsync();
        Task<Ubicacion?> ObtenerUbicacionPorIdAsync(int id);
        Task<Ubicacion?> ObtenerUbicacionPorCodigoAsync(string codigo);
        Task CrearUbicacionAsync(Ubicacion ubicacion);
        Task ActualizarUbicacionAsync(Ubicacion ubicacion);

    }
}
