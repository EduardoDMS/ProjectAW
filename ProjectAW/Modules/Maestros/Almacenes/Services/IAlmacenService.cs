using ProjectAW.Modules.Maestros.Almacenes.DTOs.Requests;
using ProjectAW.Modules.Maestros.Almacenes.DTOs.Responses;

namespace ProjectAW.Modules.Maestros.Almacenes.Services
{
    public interface IAlmacenService
    {
        Task<List<AlmacenDTO>> ObtenerAlmacenesTodosAsync();
        Task<AlmacenDTO?> ObtenerAlmacenPorIdAsync(int id);
        Task<AlmacenDTO?> ObtenerAlmacenPorCodigoAsync(string codigo);
        Task<AlmacenDTO> CrearAlmacenAsync(CrearAlmacenDto dto);
        Task<bool> ActualizarAlmacenAsync(int id,ActualizarAlmacenDto dto);
        Task<bool> CambiarEstadoAlmacenAsync(int id, bool activo);


    }
}
