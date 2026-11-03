using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Stocks.Entities;
using ProjectAW.Modules.Stocks.Services;

namespace ProjectAW.Modules.Despacho.Services;

/// <summary>
/// Aplica salidas: descuenta las lecturas de las ubicaciones origen.
/// La clase sealed se usa para que no se pueda heredar de esta clase, ya que es un servicio de dominio y no debería ser extendido.
/// </summary>
public sealed class DespachoStockProcessor : IGuiaStockProcessor
{
    private readonly IStockBalanceService _stock;

    public DespachoStockProcessor(IStockBalanceService stock) => _stock = stock;

    public int IdTipoOperacionGuia => TipoOperacionGuiaIds.Salida;
    public bool RequiereUbicacionOrigen => true;
    public bool RequiereUbicacionDestino => false;

    public void ValidarGuia(CabeceraGuia guia)
    {
        if (guia.IdAlmacenOrigen is null)
            throw new ReglaNegocioException("La guía no tiene los almacenes requeridos para su tipo de operación.");
    }

    public int ObtenerAlmacenMovimiento(CabeceraGuia guia) => guia.IdAlmacenOrigen!.Value;

    /// <summary>Descuenta las lecturas de origen y actualiza el saldo total del almacén.</summary>
    public async Task AplicarProductoAsync(
        CabeceraGuia guia, DetalleGuia detalle, IReadOnlyList<LecturaGuia> lecturas, CancellationToken ct = default)
    {
        await DescontarLecturasPorUbicacionAsync(detalle.IdProducto, lecturas, ct);
        await _stock.SincronizarAlmacenAsync(guia.IdAlmacenOrigen!.Value, detalle.IdProducto, ct);
    }

    /// <summary>
    ///     Agrupa las lecturas por ubicación y aplica un solo descuento por cada una.
    ///     IReadOnlyList se usa para indicar que la lista no se modificará, lo que ayuda a evitar errores y mejora la claridad del código.
    /// </summary>
    private async Task DescontarLecturasPorUbicacionAsync(
        int idProducto, IReadOnlyList<LecturaGuia> lecturas, CancellationToken ct)
    {
        var lecturasPorUbicacion = lecturas.GroupBy(lectura => lectura.IdUbicacionOrigen!.Value);

        foreach (var lecturasDeUbicacion in lecturasPorUbicacion)
        {
            var totalLeido = lecturasDeUbicacion.Sum(lectura => (long)lectura.Cantidad);
            var cantidadLeida = checked((int)totalLeido);
            await _stock.AjustarUbicacionAsync(
                lecturasDeUbicacion.Key, idProducto, -cantidadLeida, ct);
        }
    }
}
