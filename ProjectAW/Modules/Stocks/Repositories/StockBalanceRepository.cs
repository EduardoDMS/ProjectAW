using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Stocks.Entities;

namespace ProjectAW.Modules.Stocks.Repositories;

public sealed class StockBalanceRepository : IStockBalanceRepository
{
    private readonly AppDbContext _context;

    public StockBalanceRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<StockUbicacion>> ObtenerSaldosUbicacionAsync(
        int idUbicacion, int idProducto, CancellationToken ct = default)
    {
        return _context.StockUbicacion
            .Where(saldo => saldo.IdUbicacion == idUbicacion && saldo.IdProducto == idProducto)
            .ToListAsync(ct);
    }

    public async Task CrearSaldoUbicacionAsync(StockUbicacion saldo, CancellationToken ct = default)
    {
        await _context.StockUbicacion.AddAsync(saldo, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task ActualizarSaldoUbicacionAsync(StockUbicacion saldo, CancellationToken ct = default)
    {
        _context.StockUbicacion.Update(saldo);
        await _context.SaveChangesAsync(ct);
    }

    public Task<List<StockUbicacion>> ObtenerSaldosUbicacionAlmacenAsync(
        int idAlmacen, int idProducto, CancellationToken ct = default)
    {
        return _context.StockUbicacion
            .Include(saldo => saldo.Ubicacion)
            .Where(saldo => saldo.IdProducto == idProducto &&
                            saldo.Ubicacion!.IdAlmacen == idAlmacen)
            .ToListAsync(ct);
    }

    public Task<List<StockAlmacen>> ObtenerSaldosAlmacenAsync(
        int idAlmacen, int idProducto, CancellationToken ct = default)
    {
        return _context.StockAlmacen
            .Where(saldo => saldo.IdAlmacen == idAlmacen && saldo.IdProducto == idProducto)
            .ToListAsync(ct);
    }

    public async Task CrearSaldoAlmacenAsync(StockAlmacen saldo, CancellationToken ct = default)
    {
        await _context.StockAlmacen.AddAsync(saldo, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task ActualizarSaldoAlmacenAsync(StockAlmacen saldo, CancellationToken ct = default)
    {
        _context.StockAlmacen.Update(saldo);
        await _context.SaveChangesAsync(ct);
    }
}
