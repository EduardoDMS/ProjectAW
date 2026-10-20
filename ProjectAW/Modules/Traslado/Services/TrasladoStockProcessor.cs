using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Stocks.Entities;
using ProjectAW.Modules.Stocks.Repositories;
using ProjectAW.Modules.Stocks.Services;

namespace ProjectAW.Modules.Traslado.Services;

/// <summary>Aplica traslados: descuenta el origen y suma el destino.</summary>
public sealed class TrasladoStockProcessor : IGuiaStockProcessor
{
    private readonly IStockBalanceRepository _stock;

    public TrasladoStockProcessor(IStockBalanceRepository stock) => _stock = stock;

    public int IdTipoOperacionGuia => TipoOperacionGuiaIds.Traslado;
    public bool RequiereUbicacionOrigen => true;
    public bool RequiereUbicacionDestino => true;

    public void ValidarGuia(CabeceraGuia guia)
    {
        if (guia.IdAlmacenOrigen is null || guia.IdAlmacenDestino is null ||
            guia.IdAlmacenOrigen == guia.IdAlmacenDestino)
            throw new ReglaNegocioException("La guía no tiene los almacenes requeridos para su tipo de operación.");
    }

    // El movimiento histórico sigue asociado al almacén de origen, igual que antes.
    public int ObtenerAlmacenMovimiento(CabeceraGuia guia) => guia.IdAlmacenOrigen!.Value;

    public async Task AplicarProductoAsync(
        CabeceraGuia guia, DetalleGuia detalle, IReadOnlyList<LecturaGuia> lecturas, CancellationToken ct = default)
    {
        foreach (var grupo in lecturas.GroupBy(x => x.IdUbicacionOrigen!.Value))
        {
            var cantidad = checked((int)grupo.Sum(x => (long)x.Cantidad));
            await _stock.AjustarUbicacionAsync(grupo.Key, detalle.IdProducto, -cantidad, ct);
        }
        await _stock.SincronizarAlmacenAsync(guia.IdAlmacenOrigen!.Value, detalle.IdProducto, ct);

        foreach (var grupo in lecturas.GroupBy(x => x.IdUbicacionDestino!.Value))
        {
            var cantidad = checked((int)grupo.Sum(x => (long)x.Cantidad));
            await _stock.AjustarUbicacionAsync(grupo.Key, detalle.IdProducto, cantidad, ct);
        }
        await _stock.SincronizarAlmacenAsync(guia.IdAlmacenDestino!.Value, detalle.IdProducto, ct);
    }
}
