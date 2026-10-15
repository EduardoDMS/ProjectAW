using Microsoft.EntityFrameworkCore;
using System.Data;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Seguridad.Usuarios.Entitites;
using ProjectAW.Modules.Stocks.Entities;
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

    public async Task FinalizarLecturaAsync(
        CabeceraGuia guia,
        IReadOnlyCollection<LecturaProducto> lecturas,
        int idOperador,
        CancellationToken ct = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var actual = await ObtenerGuiaParaLecturaAsync(guia.IdGuia, ct)
            ?? throw new NoEncontradoException($"La guía {guia.IdGuia} no existe.");

        if (actual.IdEstadoGuia != EstadoGuiasIds.EnProceso)
            throw new ConflictoNegocioException("Solo se puede terminar una guía que está en proceso.");
        if (actual.IdUsuarioModificacion != idOperador)
            throw new ConflictoNegocioException("La guía está asignada a otro operador.");

        var cantidadesLeidas = lecturas.ToDictionary(l => l.IdProducto);
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
            var lectura = cantidadesLeidas[detalle.IdProducto];
            var idUbicacionOrigen = lectura.IdUbicacionOrigen;
            var idUbicacionDestino = lectura.IdUbicacionDestino;
            if (requiereOrigen != idUbicacionOrigen.HasValue || requiereDestino != idUbicacionDestino.HasValue)
                throw new ReglaNegocioException($"Selecciona las ubicaciones requeridas para el producto {detalle.IdProducto}.");
            if (requiereOrigen && !await _context.Ubicaciones.AnyAsync(u => u.IdUbicacion == idUbicacionOrigen && u.IdAlmacen == almacenOrigen && u.Activo, ct))
                throw new ReglaNegocioException($"La ubicación de origen del producto {detalle.IdProducto} no está activa o no pertenece al almacén de origen.");
            if (requiereDestino && !await _context.Ubicaciones.AnyAsync(u => u.IdUbicacion == idUbicacionDestino && u.IdAlmacen == almacenDestino && u.Activo, ct))
                throw new ReglaNegocioException($"La ubicación de destino del producto {detalle.IdProducto} no está activa o no pertenece al almacén de destino.");
            var cantidadEsperada = checked((int)detalle.CantidadEsperada);
            var cantidadLeida = lectura.CantidadLeida;

            if (tipoOperacion == TipoOperacionGuiaIds.Entrada)
            {
                await AjustarStockUbicacionAsync(idUbicacionDestino!.Value, detalle.IdProducto, cantidadLeida, ct);
                await SincronizarStockAlmacenAsync(actual.IdAlmacenDestino!.Value, detalle.IdProducto, ct);
            }
            else
            {
                await AjustarStockUbicacionAsync(idUbicacionOrigen!.Value, detalle.IdProducto, -cantidadLeida, ct);
                await SincronizarStockAlmacenAsync(actual.IdAlmacenOrigen!.Value, detalle.IdProducto, ct);
                if (tipoOperacion == TipoOperacionGuiaIds.Traslado)
                {
                    await AjustarStockUbicacionAsync(idUbicacionDestino!.Value, detalle.IdProducto, cantidadLeida, ct);
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
                Cantidad = cantidadLeida,
                CantidadDiferenciada = checked(cantidadLeida - cantidadEsperada),
                Observacion = lectura.Observacion?.Trim() ?? string.Empty,
                FchMovimiento = DateTime.UtcNow,
                IdUbicacionOrigen = idUbicacionOrigen,
                IdUbicacionDestino = idUbicacionDestino
            });
        }

        actual.IdEstadoGuia = EstadoGuiasIds.Atendida;
        actual.IdUsuarioModificacion = idOperador;
        actual.FechaActualizada = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

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
