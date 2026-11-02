using Microsoft.EntityFrameworkCore;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Stocks.Entities;

namespace ProjectAW.Modules.Stocks.Repositories;

public sealed class MovimientoStockRepository : IMovimientoStockRepository
{
    private readonly AppDbContext _context;

    public MovimientoStockRepository(AppDbContext context) => _context = context;

    // Consultas de guías y asignaciones del operador.
    public Task<CabeceraGuia?> ObtenerGuiaParaLecturaAsync(
        int idGuia, CancellationToken ct = default) =>
        ConsultaGuiaCompleta()
            .FirstOrDefaultAsync(guia => guia.IdGuia == idGuia, ct);

    public Task<CabeceraGuia?> ObtenerGuiaOperadorAsync(
        int idGuia, int idOperador, CancellationToken ct = default) =>
        ConsultaGuiaCompleta()
            .AsNoTracking()
            .FirstOrDefaultAsync(guia =>
                guia.IdGuia == idGuia &&
                (guia.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada ||
                 guia.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida) &&
                guia.IdUsuarioModificacion == idOperador &&
                (guia.IdEstadoGuia == EstadoGuiasIds.EnProceso ||
                 guia.IdEstadoGuia == EstadoGuiasIds.Atendida), ct);

    public async Task<IReadOnlyList<CabeceraGuia>> ListarGuiasOperadorAsync(
        int idOperador, int? idEstadoGuia, CancellationToken ct = default)
    {
        var query = ConsultaGuiaCompleta()
            .AsNoTracking()
            .Where(guia =>
                guia.IdUsuarioModificacion == idOperador &&
                (guia.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada ||
                 guia.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida) &&
                (guia.IdEstadoGuia == EstadoGuiasIds.EnProceso ||
                 guia.IdEstadoGuia == EstadoGuiasIds.Atendida));

        if (idEstadoGuia.HasValue)
            query = query.Where(guia => guia.IdEstadoGuia == idEstadoGuia.Value);

        return await query
            .OrderByDescending(guia => guia.FechaActualizada ?? guia.FechaProgramada)
            .ToListAsync(ct);
    }

    public Task<bool> EsOperadorAsync(int idUsuario, CancellationToken ct = default) =>
        _context.Usuarios.AnyAsync(usuario =>
            usuario.IdUsuario == idUsuario &&
            usuario.Activo &&
            usuario.rol != null &&
            usuario.rol.Nombre.Trim().ToUpper() == "OPERADOR", ct);

    // Consultas y cambios de lecturas de guía.
    public async Task<IReadOnlyList<LecturaGuia>> ListarLecturasAsync(
        int idGuia, int idOperador, CancellationToken ct = default)
    {
        var guia = await _context.CabeceraGuia
            .AsNoTracking()
            .FirstOrDefaultAsync(guia =>
                guia.IdGuia == idGuia &&
                (guia.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada ||
                 guia.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida) &&
                guia.IdUsuarioModificacion == idOperador, ct)
            ?? throw new ReglaNegocioException("La guía no existe dentro de tus guías asignadas.");

        return await _context.LecturasGuia
            .AsNoTracking() /// desactivar el seguimiento de cambios para mejorar el rendimiento de la consulta, ya que no se necesita realizar un seguimiento de las entidades devueltas.
            .Include(lectura => lectura.Producto)
            .Include(lectura => lectura.UbicacionOrigen)
            .Include(lectura => lectura.UbicacionDestino)
            .Where(lectura => lectura.IdGuia == guia.IdGuia)
            .OrderByDescending(lectura => lectura.FechaRegistroUtc)
            .ThenByDescending(lectura => lectura.IdLecturaGuia)
            .ToListAsync(ct);
    }

    public Task<LecturaGuia?> ObtenerLecturaAsync(
        int idGuia, long idLecturaGuia, int idOperador, CancellationToken ct = default) =>
        _context.LecturasGuia.FirstOrDefaultAsync(lectura =>
            lectura.IdLecturaGuia == idLecturaGuia &&
            lectura.IdGuia == idGuia &&
            lectura.IdUsuarioOperador == idOperador, ct);

    public async Task<IReadOnlyList<LecturaGuia>> ObtenerLecturasParaFinalizarAsync(
        int idGuia, CancellationToken ct = default) =>
        await _context.LecturasGuia
            .AsNoTracking()
            .Where(lectura => lectura.IdGuia == idGuia)
            .OrderBy(lectura => lectura.IdLecturaGuia)
            .ToListAsync(ct);

    public void AgregarLectura(LecturaGuia lectura) => _context.LecturasGuia.Add(lectura);

    public void EliminarLectura(LecturaGuia lectura) => _context.LecturasGuia.Remove(lectura);

    // Consultas de producto y stock usadas para validar las lecturas.
    public Task<string?> ObtenerCodigoProductoAsync(int idProducto, CancellationToken ct = default) =>
        _context.Productos
            .AsNoTracking()
            .Where(producto => producto.IdProducto == idProducto)
            .Select(producto => producto.Codigo)
            .FirstOrDefaultAsync(ct);

    public async Task<long> ObtenerCantidadStockAsync(
        int idUbicacion, int idProducto, CancellationToken ct = default) =>
        await _context.StockUbicacion
            .AsNoTracking()
            .Where(stock => stock.IdUbicacion == idUbicacion && stock.IdProducto == idProducto)
            .Select(stock => (long?)stock.CantidadActual)
            .FirstOrDefaultAsync(ct) ?? 0L;

    public async Task<long> ObtenerCantidadLeidaAsync(
        int idGuia, int idProducto, int idUbicacion, CancellationToken ct = default) =>
        await _context.LecturasGuia
            .AsNoTracking()
            .Where(lectura =>
                lectura.IdGuia == idGuia &&
                lectura.IdProducto == idProducto &&
                lectura.IdUbicacionOrigen == idUbicacion)
            .SumAsync(lectura => (long?)lectura.Cantidad, ct) ?? 0L;

    // Consultas de ubicaciones activas y habilitadas para despacho.
    public Task<bool> TieneUbicacionesDespachoActivasAsync(
        int idAlmacen, CancellationToken ct = default) =>
        _context.Ubicaciones
            .AsNoTracking()
            .AnyAsync(ubicacion =>
                ubicacion.IdAlmacen == idAlmacen &&
                ubicacion.Activo &&
                _context.UbicacionesDespacho.Any(despacho =>
                    despacho.IdAlmacen == idAlmacen &&
                    despacho.IdUbicacion == ubicacion.IdUbicacion), ct);

    public async Task<IReadOnlySet<int>> ObtenerUbicacionesActivasAsync(
        int idAlmacen,
        IReadOnlyCollection<int> idsUbicaciones,
        bool soloDespacho,
        CancellationToken ct = default)
    {
        if (idsUbicaciones.Count == 0)
            return new HashSet<int>();

        var query = _context.Ubicaciones
            .AsNoTracking()
            .Where(ubicacion =>
                ubicacion.IdAlmacen == idAlmacen &&
                ubicacion.Activo &&
                idsUbicaciones.Contains(ubicacion.IdUbicacion));

        if (soloDespacho)
        {
            query = query.Where(ubicacion => _context.UbicacionesDespacho.Any(despacho =>
                despacho.IdAlmacen == idAlmacen &&
                despacho.IdUbicacion == ubicacion.IdUbicacion));
        }

        var idsValidos = await query
            .Select(ubicacion => ubicacion.IdUbicacion)
            .ToListAsync(ct);

        return idsValidos.ToHashSet();
    }

    // Registro y consulta del historial de movimientos.
    public void AgregarMovimiento(MovimientoStock movimiento) => _context.MovimientoStock.Add(movimiento);

    public Task GuardarCambiosAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<MovimientoStock>> ListarMovimientosAsync(
        int? idGuia, int? idAlmacen, int? idOperacion, CancellationToken ct = default)
    {
        /// se esta usando Include para cargar las relaciones necesarias para mostrar la información completa de los movimientos de stock,
        /// incluyendo detalles de la guía, producto, almacén, ubicaciones y usuario. 
        /// Esto permite que la API devuelva un conjunto completo de datos relacionados con cada movimiento de stock sin necesidad de 
        /// realizar múltiples consultas a la base de datos.
        /// 
        /// y tambien ThenInclude para incluir propiedades de navegación adicionales en las relaciones, como los detalles de la guía y el proveedor, 
        /// cliente, motivo, almacenes de origen y destino, lo que permite obtener información más detallada sobre cada movimiento de stock.
        /// 
        ///y el AsNoTracking() se utiliza para mejorar el rendimiento de la consulta, ya que no se necesita realizar un seguimiento de los cambios 
        ///en las entidades devueltas por la consulta.

        var query = _context.MovimientoStock
            .AsNoTracking()
            .Include(movimiento => movimiento.Producto)
            .Include(movimiento => movimiento.Almacen)
            .Include(movimiento => movimiento.Guia).ThenInclude(guia => guia!.Proveedor)
            .Include(movimiento => movimiento.Guia).ThenInclude(guia => guia!.Detalles)
            .Include(movimiento => movimiento.Guia).ThenInclude(guia => guia!.Motivo)
            .Include(movimiento => movimiento.Guia).ThenInclude(guia => guia!.Cliente)
            .Include(movimiento => movimiento.Guia).ThenInclude(guia => guia!.AlmacenOrigen)
            .Include(movimiento => movimiento.Guia).ThenInclude(guia => guia!.AlmacenDestino)
            .Include(movimiento => movimiento.OperacionGuias)
            .Include(movimiento => movimiento.UbicacionOrigen)
            .Include(movimiento => movimiento.UbicacionDestino)
            .Include(movimiento => movimiento.Usuario)
            .AsQueryable();
        /// ASQueryable() se utiliza para convertir la consulta en un IQueryable, lo que permite aplicar filtros y condiciones adicionales de manera flexible
        /// antes de ejecutar la consulta final.
        /// 
        if (idGuia.HasValue)
            query = query.Where(movimiento => movimiento.IdGuia == idGuia.Value);

        if (idAlmacen.HasValue)
        {
            query = query.Where(movimiento =>
                movimiento.IdAlmacen == idAlmacen.Value ||
                movimiento.Guia!.IdAlmacenOrigen == idAlmacen.Value ||
                movimiento.Guia.IdAlmacenDestino == idAlmacen.Value);
        }

        if (idOperacion.HasValue)
            query = query.Where(movimiento => movimiento.IdTipoOperacion == idOperacion.Value);

        return await query
            .OrderByDescending(movimiento => movimiento.FchMovimiento)
            .ToListAsync(ct);
    }

    private IQueryable<CabeceraGuia> ConsultaGuiaCompleta() => _context.CabeceraGuia
        .Include(guia => guia.Detalles)
        .Include(guia => guia.Proveedor)
        .Include(guia => guia.Cliente)
        .Include(guia => guia.AlmacenOrigen)
        .Include(guia => guia.AlmacenDestino)
        .Include(guia => guia.Motivo).ThenInclude(motivo => motivo!.TipoComportamiento)
        .Include(guia => guia.GuiaRelacionada);
}
