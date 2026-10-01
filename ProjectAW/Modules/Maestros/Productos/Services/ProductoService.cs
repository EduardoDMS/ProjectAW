using ProjectAW.Modules.Maestros.Productos.DTOs.Requests;
using ProjectAW.Modules.Maestros.Productos.DTOs.Responses;
using ProjectAW.Modules.Maestros.Productos.Entities;
using ProjectAW.Modules.Maestros.Productos.Repositories;

namespace ProjectAW.Modules.Maestros.Productos.Services;

public class ProductoService:IProductoService
{
    private readonly IProductoRepository _repository;

    public ProductoService(IProductoRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ProductoDto>> ObtenerTodosAsync()
    {
        var productos=await _repository.ObtenerTodosAsync();

        return productos.Select(p=>new ProductoDto
        {
            IdProducto=p.IdProducto,
            Codigo=p.Codigo,
            Descripcion=p.Descripcion,
            Activo=p.Activo,
        }).ToList();
    }

    public async Task<ProductoDto?> ObtenerPorIdAsync(int id)
    {
        var producto = await _repository.ObtenerPorIdAsync(id);

        if (producto == null)
            return null;

        return new ProductoDto
        {
            IdProducto = producto.IdProducto,
            Codigo = producto.Codigo,
            Descripcion = producto.Descripcion,
            Activo = producto.Activo,
        };
    }

    public async Task<ProductoDto?> ObtenerPorCodigoAsync(string codigo)
    {
        var producto = await _repository.ObtenerPorCodigoAsync(codigo);

        if (producto == null)
            return null;

        return new ProductoDto
        {
            IdProducto = producto.IdProducto,
            Codigo = producto.Codigo,
            Descripcion = producto.Descripcion,
            Activo = producto.Activo,
        };
    }

    public async Task<ProductoDto> CrearAsync(CrearProductoDto dto)
    {
        var codigo = dto.Codigo.Trim();

        var productoExistente = await _repository.ObtenerPorCodigoAsync(codigo);

        if (productoExistente != null)
            throw new InvalidOperationException("Ya existe un producto con ese código.");

        var producto = new Producto
        {
            Codigo = codigo,
            Descripcion = dto.Descripcion?.Trim(),
            Activo = true
        };

        await _repository.CrearAsync(producto);

        return new ProductoDto
        {
            IdProducto = producto.IdProducto,
            Codigo = producto.Codigo,
            Descripcion = dto.Descripcion,
            Activo = producto.Activo
        };
    }

    public async Task<bool> ActualizarAsync(int id, ActualizarProductoDto dto)
    {
        var producto = await _repository.ObtenerPorIdAsync(id);

        if(producto == null)
            return false;

        var codigo=dto.Codigo.Trim();

        var productoConMismoCodigo = await _repository.ObtenerPorCodigoAsync(codigo);

        if(productoConMismoCodigo!=null && productoConMismoCodigo.IdProducto!=id)
        {
            throw new InvalidOperationException("Ya existe otro producto con ese código.");
        }

        producto.Codigo = codigo;
        producto.Descripcion=dto.Descripcion?.Trim();
        producto.Activo = dto.Activo;

        await _repository.ActualizarAsync(producto);

        return true;
    }

    public async Task<bool> CambiarEstadoAsync(int id, bool activo)
    {
        var producto = await _repository.ObtenerPorIdAsync(id);

        if (producto == null)
            return false;

        producto.Activo = activo;

        await _repository.ActualizarAsync(producto);

        return true;
    }
}
