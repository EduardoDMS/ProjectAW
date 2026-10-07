using ProjectAW.Modules.Maestros.Clientes.DTOs.Requests;
using ProjectAW.Modules.Maestros.Clientes.DTOs.Responses;

namespace ProjectAW.Modules.Maestros.Clientes.Services
{
    public interface IClienteService
    {
        Task<List<ClienteDto>> ObtenerTodosAsync();
        Task<ClienteDto?> ObtenerPorIdAsync(int id);
        Task<ClienteDto?> ObtenerPorNumeroDocumento(string numeroDocumento);
        Task<ClienteDto> CrearAsync(CrearClienteDto dto);
        Task<bool> ActualizarAsync(int id, ActualizarClienteDto dto);
        Task<bool> CambiarEstadoAsync(int id, bool activo);
    }
}
