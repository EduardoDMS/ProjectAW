using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Stocks.Entities;
using ProjectAW.Modules.Stocks.Repositories;
using ProjectAW.Modules.Stocks.Services;

namespace ProjectAW.Modules.Recepcion.Services;

/// <summary>Aplica entradas: suma las lecturas a las ubicaciones destino.</summary>
public sealed class RecepcionStockProcessor : IGuiaStockProcessor
{
    private readonly IStockBalanceRepository _stock;

    public RecepcionStockProcessor(IStockBalanceRepository stock) => _stock = stock;

    public int IdTipoOperacionGuia => TipoOperacionGuiaIds.Entrada;
    public bool RequiereUbicacionOrigen => false;
    public bool RequiereUbicacionDestino => true;

    public void ValidarGuia(CabeceraGuia guia)
    {
        if (guia.IdAlmacenDestino is null)
            throw new ReglaNegocioException("La guía no tiene los almacenes requeridos para su tipo de operación.");
    }

    public int ObtenerAlmacenMovimiento(CabeceraGuia guia) => guia.IdAlmacenDestino!.Value;

    public async Task AplicarProductoAsync(
        CabeceraGuia guia, DetalleGuia detalle, IReadOnlyList<LecturaGuia> lecturas, CancellationToken ct = default)
    {
        foreach (var grupo in lecturas.GroupBy(x => x.IdUbicacionDestino!.Value))
        {
            var cantidad = checked((int)grupo.Sum(x => (long)x.Cantidad));
            await _stock.AjustarUbicacionAsync(grupo.Key, detalle.IdProducto, cantidad, ct);
        }

        await _stock.SincronizarAlmacenAsync(guia.IdAlmacenDestino!.Value, detalle.IdProducto, ct);
    }
}
