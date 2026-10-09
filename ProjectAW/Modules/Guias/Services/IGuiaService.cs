using ProjectAW.Modules.Guias.DTOs.Request;
using ProjectAW.Modules.Guias.DTOs.Response;
using ProjectAW.Modules.Guias.Entities;

namespace ProjectAW.Modules.Guias.Services
{
    public interface IGuiaService
    {
        Task<GuiaResponse> CrearAsync(CrearGuiaRequest request, CancellationToken ct = default);
        Task<GuiaResponse> ObtenerPorIdAsync(int idGuia, CancellationToken ct = default);
        Task<IReadOnlyList<GuiaResponse>> ListarAsync(int? idTipoOperacionGuia, int? idEstadoGuia, CancellationToken ct = default);
        Task<GuiaResponse> ActualizarAsync(int idGuia, ActualizarGuiaRequest request, CancellationToken ct = default);
        Task CancelarAsync(int idGuia, CancellationToken ct = default);

    }
}
