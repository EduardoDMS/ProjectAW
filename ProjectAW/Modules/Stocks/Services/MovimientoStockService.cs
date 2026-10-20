using ProjectAW.Infrastructure.Errors;
using ProjectAW.Models;
using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Seguridad.Security.HttpAuth;
using ProjectAW.Modules.Stocks.DTOs.Requests;
using ProjectAW.Modules.Stocks.DTOs.Responses;
using ProjectAW.Modules.Guias.DTOs.Response;
using ProjectAW.Modules.Stocks.Repositories;

namespace ProjectAW.Modules.Stocks.Services;

public sealed class MovimientoStockService : IMovimientoStockService
{
    private readonly IMovimientoStockRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IReadOnlyDictionary<int, IGuiaStockProcessor> _stockProcessors;

    public MovimientoStockService(
        IMovimientoStockRepository repository,
        ICurrentUserService currentUser,
        IEnumerable<IGuiaStockProcessor> stockProcessors)
    {
        _repository = repository;
        _currentUser = currentUser;
        _stockProcessors = stockProcessors.ToDictionary(processor => processor.IdTipoOperacionGuia);
    }

    public async Task TomarGuiaAsync(int idGuia, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);
        await _repository.TomarGuiaAsync(idGuia, _currentUser.IdUsuario, ct);
    }

    public async Task<IReadOnlyList<GuiaOperadorDto>> ListarMisGuiasAsync(
        int? idEstadoGuia, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);
        if (idEstadoGuia.HasValue && idEstadoGuia.Value is not (EstadoGuiasIds.EnProceso or EstadoGuiasIds.Atendida))
            throw new ReglaNegocioException("Solo puedes filtrar tus guías por En proceso o Atendida.");

        var guias = await _repository.ListarGuiasOperadorAsync(_currentUser.IdUsuario, idEstadoGuia, ct);
        return guias.Select(ToGuiaOperadorDto).ToList();
    }

    public async Task<GuiaOperadorDto> ObtenerMiGuiaAsync(int idGuia, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);
        var guia = await _repository.ObtenerGuiaOperadorAsync(idGuia, _currentUser.IdUsuario, ct)
            ?? throw new NoEncontradoException("La guía no existe en tus guías en proceso o terminadas.");
        return ToGuiaOperadorDto(guia);
    }

    public async Task<LecturaGuiaDto> RegistrarLecturaAsync(int idGuia, RegistrarLecturaRequest request, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);
        var lectura = await _repository.RegistrarLecturaAsync(idGuia, _currentUser.IdUsuario, request, ct);
        return ToLecturaDto(lectura);
    }

    public async Task<IReadOnlyList<LecturaGuiaDto>> ListarLecturasAsync(int idGuia, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);
        var lecturas = await _repository.ListarLecturasAsync(idGuia, _currentUser.IdUsuario, ct);
        return lecturas.Select(ToLecturaDto).ToList();
    }

    public async Task<bool> EliminarLecturaAsync(int idGuia, long idLecturaGuia, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);
        return await _repository.EliminarLecturaAsync(idGuia, idLecturaGuia, _currentUser.IdUsuario, ct);
    }

    public async Task FinalizarLecturaAsync(int idGuia, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);
        var guia = await ObtenerGuiaAsync(idGuia, ct);

        if (guia.Detalles.Count == 0 || guia.Detalles.GroupBy(d => d.IdProducto).Any(g => g.Count() > 1))
            throw new ReglaNegocioException("La guía no tiene productos válidos para procesar.");

        ObtenerProcesador(guia.IdTipoOperacionGuia).ValidarGuia(guia);

        if (guia.Detalles.Any(d => d.CantidadEsperada != decimal.Truncate(d.CantidadEsperada) || d.CantidadEsperada > int.MaxValue))
            throw new ReglaNegocioException("Esta guía contiene cantidades fraccionarias, pero el stock actual registra unidades enteras. No se aplicó ningún movimiento.");
        await _repository.FinalizarLecturaAsync(idGuia, _currentUser.IdUsuario, ct);
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
            m.Guia?.Detalles.FirstOrDefault(d => d.IdProducto == m.IdProducto)?.CantidadEsperada ?? 0,
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

    private IGuiaStockProcessor ObtenerProcesador(int idTipoOperacionGuia) =>
        _stockProcessors.TryGetValue(idTipoOperacionGuia, out var processor)
            ? processor
            : throw new ReglaNegocioException("La guía tiene un tipo de operación inválido.");

    private async Task<Modules.Guias.Entities.CabeceraGuia> ObtenerGuiaAsync(int idGuia, CancellationToken ct) =>
        await _repository.ObtenerGuiaParaLecturaAsync(idGuia, ct)
        ?? throw new NoEncontradoException($"La guía {idGuia} no existe.");

    private static GuiaOperadorDto ToGuiaOperadorDto(Modules.Guias.Entities.CabeceraGuia guia) => new(
        guia.IdGuia,
        guia.NumeroGuia,
        guia.IdTipoOperacionGuia,
        guia.IdEstadoGuia,
        guia.IdProveedor,
        guia.IdCliente,
        guia.IdAlmacenOrigen,
        guia.IdAlmacenDestino,
        guia.FechaProgramada,
        guia.FechaRegistro,
        guia.FechaActualizada,
        guia.TipoDocumentoReferencia,
        guia.NumeroDocumentoReferencia,
        guia.Observacion,
        guia.Detalles.Select(d => new DetalleGuiaResponse(d.IdGuiaDetalle, d.IdProducto, d.CantidadEsperada)).ToList());

    private static LecturaGuiaDto ToLecturaDto(Modules.Stocks.Entities.LecturaGuia l) => new(
        l.IdLecturaGuia, l.IdGuia, l.IdProducto, l.Producto?.Codigo ?? l.CodigoLeido,
        l.Producto?.Descripcion ?? string.Empty, l.CodigoLeido, l.Cantidad, l.Lote, l.Serie,
        l.Observacion, l.IdUbicacionOrigen, l.UbicacionOrigen?.Codigo, l.IdUbicacionDestino,
        l.UbicacionDestino?.Codigo, l.FechaRegistroUtc);
}
