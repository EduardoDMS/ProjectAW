using ProjectAW.Modules.Stocks.DTOs.Requests;
using ProjectAW.Modules.Stocks.DTOs.Responses;

namespace ProjectAW.Modules.Stocks.Services;

public interface IMovimientoStockService
{
    Task TomarGuiaAsync(int idGuia, CancellationToken ct = default);
    Task FinalizarLecturaAsync(int idGuia, FinalizarLecturaRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<MovimientoStockDto>> ListarAsync(int? idGuia, int? idAlmacen, int? idOperacion, CancellationToken ct = default);
}
