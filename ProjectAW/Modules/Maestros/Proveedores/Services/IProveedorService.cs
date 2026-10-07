using ProjectAW.Modules.Maestros.Proveedores.DTOs.Requests;
using ProjectAW.Modules.Maestros.Proveedores.DTOs.Responses;

namespace ProjectAW.Modules.Maestros.Proveedores.Services
{
    public interface IProveedorService
    {
        Task<List<ProveedorDto>> ObtenerTodosAsync();
        Task<ProveedorDto?> ObtenerPorIdAsync(int id);
        Task<ProveedorDto?> ObtenerPorNumeroDocumento(string numeroDocumento);
        Task<ProveedorDto> CrearAsync(CrearProveedorDto dto);
        Task<bool> ActualizarAsync(int id, ActualizarProveedorDto dto);
        Task<bool> CambiarEstadoAsync(int id, bool activo);
    }
}
