using Microsoft.EntityFrameworkCore;
using System.Data;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Seguridad.Usuarios.Entitites;
using ProjectAW.Modules.Stocks.Entities;
using ProjectAW.Modules.Stocks.DTOs.Requests;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Models;

namespace ProjectAW.Modules.Stocks.Repositories;

public sealed class MovimientoStockRepository : IMovimientoStockRepository
{
    private readonly AppDbContext _context;

    public MovimientoStockRepository(AppDbContext context) => _context = context;

    public Task<CabeceraGuia?> ObtenerGuiaParaLecturaAsync(int idGuia, CancellationToken ct = default) =>
        _context.CabeceraGuia
            .Include(g => g.Detalles)
            .Include(g => g.Proveedor).Include(g => g.Cliente)
            .Include(g => g.AlmacenOrigen).Include(g => g.AlmacenDestino)
            .FirstOrDefaultAsync(g => g.IdGuia == idGuia, ct);

    public Task<CabeceraGuia?> ObtenerGuiaOperadorAsync(int idGuia, int idOperador, CancellationToken ct = default) =>
        _context.CabeceraGuia.AsNoTracking()
            .Include(g => g.Detalles)
            .FirstOrDefaultAsync(g => g.IdGuia == idGuia &&
                g.IdUsuarioModificacion == idOperador &&
                (g.IdEstadoGuia == EstadoGuiasIds.EnProceso || g.IdEstadoGuia == EstadoGuiasIds.Atendida), ct);

    public async Task<IReadOnlyList<CabeceraGuia>> ListarGuiasOperadorAsync(
        int idOperador, int? idEstadoGuia, CancellationToken ct = default)
    {
        var query = _context.CabeceraGuia.AsNoTracking()
            .Include(g => g.Detalles)
            .Where(g => g.IdUsuarioModificacion == idOperador &&
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
            .FirstOrDefaultAsync(g => g.IdGuia == idGuia && g.IdUsuarioModificacion == idOperador, ct)
            ?? throw new NoEncontradoException("La guía no existe dentro de tus guías asignadas.");
        return await _context.LecturasGuia.AsNoTracking()
            .Include(x => x.Producto).Include(x => x.UbicacionOrigen).Include(x => x.UbicacionDestino)
            .Where(x => x.IdGuia == guia.IdGuia)
            .OrderByDescending(x => x.FechaRegistroUtc).ThenByDescending(x => x.IdLecturaGuia).ToListAsync(ct);
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
        var tipoOperacion = actual.IdTipoOperacionGuia;
        int? almacenOrigen = actual.IdAlmacenOrigen;
        int? almacenDestino = actual.IdAlmacenDestino;
        var requiereOrigen = tipoOperacion is TipoOperacionGuiaIds.Salida or TipoOperacionGuiaIds.Traslado;
        var requiereDestino = tipoOperacion is TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Traslado;
        var idAlmacenMovimiento = tipoOperacion == TipoOperacionGuiaIds.Entrada
            ? actual.IdAlmacenDestino!.Value
            : actual.IdAlmacenOrigen!.Value;

        foreach (var detalle in actual.Detalles)
        {
            var lineas = lecturas.Where(x => x.IdProducto == detalle.IdProducto).ToList();
            long totalLeido = lineas.Sum(x => (long)x.Cantidad);
            if (totalLeido > int.MaxValue) throw new ConflictoNegocioException("La suma leída supera el máximo permitido por el inventario.");
            foreach (var linea in lineas)
            {
                if (requiereOrigen != linea.IdUbicacionOrigen.HasValue || requiereDestino != linea.IdUbicacionDestino.HasValue)
                    throw new ReglaNegocioException($"La lectura del producto {detalle.IdProducto} no tiene las ubicaciones requeridas.");
                if (requiereOrigen && !await _context.Ubicaciones.AnyAsync(u => u.IdUbicacion == linea.IdUbicacionOrigen && u.IdAlmacen == almacenOrigen && u.Activo, ct))
                    throw new ReglaNegocioException($"La ubicación de origen del producto {detalle.IdProducto} dejó de estar activa o no pertenece al almacén.");
                if (requiereDestino && !await _context.Ubicaciones.AnyAsync(u => u.IdUbicacion == linea.IdUbicacionDestino && u.IdAlmacen == almacenDestino && u.Activo, ct))
                    throw new ReglaNegocioException($"La ubicación de destino del producto {detalle.IdProducto} dejó de estar activa o no pertenece al almacén.");
            }
            var cantidadEsperada = checked((int)detalle.CantidadEsperada);
            if (tipoOperacion == TipoOperacionGuiaIds.Entrada)
            {
                foreach (var grupo in lineas.GroupBy(x => x.IdUbicacionDestino!.Value))
                {
                    var cantidad = checked((int)grupo.Sum(x => (long)x.Cantidad));
                    await AjustarStockUbicacionAsync(grupo.Key, detalle.IdProducto, cantidad, ct);
                    await SincronizarStockAlmacenAsync(actual.IdAlmacenDestino!.Value, detalle.IdProducto, ct);
                }
            }
            else
            {
                foreach (var grupo in lineas.GroupBy(x => x.IdUbicacionOrigen!.Value))
                {
                    var cantidad = checked((int)grupo.Sum(x => (long)x.Cantidad));
                    await AjustarStockUbicacionAsync(grupo.Key, detalle.IdProducto, -cantidad, ct);
                    await SincronizarStockAlmacenAsync(actual.IdAlmacenOrigen!.Value, detalle.IdProducto, ct);
                }
                if (tipoOperacion == TipoOperacionGuiaIds.Traslado)
                    foreach (var grupo in lineas.GroupBy(x => x.IdUbicacionDestino!.Value))
                    {
                        var cantidad = checked((int)grupo.Sum(x => (long)x.Cantidad));
                        await AjustarStockUbicacionAsync(grupo.Key, detalle.IdProducto, cantidad, ct);
                        await SincronizarStockAlmacenAsync(actual.IdAlmacenDestino!.Value, detalle.IdProducto, ct);
                    }
            }

            _context.MovimientoStock.Add(new MovimientoStock
            {
                IdProducto = detalle.IdProducto,
                IdAlmacen = idAlmacenMovimiento,
                IdTipoOperacion = tipoOperacion,
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
        if (guia.IdEstadoGuia != EstadoGuiasIds.EnProceso)
            throw new ConflictoNegocioException("La guía no está en proceso.");
        if (guia.IdUsuarioModificacion != idOperador)
            throw new ConflictoNegocioException("La guía está asignada a otro operador.");
    }

    private async Task ValidarUbicacionesAsync(CabeceraGuia guia, int? origen, int? destino, CancellationToken ct)
    {
        var requiereOrigen = guia.IdTipoOperacionGuia is TipoOperacionGuiaIds.Salida or TipoOperacionGuiaIds.Traslado;
        var requiereDestino = guia.IdTipoOperacionGuia is TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Traslado;
        if (requiereOrigen != (origen is > 0) || requiereDestino != (destino is > 0))
            throw new ReglaNegocioException("Indica las ubicaciones correspondientes al tipo de operación de esta guía.");
        if (requiereOrigen && !await _context.Ubicaciones.AnyAsync(u => u.IdUbicacion == origen && u.IdAlmacen == guia.IdAlmacenOrigen && u.Activo, ct))
            throw new ReglaNegocioException("La ubicación de origen no está activa o no pertenece al almacén de origen.");
        if (requiereDestino && !await _context.Ubicaciones.AnyAsync(u => u.IdUbicacion == destino && u.IdAlmacen == guia.IdAlmacenDestino && u.Activo, ct))
            throw new ReglaNegocioException("La ubicación de destino no está activa o no pertenece al almacén de destino.");
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    public async Task<IReadOnlyList<MovimientoStock>> ListarMovimientosAsync(
        int? idGuia, int? idAlmacen, int? idOperacion, CancellationToken ct = default)
    {
        var query = _context.MovimientoStock.AsNoTracking()
            .Include(m => m.Producto)
            .Include(m => m.Almacen)
            .Include(m => m.Guia)
                .ThenInclude(g => g!.Proveedor)
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

    private async Task AjustarStockUbicacionAsync(int idUbicacion, int idProducto, int diferencia, CancellationToken ct)
    {
        if (diferencia == 0) return;
        var saldos = await _context.StockUbicacion.Where(s => s.IdUbicacion == idUbicacion && s.IdProducto == idProducto).ToListAsync(ct);
        if (saldos.Count > 1) throw new ConflictoNegocioException("Hay saldos duplicados para el producto y la ubicación.");
        var saldo = saldos.SingleOrDefault();
        if (saldo is null)
        {
            if (diferencia < 0) throw new ConflictoNegocioException("No existe stock suficiente en la ubicación de origen.");
            _context.StockUbicacion.Add(new StockUbicacion { IdUbicacion = idUbicacion, IdProducto = idProducto, CantidadActual = diferencia, FechaUltimoRegistro = DateTime.UtcNow });
            return;
        }
        var nuevaCantidad = (long)saldo.CantidadActual + diferencia;
        if (nuevaCantidad < 0) throw new ConflictoNegocioException($"Stock insuficiente del producto {idProducto} en la ubicación {idUbicacion}. Disponible: {saldo.CantidadActual}.");
        if (nuevaCantidad > int.MaxValue) throw new ConflictoNegocioException("La cantidad supera el máximo soportado por el inventario.");
        saldo.CantidadActual = (int)nuevaCantidad;
        saldo.FechaUltimoRegistro = DateTime.UtcNow;
    }

    private async Task SincronizarStockAlmacenAsync(int idAlmacen, int idProducto, CancellationToken ct)
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
            if (await _context.Ubicaciones.AnyAsync(u => u.IdUbicacion == saldoNuevo.IdUbicacion && u.IdAlmacen == idAlmacen, ct))
                ubicacionesDeNuevosSaldos.Add(saldoNuevo.IdUbicacion);
        }

        var total = saldosGuardados.Sum(s => (long)s.CantidadActual)
            + _context.ChangeTracker.Entries<StockUbicacion>()
                .Where(e => e.State == EntityState.Added && e.Entity.IdProducto == idProducto && ubicacionesDeNuevosSaldos.Contains(e.Entity.IdUbicacion))
                .Sum(e => (long)e.Entity.CantidadActual);

        if (total > int.MaxValue)
            throw new ConflictoNegocioException($"El stock total del producto {idProducto} supera el máximo soportado en el almacén {idAlmacen}.");

        var stocksAlmacen = await _context.StockAlmacen
            .Where(s => s.IdAlmacen == idAlmacen && s.IdProducto == idProducto)
            .ToListAsync(ct);
        if (stocksAlmacen.Count > 1)
            throw new ConflictoNegocioException("Hay más de un saldo para el mismo producto y almacén. Debe corregirse el stock duplicado antes de continuar.");

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
