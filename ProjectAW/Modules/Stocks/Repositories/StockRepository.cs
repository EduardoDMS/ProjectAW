using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Stocks.Entities;

namespace ProjectAW.Modules.Stocks.Repositories;

public sealed class StockRepository : IStockRepository
{
    private readonly AppDbContext _context;

    public StockRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<StockUbicacion>> ListarAsync(
        int? idAlmacen = null, int? idProducto = null, CancellationToken ct = default)
    {
        if (!idAlmacen.HasValue)
            return Array.Empty<StockUbicacion>();

        var query = _context.StockUbicacion.AsNoTracking()
            .Include(s => s.Ubicacion)
            .Include(s => s.Producto)
                .ThenInclude(p => p!.UnidadMedida)
            .Where(s => s.Ubicacion != null && s.Ubicacion.Activo &&
                        s.Ubicacion.IdAlmacen == idAlmacen.Value &&
                        s.Producto != null && s.Producto.Activo)
            .AsQueryable();

        if (idProducto.HasValue) query = query.Where(s => s.IdProducto == idProducto.Value);

        return await query.OrderBy(s => s.Producto!.Codigo)
            .ThenBy(s => s.Ubicacion!.Codigo)
            .ToListAsync(ct);
    }
}
