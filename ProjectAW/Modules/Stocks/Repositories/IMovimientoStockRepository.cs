using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Stocks.DTOs.Requests;
using ProjectAW.Modules.Stocks.Entities;

namespace ProjectAW.Modules.Stocks.Repositories;

public interface IMovimientoStockRepository
{
    Task<CabeceraGuia?> ObtenerGuiaParaLecturaAsync(int idGuia, CancellationToken ct = default);
    Task<CabeceraGuia?> ObtenerGuiaOperadorAsync(int idGuia, int idOperador, CancellationToken ct = default);
    Task<IReadOnlyList<CabeceraGuia>> ListarGuiasOperadorAsync(int idOperador, int? idEstadoGuia, CancellationToken ct = default);
    Task<bool> EsOperadorAsync(int idUsuario, CancellationToken ct = default);
    Task TomarGuiaAsync(int idGuia, int idOperador, CancellationToken ct = default);
    Task<LecturaGuia> RegistrarLecturaAsync(int idGuia, int idOperador, RegistrarLecturaRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<LecturaGuia>> ListarLecturasAsync(int idGuia, int idOperador, CancellationToken ct = default);
    Task FinalizarLecturaAsync(
        int idGuia,
        int idOperador,
        CancellationToken ct = default);
    Task<IReadOnlyList<MovimientoStock>> ListarMovimientosAsync(int? idGuia, int? idAlmacen, int? idOperacion, CancellationToken ct = default);
}

