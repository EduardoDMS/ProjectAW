using ProjectAW.Modules.Stocks.DTOs.Requests;
using ProjectAW.Modules.Stocks.DTOs.Responses;

namespace ProjectAW.Modules.Stocks.Services;

public interface IMovimientoStockService
{
    Task TomarGuiaAsync(int idGuia, CancellationToken ct = default);
    Task<IReadOnlyList<GuiaOperadorDto>> ListarMisGuiasAsync(int? idEstadoGuia, CancellationToken ct = default);
    Task<GuiaOperadorDto> ObtenerMiGuiaAsync(int idGuia, CancellationToken ct = default);
    Task<LecturaGuiaDto> RegistrarLecturaAsync(int idGuia, RegistrarLecturaRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<LecturaGuiaDto>> ListarLecturasAsync(int idGuia, CancellationToken ct = default);
    Task FinalizarLecturaAsync(int idGuia, CancellationToken ct = default);
    Task<IReadOnlyList<MovimientoStockDto>> ListarAsync(int? idGuia, int? idAlmacen, int? idOperacion, CancellationToken ct = default);
}
