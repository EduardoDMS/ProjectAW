using ProjectAW.Infrastructure.Errors;
using ProjectAW.Modules.Stocks.Entities;
using ProjectAW.Modules.Stocks.Repositories;

namespace ProjectAW.Modules.Stocks.Services;

public sealed class StockBalanceService : IStockBalanceService
{
    private readonly IStockBalanceRepository _repository;

    public StockBalanceService(IStockBalanceRepository repository)
    {
        _repository = repository;
    }

    public async Task AjustarUbicacionAsync(
        int idUbicacion, int idProducto, int diferencia, CancellationToken ct = default)
    {
        if (diferencia == 0)
            return;

        var saldos = await _repository.ObtenerSaldosUbicacionAsync(idUbicacion, idProducto, ct);
        if (saldos.Count > 1)
            throw new ConflictoNegocioException("Hay saldos duplicados para el producto y la ubicación.");

        var saldo = saldos.SingleOrDefault();
        if (saldo is null)
        {
            await CrearSaldoInicialAsync(idUbicacion, idProducto, diferencia, ct);
            return;
        }

        AplicarDiferencia(saldo, idUbicacion, idProducto, diferencia);
        await _repository.ActualizarSaldoUbicacionAsync(saldo, ct);
    }

    public async Task SincronizarAlmacenAsync(
        int idAlmacen, int idProducto, CancellationToken ct = default)
    {
        var saldosUbicacion = await _repository.ObtenerSaldosUbicacionAlmacenAsync(
            idAlmacen, idProducto, ct);
        var total = saldosUbicacion.Sum(saldo => (long)saldo.CantidadActual);
        if (total > int.MaxValue)
            throw new ConflictoNegocioException(
                $"El stock total del producto {idProducto} supera el máximo soportado en el almacén {idAlmacen}.");

        await GuardarSaldoAlmacenAsync(idAlmacen, idProducto, (int)total, ct);
    }

    private async Task CrearSaldoInicialAsync(
        int idUbicacion, int idProducto, int diferencia, CancellationToken ct)
    {
        if (diferencia < 0)
            throw new ConflictoNegocioException("No existe stock suficiente en la ubicación de origen.");

        var saldo = new StockUbicacion
        {
            IdUbicacion = idUbicacion,
            IdProducto = idProducto,
            CantidadActual = diferencia,
            FechaUltimoRegistro = DateTime.UtcNow
        };

        await _repository.CrearSaldoUbicacionAsync(saldo, ct);
    }

    private async Task GuardarSaldoAlmacenAsync(
        int idAlmacen, int idProducto, int total, CancellationToken ct)
    {
        var saldos = await _repository.ObtenerSaldosAlmacenAsync(idAlmacen, idProducto, ct);
        if (saldos.Count > 1)
            throw new ConflictoNegocioException(
                "Hay más de un saldo para el mismo producto y almacén. Debe corregirse el stock duplicado antes de continuar.");

        var saldo = saldos.SingleOrDefault();
        if (saldo is null)
        {
            if (total > 0)
                await CrearSaldoAlmacenAsync(idAlmacen, idProducto, total, ct);
            return;
        }

        saldo.CantidadActual = total;
        saldo.FechaUltimoRegistro = DateTime.UtcNow;
        await _repository.ActualizarSaldoAlmacenAsync(saldo, ct);
    }

    private Task CrearSaldoAlmacenAsync(
        int idAlmacen, int idProducto, int cantidad, CancellationToken ct)
    {
        var saldo = new StockAlmacen
        {
            IdAlmacen = idAlmacen,
            IdProducto = idProducto,
            CantidadActual = cantidad,
            FechaUltimoRegistro = DateTime.UtcNow
        };

        return _repository.CrearSaldoAlmacenAsync(saldo, ct);
    }

    private static void AplicarDiferencia(
        StockUbicacion saldo, int idUbicacion, int idProducto, int diferencia)
    {
        var nuevaCantidad = (long)saldo.CantidadActual + diferencia;
        if (nuevaCantidad < 0)
            throw new ConflictoNegocioException(
                $"Stock insuficiente del producto {idProducto} en la ubicación {idUbicacion}. Disponible: {saldo.CantidadActual}.");
        if (nuevaCantidad > int.MaxValue)
            throw new ConflictoNegocioException("La cantidad supera el máximo soportado por el inventario.");

        saldo.CantidadActual = (int)nuevaCantidad;
        saldo.FechaUltimoRegistro = DateTime.UtcNow;
    }
}
