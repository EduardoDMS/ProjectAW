using ProjectAW.Modules.Maestros.Productos.DTOs;
using ProjectAW.Modules.Maestros.Productos.DTOs.Requests;
using ProjectAW.Modules.Maestros.Productos.DTOs.Responses;

namespace ProjectAW.Modules.Maestros.Productos.Services;

public interface IProductoService
{
    Task<List<ProductoDto>> ObtenerTodosAsync();
    Task<ProductoDto?> ObtenerPorIdAsync(int id);
    Task<ProductoDto?> ObtenerPorCodigoAsync(string codigo);
    Task<ProductoDto> CrearAsync(CrearProductoDto dto);
    Task<bool> ActualizarAsync(int id, ActualizarProductoDto dto);
    Task<bool> CambiarEstadoAsync(int id, bool activo);
}
