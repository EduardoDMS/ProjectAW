using ProjectAW.Modules.Maestros.Roles.DTOs.Requests;
using ProjectAW.Modules.Maestros.Roles.DTOs.Response;

namespace ProjectAW.Modules.Maestros.Roles.Services
{
    public interface IRolService
    {
        Task<List<RolDto>> ObtenerTodosRolesAsync();
        Task<RolDto?> ObtenerRolPorIdAsync(int id);
        Task<RolDto?> ObtenerRolPorNombreAsync(string nombre);
        Task<RolDto> CrearRolAsync(CrearRolDto dto);
        Task<bool> ActualizarRolAsync(int id, ActualizarRolDto dto);
    }
}
