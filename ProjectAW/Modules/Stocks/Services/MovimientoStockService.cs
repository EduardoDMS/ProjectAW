using ProjectAW.Infrastructure.Errors;
using ProjectAW.Models;
using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Seguridad.Security.HttpAuth;
using ProjectAW.Modules.Stocks.DTOs.Requests;
using ProjectAW.Modules.Stocks.DTOs.Responses;
using ProjectAW.Modules.Stocks.Repositories;

namespace ProjectAW.Modules.Stocks.Services;

public sealed class MovimientoStockService : IMovimientoStockService
{
    private readonly IMovimientoStockRepository _repository;
    private readonly ICurrentUserService _currentUser;

    public MovimientoStockService(IMovimientoStockRepository repository, ICurrentUserService currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task TomarGuiaAsync(int idGuia, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);
        await _repository.TomarGuiaAsync(idGuia, _currentUser.IdUsuario, ct);
    }

    public async Task FinalizarLecturaAsync(
        int idGuia, FinalizarLecturaRequest request, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);
        var guia = await ObtenerGuiaAsync(idGuia, ct);

        if (guia.Detalles.Count == 0 || guia.Detalles.GroupBy(d => d.IdProducto).Any(g => g.Count() > 1))
            throw new ReglaNegocioException("La guía no tiene productos válidos para procesar.");

        if (guia.IdTipoOperacionGuia is not (TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Salida or TipoOperacionGuiaIds.Traslado))
            throw new ReglaNegocioException("La guía tiene un tipo de operación inválido.");

        var tieneAlmacenes = guia.IdTipoOperacionGuia switch
        {
            TipoOperacionGuiaIds.Entrada => guia.IdAlmacenDestino.HasValue,
            TipoOperacionGuiaIds.Salida => guia.IdAlmacenOrigen.HasValue,
            TipoOperacionGuiaIds.Traslado => guia.IdAlmacenOrigen.HasValue &&
                guia.IdAlmacenDestino.HasValue && guia.IdAlmacenOrigen != guia.IdAlmacenDestino,
            _ => false
        };
        if (!tieneAlmacenes)
            throw new ReglaNegocioException("La guía no tiene los almacenes requeridos para su tipo de operación.");

        if (request.Lecturas is null || request.Lecturas.Count == 0)
            throw new ReglaNegocioException("Debes enviar la lectura de todos los productos de la guía.");

        if (request.Lecturas.Any(l => l.CantidadLeida < 0))
            throw new ReglaNegocioException("La cantidad leída no puede ser negativa.");

        if (request.Lecturas.GroupBy(l => l.IdProducto).Any(g => g.Count() > 1))
            throw new ReglaNegocioException("No repitas productos en la lectura; envía una sola cantidad por producto.");

        var productosEsperados = guia.Detalles.Select(d => d.IdProducto).ToHashSet();
        var productosLeidos = request.Lecturas.Select(l => l.IdProducto).ToHashSet();

        if (!productosEsperados.SetEquals(productosLeidos))
            throw new ReglaNegocioException("La lectura debe incluir una sola cantidad para cada producto de la guía, sin agregar productos ajenos.");

        if (guia.Detalles.Any(d => d.CantidadEsperada != decimal.Truncate(d.CantidadEsperada) || d.CantidadEsperada > int.MaxValue))
            throw new ReglaNegocioException("Esta guía contiene cantidades fraccionarias, pero el stock actual registra unidades enteras. No se aplicó ningún movimiento.");

        var lecturas = request.Lecturas
            .Select(l => new LecturaProducto(
                l.IdProducto,
                l.CantidadLeida,
                l.Observacion?.Trim(),
                l.IdUbicacionOrigen is > 0 ? l.IdUbicacionOrigen : null,
                l.IdUbicacionDestino is > 0 ? l.IdUbicacionDestino : null))
            .ToList();

        ValidarUbicaciones(guia.IdTipoOperacionGuia, lecturas);

        await _repository.FinalizarLecturaAsync(guia, lecturas, _currentUser.IdUsuario, ct);
    }

    public async Task<IReadOnlyList<MovimientoStockDto>> ListarAsync(
        int? idGuia, int? idAlmacen, int? idOperacion, CancellationToken ct = default)
    {
        var movimientos = await _repository.ListarMovimientosAsync(idGuia, idAlmacen, idOperacion, ct);
        return movimientos.Select(m => new MovimientoStockDto(
            m.IdMovimientoStock,
            m.IdGuia,
            m.Guia?.NumeroGuia ?? string.Empty,
            m.IdTipoOperacion,
            m.OperacionGuias?.Nombre ?? string.Empty,
            m.IdProducto,
            m.Producto?.Codigo ?? string.Empty,
            m.Producto?.Descripcion ?? string.Empty,
            m.IdAlmacen,
            m.IdTipoOperacion == TipoOperacionGuiaIds.Entrada ? m.Guia?.Proveedor?.RazonSocial ?? "Proveedor" : m.Almacen?.Descripcion ?? "Almacén",
            m.IdTipoOperacion == TipoOperacionGuiaIds.Salida ? m.Guia?.Cliente?.RazonSocial ?? "Cliente" : m.IdTipoOperacion == TipoOperacionGuiaIds.Entrada ? m.Guia?.AlmacenDestino?.Descripcion ?? "Almacén" : m.Guia?.AlmacenDestino?.Descripcion ?? "Almacén",
            m.Cantidad,
            m.CantidadDiferenciada,
            m.Observacion,
            m.IdUsuarioOperador,
            m.Usuario is null ? string.Empty : $"{m.Usuario.Nombre} {m.Usuario.Apellido}".Trim(),
            m.IdUbicacionOrigen,
            m.UbicacionOrigen?.Codigo ?? string.Empty,
            m.IdUbicacionDestino,
            m.UbicacionDestino?.Codigo ?? string.Empty,
            m.FchMovimiento)).ToList();
    }

    private async Task ExigirOperadorAsync(CancellationToken ct)
    {
        if (!await _repository.EsOperadorAsync(_currentUser.IdUsuario, ct))
            throw new ReglaNegocioException("Solo un usuario activo con rol Operador puede realizar lecturas de guías.");
    }

    private static void ValidarUbicaciones(int tipoOperacion, IReadOnlyCollection<LecturaProducto> lecturas)
    {
        var requiereOrigen = tipoOperacion is TipoOperacionGuiaIds.Salida or TipoOperacionGuiaIds.Traslado;
        var requiereDestino = tipoOperacion is TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Traslado;

        foreach (var lectura in lecturas)
        {
            if (lectura.IdUbicacionOrigen.HasValue != requiereOrigen || lectura.IdUbicacionDestino.HasValue != requiereDestino)
            {
                var ubicacionesEsperadas = tipoOperacion switch
                {
                    TipoOperacionGuiaIds.Entrada => "solo la ubicación destino",
                    TipoOperacionGuiaIds.Salida => "solo la ubicación origen",
                    TipoOperacionGuiaIds.Traslado => "la ubicación origen y la ubicación destino",
                    _ => "las ubicaciones correspondientes a la operación"
                };
                throw new ReglaNegocioException($"La operación de la guía requiere {ubicacionesEsperadas} para el producto {lectura.IdProducto}. Omite las ubicaciones no aplicables o envíalas como 0.");
            }
        }
    }

    private async Task<Modules.Guias.Entities.CabeceraGuia> ObtenerGuiaAsync(int idGuia, CancellationToken ct) =>
        await _repository.ObtenerGuiaParaLecturaAsync(idGuia, ct)
        ?? throw new NoEncontradoException($"La guía {idGuia} no existe.");
}
