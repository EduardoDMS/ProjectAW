using ProjectAW.Modules.Maestros.Ubicaciones.DTOs.Requests;
using ProjectAW.Modules.Maestros.Ubicaciones.DTOs.Responses;

namespace ProjectAW.Modules.Maestros.Ubicaciones.Services
{
    public interface IUbicacionService
    {
        Task<List<UbicacionDto>> ObtenerUbicacionesAsync();
        Task<UbicacionDto?> ObtenerUbicacionPorIdAsync(int id);
        Task<UbicacionDto?> ObtenerUbicacionPorCodigoAsync(string codigo);
        Task<UbicacionDto> CrearUbicacionAsync(CrearUbicacionDto dto);
        Task<bool> ActualizarAsync(int id, ActualizarUbicacionDto dto);
        Task<bool> CambiarEstadoAsync(int id, bool activo);

    }
}
