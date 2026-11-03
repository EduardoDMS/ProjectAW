using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Stocks.Entities;
using ProjectAW.Modules.Stocks.Services;

namespace ProjectAW.Modules.Recepcion.Services;

/// <summary>Aplica entradas: suma las lecturas a las ubicaciones destino.</summary>
public sealed class RecepcionStockProcessor : IGuiaStockProcessor
{
    private readonly IStockBalanceService _stock;

    public RecepcionStockProcessor(IStockBalanceService stock) => _stock = stock;

    public int IdTipoOperacionGuia => TipoOperacionGuiaIds.Entrada;
    public bool RequiereUbicacionOrigen => false;
    public bool RequiereUbicacionDestino => true;

    public void ValidarGuia(CabeceraGuia guia)
    {
        if (guia.IdAlmacenDestino is null)
            throw new ReglaNegocioException("La guía no tiene los almacenes requeridos para su tipo de operación.");
    }

    public int ObtenerAlmacenMovimiento(CabeceraGuia guia) => guia.IdAlmacenDestino!.Value;

    /// <summary>Suma las lecturas en sus ubicaciones destino y sincroniza el almacén.</summary>
    public async Task AplicarProductoAsync(
        CabeceraGuia guia, DetalleGuia detalle, IReadOnlyList<LecturaGuia> lecturas, CancellationToken ct = default)
    {
        await SumarLecturasPorUbicacionDestinoAsync(detalle.IdProducto, lecturas, ct);
        await _stock.SincronizarAlmacenAsync(guia.IdAlmacenDestino!.Value, detalle.IdProducto, ct);
    }

    /// <summary>Aplica una suma por cada ubicación destino distinta.</summary>
    private async Task SumarLecturasPorUbicacionDestinoAsync(
        int idProducto, IReadOnlyList<LecturaGuia> lecturas, CancellationToken ct)
    {
        var lecturasPorUbicacion = lecturas
            .GroupBy(lectura => lectura.IdUbicacionDestino!.Value);

        foreach (var lecturasDeUbicacion in lecturasPorUbicacion)
        {
            var totalLeido = lecturasDeUbicacion.Sum(lectura => (long)lectura.Cantidad);
            var cantidadLeida = checked((int)totalLeido);
            await _stock.AjustarUbicacionAsync(
                lecturasDeUbicacion.Key, idProducto, cantidadLeida, ct);
        }
    }
}
