namespace ProjectAW.Modules.Stocks.Repositories;

/// <summary>
/// Actualiza saldos por ubicación y mantiene el saldo agregado del almacén.
/// No confirma cambios: la transacción pertenece al flujo que finaliza la guía.
/// </summary>
public interface IStockBalanceRepository
{
    Task AjustarUbicacionAsync(int idUbicacion, int idProducto, int diferencia, CancellationToken ct = default);
    Task SincronizarAlmacenAsync(int idAlmacen, int idProducto, CancellationToken ct = default);
}
