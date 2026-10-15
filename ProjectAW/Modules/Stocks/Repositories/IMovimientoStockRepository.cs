using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Stocks.Entities;

namespace ProjectAW.Modules.Stocks.Repositories;

public interface IMovimientoStockRepository
{
    Task<CabeceraGuia?> ObtenerGuiaParaLecturaAsync(int idGuia, CancellationToken ct = default);
    Task<bool> EsOperadorAsync(int idUsuario, CancellationToken ct = default);
    Task TomarGuiaAsync(int idGuia, int idOperador, CancellationToken ct = default);
    Task FinalizarLecturaAsync(
        CabeceraGuia guia,
        IReadOnlyCollection<LecturaProducto> lecturas,
        int idOperador,
        CancellationToken ct = default);
    Task<IReadOnlyList<MovimientoStock>> ListarMovimientosAsync(int? idGuia, int? idAlmacen, int? idOperacion, CancellationToken ct = default);
}

public sealed record LecturaProducto(int IdProducto, int CantidadLeida, string? Observacion, int? IdUbicacionOrigen, int? IdUbicacionDestino);
