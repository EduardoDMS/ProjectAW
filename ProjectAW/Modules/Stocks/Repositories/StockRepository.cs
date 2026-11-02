using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Stocks.Entities;

namespace ProjectAW.Modules.Stocks.Repositories;

public sealed class StockRepository : IStockRepository
{
    private readonly AppDbContext _context;

    public StockRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<StockUbicacion>> ListarAsync(
        int? idAlmacen = null, int? idProducto = null, string? codigoUbicacion = null, CancellationToken ct = default)
    {
        // Evita devolver todo el inventario si la consulta llega sin criterios.
        if (!idAlmacen.HasValue && !idProducto.HasValue && string.IsNullOrWhiteSpace(codigoUbicacion))
            return Array.Empty<StockUbicacion>();

        var query = _context.StockUbicacion.AsNoTracking()
                .Include(s => s.Ubicacion)
                .ThenInclude(u => u!.Almacen)
                .Include(s => s.Producto)
                .ThenInclude(p => p!.UnidadMedida)
                .AsQueryable();

        // Solo expone stock de productos y ubicaciones vigentes; aplica después los filtros recibidos.
        query = query.Where(stock => stock.Ubicacion != null && stock.Ubicacion.Activo &&
            stock.Producto != null && stock.Producto.Activo);
        if (idAlmacen.HasValue)
            query = query.Where(stock => stock.Ubicacion!.IdAlmacen == idAlmacen.Value);
        if (idProducto.HasValue) query = query.Where(s => s.IdProducto == idProducto.Value);
        if (!string.IsNullOrWhiteSpace(codigoUbicacion))
        {
            var codigo = codigoUbicacion.Trim();
            query = query.Where(s => s.Ubicacion!.Codigo == codigo);
        }

        return await query.OrderBy(s => s.Producto!.Codigo)
            .ThenBy(s => s.Ubicacion!.Codigo)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<int, long>> ObtenerDisponibilidadAsync(
        int idAlmacen, IReadOnlyCollection<int> idsProductos, CancellationToken ct = default)
    {
        if (idsProductos.Count == 0)
            return new Dictionary<int, long>();

        return await _context.StockUbicacion.AsNoTracking()
            .Where(s => s.Ubicacion != null && s.Ubicacion.Activo &&
                        s.Ubicacion.IdAlmacen == idAlmacen && idsProductos.Contains(s.IdProducto))
            .GroupBy(s => s.IdProducto)
            .Select(g => new { IdProducto = g.Key, Cantidad = g.Sum(s => (long)s.CantidadActual) })
            .ToDictionaryAsync(x => x.IdProducto, x => x.Cantidad, ct);
    }
}
