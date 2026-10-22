using Microsoft.EntityFrameworkCore;
using System.Data;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Seguridad.Usuarios.Entitites;
using ProjectAW.Modules.Stocks.Entities;
using ProjectAW.Modules.Stocks.DTOs.Requests;
using ProjectAW.Modules.Stocks.Services;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Models;

namespace ProjectAW.Modules.Stocks.Repositories;

public sealed class MovimientoStockRepository : IMovimientoStockRepository
{
    private readonly AppDbContext _context;
    private readonly IReadOnlyDictionary<int, IGuiaStockProcessor> _stockProcessors;

    public MovimientoStockRepository(AppDbContext context, IEnumerable<IGuiaStockProcessor> stockProcessors)
    {
        _context = context;
        _stockProcessors = stockProcessors.ToDictionary(processor => processor.IdTipoOperacionGuia);
    }

    public Task<CabeceraGuia?> ObtenerGuiaParaLecturaAsync(int idGuia, CancellationToken ct = default) =>
        _context.CabeceraGuia
            .Include(g => g.Detalles)
            .Include(g => g.Proveedor).Include(g => g.Cliente)
            .Include(g => g.AlmacenOrigen).Include(g => g.AlmacenDestino)
            .Include(g => g.Motivo).ThenInclude(m => m!.TipoComportamiento)
            .Include(g => g.GuiaRelacionada)
            .FirstOrDefaultAsync(g => g.IdGuia == idGuia, ct);

    public Task<CabeceraGuia?> ObtenerGuiaOperadorAsync(int idGuia, int idOperador, CancellationToken ct = default) =>
        _context.CabeceraGuia.AsNoTracking()
            .Include(g => g.Detalles)
            .Include(g => g.Proveedor).Include(g => g.Cliente)
            .Include(g => g.AlmacenOrigen).Include(g => g.AlmacenDestino)
            .Include(g => g.Motivo).ThenInclude(m => m!.TipoComportamiento)
            .Include(g => g.GuiaRelacionada)
            .FirstOrDefaultAsync(g => g.IdGuia == idGuia &&
                (g.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada || g.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida) &&
                g.IdUsuarioModificacion == idOperador &&
                (g.IdEstadoGuia == EstadoGuiasIds.EnProceso || g.IdEstadoGuia == EstadoGuiasIds.Atendida), ct);

    public async Task<IReadOnlyList<CabeceraGuia>> ListarGuiasOperadorAsync(
        int idOperador, int? idEstadoGuia, CancellationToken ct = default)
    {
        var query = _context.CabeceraGuia.AsNoTracking()
            .Include(g => g.Detalles)
            .Include(g => g.Proveedor).Include(g => g.Cliente)
            .Include(g => g.AlmacenOrigen).Include(g => g.AlmacenDestino)
            .Include(g => g.Motivo).ThenInclude(m => m!.TipoComportamiento)
            .Include(g => g.GuiaRelacionada)
            .Where(g => g.IdUsuarioModificacion == idOperador &&
                (g.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada || g.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida) &&
                (g.IdEstadoGuia == EstadoGuiasIds.EnProceso || g.IdEstadoGuia == EstadoGuiasIds.Atendida));

        if (idEstadoGuia.HasValue)
            query = query.Where(g => g.IdEstadoGuia == idEstadoGuia.Value);

        return await query.OrderByDescending(g => g.FechaActualizada ?? g.FechaProgramada).ToListAsync(ct);
    }

    public Task<bool> EsOperadorAsync(int idUsuario, CancellationToken ct = default) =>
        _context.Usuarios
            .AnyAsync(u => u.IdUsuario == idUsuario && u.Activo && u.rol != null &&
                u.rol.Nombre.Trim().ToUpper() == "OPERADOR", ct);

    public async Task TomarGuiaAsync(int idGuia, int idOperador, CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var actual = await ObtenerGuiaParaLecturaAsync(idGuia, ct)
            ?? throw new NoEncontradoException($"La guía {idGuia} no existe.");

        if (actual.IdTipoOperacionGuia is not (TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Salida))
            throw new ReglaNegocioException("El operador solo puede atender guías de Entrada o Salida. Los movimientos internos se generan mediante guías relacionadas.");

        if (actual.IdEstadoGuia != EstadoGuiasIds.Pendiente)
            throw new ConflictoNegocioException("La guía ya no está pendiente y no puede ser tomada.");
        actual.IdEstadoGuia = EstadoGuiasIds.EnProceso;
        actual.IdUsuarioModificacion = idOperador; // En proceso identifica al operador que tomó la guía.
        actual.FechaActualizada = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<LecturaGuia> RegistrarLecturaAsync(int idGuia, int idOperador, RegistrarLecturaRequest request, CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var guia = await ObtenerGuiaParaLecturaAsync(idGuia, ct)
            ?? throw new NoEncontradoException($"La guía {idGuia} no existe.");
        ValidarGuiaEnProceso(guia, idOperador);
        var detalle = guia.Detalles.FirstOrDefault(d => d.IdProducto == request.IdProducto)
            ?? throw new ReglaNegocioException("El producto leído no pertenece a esta guía.");
        var producto = await _context.Productos.AsNoTracking().FirstOrDefaultAsync(p => p.IdProducto == detalle.IdProducto, ct)
            ?? throw new NoEncontradoException("El producto de la guía ya no existe.");
        if (!string.Equals(producto.Codigo.Trim(), request.CodigoLeido.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ReglaNegocioException("El código leído no corresponde al producto seleccionado.");
        await ValidarUbicacionesAsync(guia, request.IdUbicacionOrigen, request.IdUbicacionDestino, ct);
        if (guia.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida)
        {
            var stockActual = await _context.StockUbicacion.AsNoTracking()
                .Where(s => s.IdUbicacion == request.IdUbicacionOrigen && s.IdProducto == request.IdProducto)
                .Select(s => (long?)s.CantidadActual).FirstOrDefaultAsync(ct) ?? 0;
            var yaRegistrado = await _context.LecturasGuia.AsNoTracking()
                .Where(l => l.IdGuia == idGuia && l.IdProducto == request.IdProducto &&
                            l.IdUbicacionOrigen == request.IdUbicacionOrigen)
                .SumAsync(l => (long?)l.Cantidad, ct) ?? 0;
            if (yaRegistrado + request.Cantidad > stockActual)
                throw new ReglaNegocioException($"La ubicación de origen solo tiene {Math.Max(stockActual - yaRegistrado, 0)} unidades disponibles de este producto.");
        }

        var lectura = new LecturaGuia
        {
            IdGuia = idGuia,
            IdProducto = request.IdProducto,
            IdUsuarioOperador = idOperador,
            Cantidad = request.Cantidad,
            CodigoLeido = request.CodigoLeido.Trim(),
            Lote = Normalizar(request.Lote),
            Serie = Normalizar(request.Serie),
            Observacion = Normalizar(request.Observacion),
            IdUbicacionOrigen = request.IdUbicacionOrigen is > 0 ? request.IdUbicacionOrigen : null,
            IdUbicacionDestino = request.IdUbicacionDestino is > 0 ? request.IdUbicacionDestino : null,
            FechaRegistroUtc = DateTime.UtcNow
        };
        _context.LecturasGuia.Add(lectura);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return lectura;
    }

    public async Task<IReadOnlyList<LecturaGuia>> ListarLecturasAsync(int idGuia, int idOperador, CancellationToken ct = default)
    {
        var guia = await _context.CabeceraGuia.AsNoTracking()
            .FirstOrDefaultAsync(g => g.IdGuia == idGuia &&
                (g.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada || g.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida) &&
                g.IdUsuarioModificacion == idOperador, ct)
            ?? throw new NoEncontradoException("La guía no existe dentro de tus guías asignadas.");
        return await _context.LecturasGuia.AsNoTracking()
            .Include(x => x.Producto).Include(x => x.UbicacionOrigen).Include(x => x.UbicacionDestino)
            .Where(x => x.IdGuia == guia.IdGuia)
            .OrderByDescending(x => x.FechaRegistroUtc).ThenByDescending(x => x.IdLecturaGuia).ToListAsync(ct);
    }

    public async Task<bool> EliminarLecturaAsync(
        int idGuia, long idLecturaGuia, int idOperador, CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var guia = await ObtenerGuiaParaLecturaAsync(idGuia, ct)
            ?? throw new NoEncontradoException($"La guía {idGuia} no existe.");
        ValidarGuiaEnProceso(guia, idOperador);

        var lectura = await _context.LecturasGuia.FirstOrDefaultAsync(
            x => x.IdLecturaGuia == idLecturaGuia && x.IdGuia == idGuia && x.IdUsuarioOperador == idOperador, ct);
        if (lectura is null) return false;

        _context.LecturasGuia.Remove(lectura);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    public async Task FinalizarLecturaAsync(
        int idGuia,
        int idOperador,
        CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var actual = await ObtenerGuiaParaLecturaAsync(idGuia, ct)
            ?? throw new NoEncontradoException($"La guía {idGuia} no existe.");
        ValidarGuiaEnProceso(actual, idOperador);
        if (actual.Detalles.Count == 0 || actual.Detalles.Any(d => d.CantidadEsperada != decimal.Truncate(d.CantidadEsperada) || d.CantidadEsperada > int.MaxValue))
            throw new ReglaNegocioException("La guía contiene cantidades inválidas o fraccionarias para el stock actual.");

        var lecturas = await _context.LecturasGuia.Where(x => x.IdGuia == idGuia).OrderBy(x => x.IdLecturaGuia).ToListAsync(ct);
        var productosLeidos = lecturas.Select(x => x.IdProducto).ToHashSet();
        if (!actual.Detalles.Select(x => x.IdProducto).ToHashSet().SetEquals(productosLeidos))
            throw new ReglaNegocioException("Registra al menos una lectura para cada producto de la guía antes de finalizar.");
        var processor = ObtenerProcesador(actual.IdTipoOperacionGuia);
        var salida = actual.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida;
        var requiereUbicacionDespacho = salida && await _context.Ubicaciones.AsNoTracking()
            .AnyAsync(u => u.IdAlmacen == actual.IdAlmacenOrigen && u.Activo && _context.UbicacionesDespacho.Any(d => d.IdAlmacen == u.IdAlmacen && d.IdUbicacion == u.IdUbicacion), ct);

        foreach (var detalle in actual.Detalles)
        {
            var lineas = lecturas.Where(x => x.IdProducto == detalle.IdProducto).ToList();
            long totalLeido = lineas.Sum(x => (long)x.Cantidad);
            if (totalLeido > int.MaxValue) throw new ConflictoNegocioException("La suma leída supera el máximo permitido por el inventario.");
            foreach (var linea in lineas)
            {
                if (processor.RequiereUbicacionOrigen != linea.IdUbicacionOrigen.HasValue ||
                    (salida ? requiereUbicacionDespacho != linea.IdUbicacionDestino.HasValue : processor.RequiereUbicacionDestino != linea.IdUbicacionDestino.HasValue))
                    throw new ReglaNegocioException($"La lectura del producto {detalle.IdProducto} no tiene las ubicaciones requeridas.");
                if (processor.RequiereUbicacionOrigen && !await _context.Ubicaciones.AnyAsync(
                        u => u.IdUbicacion == linea.IdUbicacionOrigen && u.IdAlmacen == actual.IdAlmacenOrigen && u.Activo, ct))
                    throw new ReglaNegocioException($"La ubicación de origen del producto {detalle.IdProducto} dejó de estar activa o no pertenece al almacén.");
                if (salida && requiereUbicacionDespacho && !await _context.Ubicaciones.AnyAsync(
                        u => u.IdUbicacion == linea.IdUbicacionDestino && u.IdAlmacen == actual.IdAlmacenOrigen && u.Activo && _context.UbicacionesDespacho.Any(d => d.IdAlmacen == u.IdAlmacen && d.IdUbicacion == u.IdUbicacion), ct))
                    throw new ReglaNegocioException($"La ubicación elegida para el despacho del producto {detalle.IdProducto} no está habilitada para despacho en el almacén de origen.");
                if (!salida && processor.RequiereUbicacionDestino && !await _context.Ubicaciones.AnyAsync(
                        u => u.IdUbicacion == linea.IdUbicacionDestino && u.IdAlmacen == actual.IdAlmacenDestino && u.Activo, ct))
                    throw new ReglaNegocioException($"La ubicación destino del producto {detalle.IdProducto} dejó de estar activa o no pertenece al almacén asignado.");
            }

            await processor.AplicarProductoAsync(actual, detalle, lineas, ct);
            var cantidadEsperada = checked((int)detalle.CantidadEsperada);

            _context.MovimientoStock.Add(new MovimientoStock
            {
                IdProducto = detalle.IdProducto,
                IdAlmacen = processor.ObtenerAlmacenMovimiento(actual),
                IdTipoOperacion = processor.IdTipoOperacionGuia,
                IdGuia = actual.IdGuia,
                IdUsuarioOperador = idOperador,
                Cantidad = (int)totalLeido,
                CantidadDiferenciada = checked((int)totalLeido - cantidadEsperada),
                Observacion = lineas.Select(x => x.Observacion).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty,
                FchMovimiento = DateTime.UtcNow,
                IdUbicacionOrigen = lineas.Select(x => x.IdUbicacionOrigen).Distinct().Count() == 1 ? lineas[0].IdUbicacionOrigen : null,
                IdUbicacionDestino = lineas.Select(x => x.IdUbicacionDestino).Distinct().Count() == 1 ? lineas[0].IdUbicacionDestino : null
            });
        }

        actual.IdEstadoGuia = EstadoGuiasIds.Atendida;
        actual.IdUsuarioModificacion = idOperador;
        actual.FechaActualizada = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private static void ValidarGuiaEnProceso(CabeceraGuia guia, int idOperador)
    {
        if (guia.IdTipoOperacionGuia is not (TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Salida))
            throw new ReglaNegocioException("El operador solo puede atender guías de Entrada o Salida. Los movimientos internos se generan mediante guías relacionadas.");
        if (guia.IdEstadoGuia != EstadoGuiasIds.EnProceso)
            throw new ConflictoNegocioException("La guía no está en proceso.");
        if (guia.IdUsuarioModificacion != idOperador)
            throw new ConflictoNegocioException("La guía está asignada a otro operador.");
    }

    private async Task ValidarUbicacionesAsync(CabeceraGuia guia, int? origen, int? destino, CancellationToken ct)
    {
        var processor = ObtenerProcesador(guia.IdTipoOperacionGuia);
        var salida = guia.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida;
        var requiereUbicacionDespacho = salida && await _context.Ubicaciones.AsNoTracking()
            .AnyAsync(u => u.IdAlmacen == guia.IdAlmacenOrigen && u.Activo && _context.UbicacionesDespacho.Any(d => d.IdAlmacen == u.IdAlmacen && d.IdUbicacion == u.IdUbicacion), ct);
        if (processor.RequiereUbicacionOrigen != (origen is > 0) ||
            (salida ? requiereUbicacionDespacho != (destino is > 0) : processor.RequiereUbicacionDestino != (destino is > 0)))
            throw new ReglaNegocioException("Indica las ubicaciones correspondientes al tipo de operación de esta guía.");
        if (processor.RequiereUbicacionOrigen && !await _context.Ubicaciones.AnyAsync(
                u => u.IdUbicacion == origen && u.IdAlmacen == guia.IdAlmacenOrigen && u.Activo, ct))
            throw new ReglaNegocioException("La ubicación de origen no está activa o no pertenece al almacén de origen.");
        if (salida && requiereUbicacionDespacho && !await _context.Ubicaciones.AnyAsync(
                u => u.IdUbicacion == destino && u.IdAlmacen == guia.IdAlmacenOrigen && u.Activo && _context.UbicacionesDespacho.Any(d => d.IdAlmacen == u.IdAlmacen && d.IdUbicacion == u.IdUbicacion), ct))
            throw new ReglaNegocioException("La ubicación elegida no está habilitada para despacho en el almacén de origen.");
        if (!salida && processor.RequiereUbicacionDestino && !await _context.Ubicaciones.AnyAsync(
                u => u.IdUbicacion == destino && u.IdAlmacen == guia.IdAlmacenDestino && u.Activo, ct))
            throw new ReglaNegocioException("La ubicación de destino no está activa o no pertenece al almacén de destino.");
    }

    private IGuiaStockProcessor ObtenerProcesador(int idTipoOperacionGuia) =>
        _stockProcessors.TryGetValue(idTipoOperacionGuia, out var processor)
            ? processor
            : throw new ReglaNegocioException("La guía tiene un tipo de operación no compatible con el inventario.");

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    public async Task<IReadOnlyList<MovimientoStock>> ListarMovimientosAsync(
        int? idGuia, int? idAlmacen, int? idOperacion, CancellationToken ct = default)
    {
        var query = _context.MovimientoStock.AsNoTracking()
            .Include(m => m.Producto)
            .Include(m => m.Almacen)
            .Include(m => m.Guia)
                .ThenInclude(g => g!.Proveedor)
            .Include(m => m.Guia)
                .ThenInclude(g => g!.Detalles)
            .Include(m => m.Guia)
                .ThenInclude(g => g!.Motivo)
            .Include(m => m.Guia).ThenInclude(g => g!.Cliente)
            .Include(m => m.Guia).ThenInclude(g => g!.AlmacenOrigen)
            .Include(m => m.Guia).ThenInclude(g => g!.AlmacenDestino)
            .Include(m => m.OperacionGuias)
            .Include(m => m.UbicacionOrigen)
            .Include(m => m.UbicacionDestino)
            .Include(m => m.Usuario)
            .AsQueryable();

        if (idGuia.HasValue) query = query.Where(m => m.IdGuia == idGuia.Value);
        if (idAlmacen.HasValue) query = query.Where(m => m.IdAlmacen == idAlmacen.Value || m.Guia!.IdAlmacenOrigen == idAlmacen.Value || m.Guia.IdAlmacenDestino == idAlmacen.Value);
        if (idOperacion.HasValue) query = query.Where(m => m.IdTipoOperacion == idOperacion.Value);

        return await query.OrderByDescending(m => m.FchMovimiento).ToListAsync(ct);
    }

}
