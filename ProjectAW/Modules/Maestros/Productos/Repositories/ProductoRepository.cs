using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Maestros.Productos.Entities;

namespace ProjectAW.Modules.Maestros.Productos.Repositories;

public class ProductoRepository:IProductoRepository
{
    private readonly AppDbContext _context;

    public ProductoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Producto>> ObtenerTodosAsync()
    {
        return await _context.Productos.AsNoTracking().ToListAsync();
    }

    public async Task<Producto?> ObtenerPorIdAsync(int id)
    {
        return await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p=>p.IdProducto==id);
    }

    public async Task<Producto?> ObtenerPorCodigoAsync(string codigo)
    {
        return await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p=>p.Codigo==codigo);
    }

    public async Task CrearAsync(Producto producto)
    {
        await _context.Productos.AddAsync(producto);
        await _context.SaveChangesAsync();
    }

    public async Task ActualizarAsync(Producto producto)
    {
        _context.Productos.Update(producto);
        await _context.SaveChangesAsync();
    }
}
