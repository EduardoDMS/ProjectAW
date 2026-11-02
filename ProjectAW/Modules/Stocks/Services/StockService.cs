using ProjectAW.Modules.Stocks.DTOs.Responses;
using ProjectAW.Modules.Stocks.Entities;
using ProjectAW.Modules.Stocks.Repositories;

namespace ProjectAW.Modules.Stocks.Services;

public sealed class StockService : IStockService
{
    private readonly IStockRepository _repository;

    public StockService(IStockRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<StockAlmacenDto>> ListarAsync(
        int? idAlmacen, int? idProducto, string? codigoUbicacion, CancellationToken ct = default)
    {
        var stocks = await _repository.ListarAsync(idAlmacen, idProducto, codigoUbicacion, ct);
        return stocks.Select(ToDto).ToList();
    }

    public Task<IReadOnlyDictionary<int, long>> ObtenerDisponibilidadAsync(
        int idAlmacen, IReadOnlyCollection<int> idsProductos, CancellationToken ct = default)
    {
        return _repository.ObtenerDisponibilidadAsync(idAlmacen, idsProductos, ct);
    }

    private static StockAlmacenDto ToDto(StockUbicacion s) => new(
        s.IdStockUbicacion,
        s.IdProducto,
        s.Producto?.Codigo ?? string.Empty,
        s.Producto?.Descripcion ?? string.Empty,
        s.Ubicacion?.IdAlmacen ?? 0,
        s.Ubicacion?.IdUbicacion ?? 0,
        s.Ubicacion?.Almacen?.Codigo ?? string.Empty,
        s.Ubicacion?.Almacen?.Descripcion ?? string.Empty,
        s.Ubicacion?.Codigo ?? string.Empty,
        s.CantidadActual,
        s.Producto?.UnidadMedida?.Nombre ?? string.Empty,
        s.FechaUltimoRegistro);
}
