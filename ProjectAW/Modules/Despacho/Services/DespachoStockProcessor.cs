using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Stocks.Entities;
using ProjectAW.Modules.Stocks.Repositories;
using ProjectAW.Modules.Stocks.Services;

namespace ProjectAW.Modules.Despacho.Services;

/// <summary>Aplica salidas: descuenta las lecturas de las ubicaciones origen.</summary>
public sealed class DespachoStockProcessor : IGuiaStockProcessor
{
    private readonly IStockBalanceRepository _stock;

    public DespachoStockProcessor(IStockBalanceRepository stock) => _stock = stock;

    public int IdTipoOperacionGuia => TipoOperacionGuiaIds.Salida;
    public bool RequiereUbicacionOrigen => true;
    public bool RequiereUbicacionDestino => false;

    public void ValidarGuia(CabeceraGuia guia)
    {
        if (guia.IdAlmacenOrigen is null)
            throw new ReglaNegocioException("La guía no tiene los almacenes requeridos para su tipo de operación.");
    }

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
    }
}
