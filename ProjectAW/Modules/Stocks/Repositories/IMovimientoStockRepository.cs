using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Stocks.Entities;

namespace ProjectAW.Modules.Stocks.Repositories;

public interface IMovimientoStockRepository
{
    Task<CabeceraGuia?> ObtenerGuiaParaLecturaAsync(int idGuia, CancellationToken ct = default);
    Task<CabeceraGuia?> ObtenerGuiaOperadorAsync(int idGuia, int idOperador, CancellationToken ct = default);
    Task<IReadOnlyList<CabeceraGuia>> ListarGuiasOperadorAsync(int idOperador, int? idEstadoGuia, CancellationToken ct = default);
    Task<bool> EsOperadorAsync(int idUsuario, CancellationToken ct = default);
    Task<IReadOnlyList<LecturaGuia>> ListarLecturasAsync(int idGuia, int idOperador, CancellationToken ct = default);
    Task<string?> ObtenerCodigoProductoAsync(int idProducto, CancellationToken ct = default);
    Task<long> ObtenerCantidadStockAsync(int idUbicacion, int idProducto, CancellationToken ct = default);
    Task<long> ObtenerCantidadLeidaAsync(int idGuia, int idProducto, int idUbicacion, CancellationToken ct = default);
    Task<LecturaGuia?> ObtenerLecturaAsync(int idGuia, long idLecturaGuia, int idOperador, CancellationToken ct = default);
    void AgregarLectura(LecturaGuia lectura);
    void EliminarLectura(LecturaGuia lectura);
    Task<IReadOnlyList<LecturaGuia>> ObtenerLecturasParaFinalizarAsync(int idGuia, CancellationToken ct = default);
    Task<bool> TieneUbicacionesDespachoActivasAsync(int idAlmacen, CancellationToken ct = default);
    Task<IReadOnlySet<int>> ObtenerUbicacionesActivasAsync(
        int idAlmacen,
        IReadOnlyCollection<int> idsUbicaciones,
        bool soloDespacho,
        CancellationToken ct = default);
    void AgregarMovimiento(MovimientoStock movimiento);
    Task GuardarCambiosAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MovimientoStock>> ListarMovimientosAsync(int? idGuia, int? idAlmacen, int? idOperacion, CancellationToken ct = default);
}

