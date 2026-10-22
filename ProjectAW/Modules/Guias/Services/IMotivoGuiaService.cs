using ProjectAW.Modules.Guias.DTOs.Request;
using ProjectAW.Modules.Guias.DTOs.Response;

namespace ProjectAW.Modules.Guias.Services;

public interface IMotivoGuiaService
{
    Task<IReadOnlyList<MotivoGuiaResponse>> ListarAsync(int? operacion, bool soloActivos, CancellationToken ct = default);
    Task<MotivoGuiaResponse> ObtenerAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<TipoComportamientoGuiaResponse>> ListarComportamientosAsync(CancellationToken ct = default);
    Task<MotivoGuiaResponse> CrearAsync(GuardarMotivoGuiaRequest request, CancellationToken ct = default);
    Task<MotivoGuiaResponse> ActualizarAsync(int id, GuardarMotivoGuiaRequest request, CancellationToken ct = default);
    Task CambiarEstadoAsync(int id, bool activo, CancellationToken ct = default);
    Task EliminarAsync(int id, CancellationToken ct = default);
}
