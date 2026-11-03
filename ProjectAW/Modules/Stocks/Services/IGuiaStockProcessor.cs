using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Stocks.Entities;

namespace ProjectAW.Modules.Stocks.Services;

/// <summary>
/// Reglas de aplicación de inventario propias de cada tipo de operación.
/// El servicio coordina procesadores y la transacción; la implementación no confirma cambios.
/// </summary>
public interface IGuiaStockProcessor
{
    int IdTipoOperacionGuia { get; }
    bool RequiereUbicacionOrigen { get; }
    bool RequiereUbicacionDestino { get; }

    void ValidarGuia(CabeceraGuia guia);

    int ObtenerAlmacenMovimiento(CabeceraGuia guia);

    Task AplicarProductoAsync(
        CabeceraGuia guia,
        DetalleGuia detalle,
        IReadOnlyList<LecturaGuia> lecturas,
        CancellationToken ct = default);
}
