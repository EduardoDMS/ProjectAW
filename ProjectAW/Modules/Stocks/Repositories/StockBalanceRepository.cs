using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Modules.Stocks.Entities;

namespace ProjectAW.Modules.Stocks.Repositories;

/// <summary>
/// Operaciones comunes sobre saldos. Comparte el mismo AppDbContext scoped
/// para que sus cambios queden dentro de la transacción de finalización.
/// </summary>
public sealed class StockBalanceRepository : IStockBalanceRepository
{
    private readonly AppDbContext _context;

    public StockBalanceRepository(AppDbContext context) => _context = context;

    public async Task AjustarUbicacionAsync(
        int idUbicacion, int idProducto, int diferencia, CancellationToken ct = default)
    {
        if (diferencia == 0) return;

        var saldos = await _context.StockUbicacion
            .Where(s => s.IdUbicacion == idUbicacion && s.IdProducto == idProducto)
            .ToListAsync(ct);
        if (saldos.Count > 1)
            throw new ConflictoNegocioException("Hay saldos duplicados para el producto y la ubicación.");

        var saldo = saldos.SingleOrDefault();
        if (saldo is null)
        {
            if (diferencia < 0)
                throw new ConflictoNegocioException("No existe stock suficiente en la ubicación de origen.");

            _context.StockUbicacion.Add(new StockUbicacion
            {
                IdUbicacion = idUbicacion,
                IdProducto = idProducto,
                CantidadActual = diferencia,
                FechaUltimoRegistro = DateTime.UtcNow
            });
            return;
        }

        var nuevaCantidad = (long)saldo.CantidadActual + diferencia;
        if (nuevaCantidad < 0)
            throw new ConflictoNegocioException(
                $"Stock insuficiente del producto {idProducto} en la ubicación {idUbicacion}. Disponible: {saldo.CantidadActual}.");
        if (nuevaCantidad > int.MaxValue)
            throw new ConflictoNegocioException("La cantidad supera el máximo soportado por el inventario.");

        saldo.CantidadActual = (int)nuevaCantidad;
        saldo.FechaUltimoRegistro = DateTime.UtcNow;
    }

    public async Task SincronizarAlmacenAsync(int idAlmacen, int idProducto, CancellationToken ct = default)
    {
        var saldosGuardados = await _context.StockUbicacion
            .Include(s => s.Ubicacion)
            .Where(s => s.IdProducto == idProducto && s.Ubicacion!.IdAlmacen == idAlmacen)
            .ToListAsync(ct);

        var ubicacionesDeNuevosSaldos = new HashSet<int>();
        foreach (var saldoNuevo in _context.ChangeTracker.Entries<StockUbicacion>()
                     .Where(e => e.State == EntityState.Added && e.Entity.IdProducto == idProducto)
                     .Select(e => e.Entity)
                     .Where(e => saldosGuardados.All(s => s.IdStockUbicacion != e.IdStockUbicacion)))
        {
            if (await _context.Ubicaciones.AnyAsync(
                    u => u.IdUbicacion == saldoNuevo.IdUbicacion && u.IdAlmacen == idAlmacen, ct))
                ubicacionesDeNuevosSaldos.Add(saldoNuevo.IdUbicacion);
        }

        var total = saldosGuardados.Sum(s => (long)s.CantidadActual)
            + _context.ChangeTracker.Entries<StockUbicacion>()
                .Where(e => e.State == EntityState.Added && e.Entity.IdProducto == idProducto &&
                            ubicacionesDeNuevosSaldos.Contains(e.Entity.IdUbicacion))
                .Sum(e => (long)e.Entity.CantidadActual);

        if (total > int.MaxValue)
            throw new ConflictoNegocioException(
                $"El stock total del producto {idProducto} supera el máximo soportado en el almacén {idAlmacen}.");

        var stocksAlmacen = await _context.StockAlmacen
            .Where(s => s.IdAlmacen == idAlmacen && s.IdProducto == idProducto)
            .ToListAsync(ct);
        if (stocksAlmacen.Count > 1)
            throw new ConflictoNegocioException(
                "Hay más de un saldo para el mismo producto y almacén. Debe corregirse el stock duplicado antes de continuar.");

        var stockAlmacen = stocksAlmacen.SingleOrDefault();
        if (stockAlmacen is null && total > 0)
        {
            _context.StockAlmacen.Add(new StockAlmacen
            {
                IdAlmacen = idAlmacen,
                IdProducto = idProducto,
                CantidadActual = (int)total,
                FechaUltimoRegistro = DateTime.UtcNow
            });
        }
        else if (stockAlmacen is not null)
        {
            stockAlmacen.CantidadActual = (int)total;
            stockAlmacen.FechaUltimoRegistro = DateTime.UtcNow;
        }
    }
}
