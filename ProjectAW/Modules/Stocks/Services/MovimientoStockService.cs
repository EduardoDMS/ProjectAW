using ProjectAW.Infrastructure.Errors;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Models;
using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Seguridad.Security.HttpAuth;
using ProjectAW.Modules.Stocks.DTOs.Requests;
using ProjectAW.Modules.Stocks.DTOs.Responses;
using ProjectAW.Modules.Stocks.Entities;
using ProjectAW.Modules.Guias.DTOs.Response;
using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Stocks.Repositories;

namespace ProjectAW.Modules.Stocks.Services;

public sealed class MovimientoStockService : IMovimientoStockService
{
    private readonly IMovimientoStockRepository _repository;
    private readonly ITransactionManager _transactions;
    private readonly ICurrentUserService _currentUser;
    private readonly IReadOnlyDictionary<int, IGuiaStockProcessor> _stockProcessors;

    public MovimientoStockService(
        IMovimientoStockRepository repository,
        ITransactionManager transactions,
        ICurrentUserService currentUser,
        IEnumerable<IGuiaStockProcessor> stockProcessors)
    {
        _repository = repository;
        _transactions = transactions;
        _currentUser = currentUser;
        _stockProcessors = stockProcessors.ToDictionary(processor => processor.IdTipoOperacionGuia);
    }
    /// <summary>
    ///     se usa toDictionary para crear un diccionario a partir de la colección de procesadores de stock, donde la clave es el IdTipoOperacionGuia 
    ///     de cada procesador. Esto permite acceder rápidamente a un procesador específico según el tipo de operación de la guía. 
    /// </summary>

    public async Task TomarGuiaAsync(int idGuia, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);
        await _transactions.ExecuteSerializableAsync(
            transactionCt => TomarGuiaEnTransaccionAsync(idGuia, transactionCt), ct);
    }

    private async Task TomarGuiaEnTransaccionAsync(int idGuia, CancellationToken ct)
    {
        var guia = await ObtenerGuiaAsync(idGuia, ct);
        ValidarGuiaPuedeSerTomada(guia);

        guia.IdEstadoGuia = EstadoGuiasIds.EnProceso;
        guia.IdUsuarioModificacion = _currentUser.IdUsuario;
        guia.FechaActualizada = DateTime.UtcNow;
        await _repository.GuardarCambiosAsync(ct);
    }

    private static void ValidarGuiaPuedeSerTomada(CabeceraGuia guia)
    {
        ExigirOperacionOperador(guia.IdTipoOperacionGuia);
        if (guia.IdEstadoGuia != EstadoGuiasIds.Pendiente)
            throw new ConflictoNegocioException("La guía ya no está pendiente y no puede ser tomada.");
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
        ExigirOperacionOperador(guia.IdTipoOperacionGuia);
        return ToGuiaOperadorDto(guia);
    }

    public async Task<LecturaGuiaDto> RegistrarLecturaAsync(int idGuia, RegistrarLecturaRequest request, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);
        var lectura = await RegistrarLecturaEnTransaccionAsync(idGuia, request, ct);
        return ToLecturaDto(lectura);
    }

    /// <summary>
    /// En una transacción serializable valida la guía y el producto, comprueba ubicaciones
    /// y stock disponible, y guarda la lectura solo cuando todas las reglas se cumplen.
    /// </summary>
    private async Task<LecturaGuia> RegistrarLecturaEnTransaccionAsync(
        int idGuia, RegistrarLecturaRequest request, CancellationToken ct)
    {
        LecturaGuia? lectura = null;
        await _transactions.ExecuteSerializableAsync(async transactionCt =>
        {
            var guia = await ObtenerGuiaAsync(idGuia, transactionCt);
            ValidarGuiaEnProceso(guia, _currentUser.IdUsuario);

            await ValidarProductoLeidoAsync(guia, request, transactionCt);
            await ValidarUbicacionesYDisponibilidadAsync(guia, idGuia, request, transactionCt);

            lectura = CrearLectura(idGuia, request);
            _repository.AgregarLectura(lectura);
            await _repository.GuardarCambiosAsync(transactionCt);
        }, ct);

        return lectura ?? throw new InvalidOperationException("La transacción terminó sin registrar la lectura.");
    }

    private async Task ValidarProductoLeidoAsync(
        CabeceraGuia guia, RegistrarLecturaRequest request, CancellationToken ct)
    {
        var detalle = guia.Detalles.FirstOrDefault(d => d.IdProducto == request.IdProducto)
            ?? throw new ReglaNegocioException("El producto leído no pertenece a esta guía.");
        var codigoProducto = await _repository.ObtenerCodigoProductoAsync(detalle.IdProducto, ct)
            ?? throw new NoEncontradoException("El producto de la guía ya no existe.");

        if (!string.Equals(codigoProducto.Trim(), request.CodigoLeido.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ReglaNegocioException("El código leído no corresponde al producto seleccionado.");
    }

    private async Task ValidarUbicacionesYDisponibilidadAsync(
        CabeceraGuia guia, int idGuia, RegistrarLecturaRequest request, CancellationToken ct)
    {
        await ValidarUbicacionesAsync(guia, request.IdUbicacionOrigen, request.IdUbicacionDestino, ct);
        if (guia.IdTipoOperacionGuia != TipoOperacionGuiaIds.Salida)
            return;

        await ValidarDisponibilidadSalidaAsync(idGuia, request, ct);
    }

    private async Task ValidarDisponibilidadSalidaAsync(
        int idGuia, RegistrarLecturaRequest request, CancellationToken ct)
    {
        var idUbicacionOrigen = request.IdUbicacionOrigen!.Value;
        var stockActual = await _repository.ObtenerCantidadStockAsync(idUbicacionOrigen, request.IdProducto, ct);
        var yaRegistrado = await _repository.ObtenerCantidadLeidaAsync(
            idGuia, request.IdProducto, idUbicacionOrigen, ct);
        if (yaRegistrado + request.Cantidad > stockActual)
            throw new ReglaNegocioException($"La ubicación de origen solo tiene {Math.Max(stockActual - yaRegistrado, 0)} unidades disponibles de este producto.");
    }

    private LecturaGuia CrearLectura(int idGuia, RegistrarLecturaRequest request) => new()
    {
        IdGuia = idGuia,
        IdProducto = request.IdProducto,
        IdUsuarioOperador = _currentUser.IdUsuario,
        Cantidad = request.Cantidad,
        CodigoLeido = request.CodigoLeido.Trim(),
        Lote = Normalizar(request.Lote),
        Serie = Normalizar(request.Serie),
        Observacion = Normalizar(request.Observacion),
        IdUbicacionOrigen = request.IdUbicacionOrigen is > 0 ? request.IdUbicacionOrigen : null,
        IdUbicacionDestino = request.IdUbicacionDestino is > 0 ? request.IdUbicacionDestino : null,
        FechaRegistroUtc = DateTime.UtcNow
    };

    public async Task<IReadOnlyList<LecturaGuiaDto>> ListarLecturasAsync(int idGuia, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);
        var lecturas = await _repository.ListarLecturasAsync(idGuia, _currentUser.IdUsuario, ct);
        return lecturas.Select(ToLecturaDto).ToList();
    }

    public async Task<bool> EliminarLecturaAsync(int idGuia, long idLecturaGuia, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);

        var eliminada = false;

        // La guía, la lectura y su eliminación se procesan juntas para evitar cambios
        // si la guía deja de estar asignada o cambia de estado durante la operación.
        await _transactions.ExecuteSerializableAsync(async transactionCt =>
        {
            var guia = await ObtenerGuiaAsync(idGuia, transactionCt);
            ValidarGuiaEnProceso(guia, _currentUser.IdUsuario);

            var lectura = await _repository.ObtenerLecturaAsync(
                idGuia, idLecturaGuia, _currentUser.IdUsuario, transactionCt);
            if (lectura is null)
                return;

            _repository.EliminarLectura(lectura);
            await _repository.GuardarCambiosAsync(transactionCt);
            eliminada = true;
        }, ct);

        return eliminada;
    }

    public async Task FinalizarLecturaAsync(int idGuia, CancellationToken ct = default)
    {
        await ExigirOperadorAsync(ct);

        // El cierre completo comparte una transacción: validaciones, ajustes de stock,
        // movimientos de historial y cambio de estado se confirman o revierten juntos.
        await _transactions.ExecuteSerializableAsync(
            transactionCt => FinalizarLecturaEnTransaccionAsync(idGuia, transactionCt), ct);
    }

    /// <summary>
    /// Cierra la guía dentro de una transacción serializable. Valida el estado y las lecturas,
    /// aplica el procesador de stock, crea un movimiento por producto y marca la guía atendida.
    /// Cualquier error cancela juntos los cambios de stock, movimientos y estado.
    /// </summary>
    private async Task FinalizarLecturaEnTransaccionAsync(int idGuia, CancellationToken ct)
    {
        var guia = await ObtenerGuiaAsync(idGuia, ct);
        var processor = ValidarGuiaParaFinalizar(guia);

        var lecturas = await ObtenerLecturasValidadasAsync(guia, processor, ct);

        await AplicarLecturasAsync(guia, lecturas, processor, _currentUser.IdUsuario, ct);

        // Marca la guía como atendida únicamente después de preparar el stock y el historial.
        guia.IdEstadoGuia = EstadoGuiasIds.Atendida;
        guia.IdUsuarioModificacion = _currentUser.IdUsuario;
        guia.FechaActualizada = DateTime.UtcNow;

        await _repository.GuardarCambiosAsync(ct);
    }

    private IGuiaStockProcessor ValidarGuiaParaFinalizar(CabeceraGuia guia)
    {
        ExigirOperacionOperador(guia.IdTipoOperacionGuia);
        ValidarDetallesDeGuia(guia);

        var processor = ObtenerProcesador(guia.IdTipoOperacionGuia);
        processor.ValidarGuia(guia);
        ValidarCantidadesEnteras(guia);
        ValidarAsignacionEnProceso(guia, _currentUser.IdUsuario);
        return processor;
    }

    private static void ValidarDetallesDeGuia(CabeceraGuia guia)
    {
        var noTieneDetalles = guia.Detalles.Count == 0;
        var tieneProductosDuplicados = guia.Detalles
            .GroupBy(detalle => detalle.IdProducto)
            .Any(grupo => grupo.Count() > 1);

        if (noTieneDetalles || tieneProductosDuplicados)
            throw new ReglaNegocioException("La guía no tiene productos válidos para procesar.");
    }

    private static void ValidarCantidadesEnteras(CabeceraGuia guia)
    {
        var contieneCantidadNoEntera = guia.Detalles.Any(detalle =>
            detalle.CantidadEsperada != decimal.Truncate(detalle.CantidadEsperada) ||
            detalle.CantidadEsperada > int.MaxValue);

        if (contieneCantidadNoEntera)
            throw new ReglaNegocioException("Esta guía contiene cantidades fraccionarias, pero el stock actual registra unidades enteras. No se aplicó ningún movimiento.");
    }

    /// <summary>Confirma lecturas completas y ubicaciones activas antes de modificar saldos.</summary>
    private async Task<IReadOnlyList<LecturaGuia>> ObtenerLecturasValidadasAsync(
        CabeceraGuia guia, IGuiaStockProcessor processor, CancellationToken ct)
    {
        var lecturas = await _repository.ObtenerLecturasParaFinalizarAsync(guia.IdGuia, ct);
        var productosRequeridos = guia.Detalles.Select(detalle => detalle.IdProducto).ToHashSet();
        var productosLeidos = lecturas.Select(lectura => lectura.IdProducto).ToHashSet();
        if (!productosRequeridos.SetEquals(productosLeidos))
            throw new ReglaNegocioException("Registra al menos una lectura para cada producto de la guía antes de finalizar.");

        // La guía de salida exige ubicación de despacho si el almacén tiene alguna configurada.
        var requiereUbicacionDespacho = guia.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida &&
            await _repository.TieneUbicacionesDespachoActivasAsync(guia.IdAlmacenOrigen!.Value, ct);
        await ValidarUbicacionesFinalesAsync(guia, lecturas, processor, requiereUbicacionDespacho, ct);
        return lecturas;
    }

    private async Task AplicarLecturasAsync(
        CabeceraGuia guia,
        IReadOnlyList<LecturaGuia> lecturas,
        IGuiaStockProcessor processor,
        int idOperador,
        CancellationToken ct)
    {
        // El procesador cambia los saldos; el servicio registra el historial común del movimiento.
        foreach (var detalle in guia.Detalles)
            await AplicarLecturasDelProductoAsync(guia, detalle, lecturas, processor, idOperador, ct);
    }

    private async Task AplicarLecturasDelProductoAsync(
        CabeceraGuia guia,
        DetalleGuia detalle,
        IReadOnlyList<LecturaGuia> lecturas,
        IGuiaStockProcessor processor,
        int idOperador,
        CancellationToken ct)
    {
        var lecturasDelProducto = lecturas
            .Where(lectura => lectura.IdProducto == detalle.IdProducto)
            .ToList();
        var totalLeido = lecturasDelProducto.Sum(lectura => (long)lectura.Cantidad);
        if (totalLeido > int.MaxValue)
            throw new ConflictoNegocioException("La suma leída supera el máximo permitido por el inventario.");

        await processor.AplicarProductoAsync(guia, detalle, lecturasDelProducto, ct);

        var cantidadEsperada = checked((int)detalle.CantidadEsperada);
        var movimiento = CrearMovimiento(
            guia, detalle, lecturasDelProducto, processor, idOperador,
            (int)totalLeido, cantidadEsperada);
        _repository.AgregarMovimiento(movimiento);
    }

    public async Task<IReadOnlyList<MovimientoStockDto>> ListarAsync(
        int? idGuia, int? idAlmacen, int? idOperacion, CancellationToken ct = default)
    {
        var movimientos = await _repository.ListarMovimientosAsync(idGuia, idAlmacen, idOperacion, ct);
        return movimientos.Select(ToMovimientoDto).ToList();
    }

    private static MovimientoStockDto ToMovimientoDto(MovimientoStock movimiento) => new(
        IdMovimientoStock: movimiento.IdMovimientoStock,
        IdGuia: movimiento.IdGuia,
        NumeroGuia: movimiento.Guia?.NumeroGuia ?? string.Empty,
        MotivoGuia: movimiento.Guia?.Motivo?.NombreMotivo ?? "—",
        IdOperacion: movimiento.IdTipoOperacion,
        Operacion: movimiento.OperacionGuias?.Nombre ?? string.Empty,
        IdProducto: movimiento.IdProducto,
        CodigoProducto: movimiento.Producto?.Codigo ?? string.Empty,
        Producto: movimiento.Producto?.Descripcion ?? string.Empty,
        IdAlmacen: movimiento.IdAlmacen,
        Origen: NombreOrigen(movimiento),
        Destino: NombreDestino(movimiento),
        CantidadEsperada: movimiento.Guia?.Detalles
            .FirstOrDefault(detalle => detalle.IdProducto == movimiento.IdProducto)?.CantidadEsperada ?? 0,
        Cantidad: movimiento.Cantidad,
        CantidadDiferenciada: movimiento.CantidadDiferenciada,
        Observacion: movimiento.Observacion,
        IdUsuarioOperador: movimiento.IdUsuarioOperador,
        Operador: NombreOperador(movimiento.Usuario),
        IdUbicacionOrigen: movimiento.IdUbicacionOrigen,
        UbicacionOrigen: movimiento.UbicacionOrigen?.Codigo ?? string.Empty,
        IdUbicacionDestino: movimiento.IdUbicacionDestino,
        UbicacionDestino: movimiento.UbicacionDestino?.Codigo ?? string.Empty,
        FechaMovimiento: movimiento.FchMovimiento);

    private static string NombreOperador(ProjectAW.Modules.Seguridad.Usuarios.Entitites.Usuario? usuario) =>
        usuario is null ? string.Empty : $"{usuario.Nombre} {usuario.Apellido}".Trim();

    private static string NombreOrigen(MovimientoStock movimiento) =>
        movimiento.IdTipoOperacion == TipoOperacionGuiaIds.Entrada
            ? movimiento.Guia?.Proveedor?.RazonSocial ?? "Proveedor"
            : movimiento.Almacen?.Descripcion ?? "Almacén";

    private static string NombreDestino(MovimientoStock movimiento) =>
        movimiento.IdTipoOperacion == TipoOperacionGuiaIds.Salida
            ? movimiento.Guia?.Cliente?.RazonSocial ?? "Cliente"
            : movimiento.Guia?.AlmacenDestino?.Descripcion ?? "Almacén";

    private async Task ExigirOperadorAsync(CancellationToken ct)
    {
        if (!await _repository.EsOperadorAsync(_currentUser.IdUsuario, ct))
            throw new ReglaNegocioException("Solo un usuario activo con rol Operador puede realizar lecturas de guías.");
    }

    private IGuiaStockProcessor ObtenerProcesador(int idTipoOperacionGuia) =>
        _stockProcessors.TryGetValue(idTipoOperacionGuia, out var processor)
            ? processor
            : throw new ReglaNegocioException("La guía tiene un tipo de operación inválido.");

    private static void ExigirOperacionOperador(int idTipoOperacionGuia)
    {
        if (idTipoOperacionGuia is not (TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Salida))
            throw new ReglaNegocioException("El operador solo puede atender guías de Entrada o Salida. Los movimientos internos se generan mediante guías relacionadas.");
    }

    private static void ValidarGuiaEnProceso(CabeceraGuia guia, int idOperador)
    {
        if (guia.IdTipoOperacionGuia is not (TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Salida))
            throw new ReglaNegocioException("El operador solo puede atender guías de Entrada o Salida. Los movimientos internos se generan mediante guías relacionadas.");
        ValidarAsignacionEnProceso(guia, idOperador);
    }

    private static void ValidarAsignacionEnProceso(CabeceraGuia guia, int idOperador)
    {
        if (guia.IdEstadoGuia != EstadoGuiasIds.EnProceso)
            throw new ConflictoNegocioException("La guía no está en proceso.");
        if (guia.IdUsuarioModificacion != idOperador)
            throw new ConflictoNegocioException("La guía está asignada a otro operador.");
    }

    private async Task ValidarUbicacionesAsync(
        CabeceraGuia guia, int? origen, int? destino, CancellationToken ct)
    {
        var processor = ObtenerProcesador(guia.IdTipoOperacionGuia);
        var salida = guia.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida;
        var requiereUbicacionDespacho = await RequiereUbicacionDespachoAsync(guia, salida, ct);
        var requiereDestino = salida ? requiereUbicacionDespacho : processor.RequiereUbicacionDestino;

        ValidarUbicacionesIndicadas(processor.RequiereUbicacionOrigen, requiereDestino, origen, destino);
        await ValidarUbicacionOrigenAsync(guia, processor, origen, ct);
        await ValidarUbicacionDespachoAsync(guia, salida, requiereUbicacionDespacho, destino, ct);
        await ValidarUbicacionDestinoAsync(guia, processor, salida, destino, ct);
    }

    private Task<bool> RequiereUbicacionDespachoAsync(
        CabeceraGuia guia, bool salida, CancellationToken ct)
    {
        if (!salida || guia.IdAlmacenOrigen is not int idAlmacenOrigen)
            return Task.FromResult(false);

        return _repository.TieneUbicacionesDespachoActivasAsync(idAlmacenOrigen, ct);
    }

    private static void ValidarUbicacionesIndicadas(
        bool requiereOrigen, bool requiereDestino, int? origen, int? destino)
    {
        var origenIndicado = origen is > 0;
        var destinoIndicado = destino is > 0;
        if (requiereOrigen != origenIndicado || requiereDestino != destinoIndicado)
            throw new ReglaNegocioException("Indica las ubicaciones correspondientes al tipo de operación de esta guía.");
    }

    private async Task ValidarUbicacionOrigenAsync(
        CabeceraGuia guia, IGuiaStockProcessor processor, int? origen, CancellationToken ct)
    {
        if (!processor.RequiereUbicacionOrigen)
            return;

        if (guia.IdAlmacenOrigen is not int idAlmacenOrigen ||
            !await UbicacionActivaAsync(idAlmacenOrigen, origen!.Value, false, ct))
            throw new ReglaNegocioException("La ubicación de origen no está activa o no pertenece al almacén de origen.");
    }

    private async Task ValidarUbicacionDespachoAsync(
        CabeceraGuia guia, bool salida, bool requiereUbicacionDespacho,
        int? destino, CancellationToken ct)
    {
        if (!salida || !requiereUbicacionDespacho)
            return;

        if (!await UbicacionActivaAsync(
                guia.IdAlmacenOrigen!.Value, destino!.Value, true, ct))
            throw new ReglaNegocioException("La ubicación elegida no está habilitada para despacho en el almacén de origen.");
    }

    private async Task ValidarUbicacionDestinoAsync(
        CabeceraGuia guia,
        IGuiaStockProcessor processor,
        bool salida,
        int? destino,
        CancellationToken ct)
    {
        if (salida || !processor.RequiereUbicacionDestino)
            return;

        if (guia.IdAlmacenDestino is not int idAlmacenDestino ||
            !await UbicacionActivaAsync(idAlmacenDestino, destino!.Value, false, ct))
            throw new ReglaNegocioException("La ubicación de destino no está activa o no pertenece al almacén de destino.");
    }

    private async Task ValidarUbicacionesFinalesAsync(
        CabeceraGuia guia,
        IReadOnlyList<LecturaGuia> lecturas,
        IGuiaStockProcessor processor,
        bool requiereUbicacionDespacho,
        CancellationToken ct)
    {
        // Consulta las ubicaciones en bloque y reutiliza esos resultados al validar cada lectura.
        var salida = guia.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida;
        var ubicacionesOrigenValidas = await ObtenerUbicacionesOrigenValidasAsync(
            guia, lecturas, processor, ct);

        var requiereUbicacionDestino = salida ? requiereUbicacionDespacho : processor.RequiereUbicacionDestino;
        var almacenDestino = salida ? guia.IdAlmacenOrigen!.Value : guia.IdAlmacenDestino!.Value;
        var ubicacionesDestinoValidas = await ObtenerUbicacionesDestinoValidasAsync(
            almacenDestino, lecturas, requiereUbicacionDestino, salida, ct);

        ValidarLecturasContraUbicaciones(
            lecturas, processor, salida, requiereUbicacionDespacho,
            ubicacionesOrigenValidas, ubicacionesDestinoValidas);
    }

    private Task<IReadOnlySet<int>> ObtenerUbicacionesOrigenValidasAsync(
        CabeceraGuia guia,
        IReadOnlyList<LecturaGuia> lecturas,
        IGuiaStockProcessor processor,
        CancellationToken ct)
    {
        if (!processor.RequiereUbicacionOrigen)
            return Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());

        var idsOrigen = lecturas
            .Where(lectura => lectura.IdUbicacionOrigen.HasValue)
            .Select(lectura => lectura.IdUbicacionOrigen!.Value)
            .Distinct()
            .ToArray();

        return _repository.ObtenerUbicacionesActivasAsync(
            guia.IdAlmacenOrigen!.Value, idsOrigen, false, ct);
    }

    private Task<IReadOnlySet<int>> ObtenerUbicacionesDestinoValidasAsync(
        int idAlmacenDestino,
        IReadOnlyList<LecturaGuia> lecturas,
        bool requiereUbicacionDestino,
        bool esDespacho,
        CancellationToken ct)
    {
        if (!requiereUbicacionDestino)
            return Task.FromResult<IReadOnlySet<int>>(new HashSet<int>());

        var idsDestino = lecturas
            .Where(lectura => lectura.IdUbicacionDestino.HasValue)
            .Select(lectura => lectura.IdUbicacionDestino!.Value)
            .Distinct()
            .ToArray();

        return _repository.ObtenerUbicacionesActivasAsync(
            idAlmacenDestino, idsDestino, esDespacho, ct);
    }

    private static void ValidarLecturasContraUbicaciones(
        IReadOnlyList<LecturaGuia> lecturas,
        IGuiaStockProcessor processor,
        bool salida,
        bool requiereUbicacionDespacho,
        IReadOnlySet<int> ubicacionesOrigenValidas,
        IReadOnlySet<int> ubicacionesDestinoValidas)
    {
        foreach (var lectura in lecturas)
            ValidarUbicacionLeida(lectura, processor, salida, requiereUbicacionDespacho,
                ubicacionesOrigenValidas, ubicacionesDestinoValidas);
    }

    private static void ValidarUbicacionLeida(
        LecturaGuia lectura,
        IGuiaStockProcessor processor,
        bool salida,
        bool requiereUbicacionDespacho,
        IReadOnlySet<int> ubicacionesOrigenValidas,
        IReadOnlySet<int> ubicacionesDestinoValidas)
    {
        var requiereDestino = salida
            ? requiereUbicacionDespacho
            : processor.RequiereUbicacionDestino;
        ValidarUbicacionesIndicadas(
            processor.RequiereUbicacionOrigen, requiereDestino,
            lectura.IdUbicacionOrigen.HasValue, lectura.IdUbicacionDestino.HasValue,
            lectura.IdProducto);

        ValidarUbicacionOrigenLeida(lectura, processor.RequiereUbicacionOrigen, ubicacionesOrigenValidas);
        ValidarUbicacionDestinoLeida(
            lectura, salida, requiereDestino, ubicacionesDestinoValidas);
    }

    private static void ValidarUbicacionesIndicadas(
        bool requiereOrigen, bool requiereDestino, bool tieneOrigen, bool tieneDestino, int idProducto)
    {
        if (requiereOrigen != tieneOrigen || requiereDestino != tieneDestino)
            throw new ReglaNegocioException($"La lectura del producto {idProducto} no tiene las ubicaciones requeridas.");
    }

    private static void ValidarUbicacionOrigenLeida(
        LecturaGuia lectura, bool requiereOrigen, IReadOnlySet<int> ubicacionesOrigenValidas)
    {
        if (requiereOrigen && !ubicacionesOrigenValidas.Contains(lectura.IdUbicacionOrigen!.Value))
            throw new ReglaNegocioException($"La ubicación de origen del producto {lectura.IdProducto} dejó de estar activa o no pertenece al almacén.");
    }

    private static void ValidarUbicacionDestinoLeida(
        LecturaGuia lectura,
        bool salida,
        bool requiereUbicacionDestino,
        IReadOnlySet<int> ubicacionesDestinoValidas)
    {
        if (!requiereUbicacionDestino)
            return;

        if (!ubicacionesDestinoValidas.Contains(lectura.IdUbicacionDestino!.Value))
        {
            var mensaje = salida
                ? $"La ubicación elegida para el despacho del producto {lectura.IdProducto} no está habilitada para despacho en el almacén de origen."
                : $"La ubicación destino del producto {lectura.IdProducto} dejó de estar activa o no pertenece al almacén asignado.";
            throw new ReglaNegocioException(mensaje);
        }
    }

    private async Task<bool> UbicacionActivaAsync(
        int idAlmacen, int idUbicacion, bool soloDespacho, CancellationToken ct)
    {
        var ubicaciones = await _repository.ObtenerUbicacionesActivasAsync(
            idAlmacen, [idUbicacion], soloDespacho, ct);
        return ubicaciones.Contains(idUbicacion);
    }

    private static MovimientoStock CrearMovimiento(
        CabeceraGuia guia,
        DetalleGuia detalle,
        IReadOnlyList<LecturaGuia> lecturas,
        IGuiaStockProcessor processor,
        int idOperador,
        int totalLeido,
        int cantidadEsperada)
    {
        var observacion = lecturas
            .Select(lectura => lectura.Observacion)
            .FirstOrDefault(texto => !string.IsNullOrWhiteSpace(texto)) ?? string.Empty;

        return new MovimientoStock
        {
            IdProducto = detalle.IdProducto,
            IdAlmacen = processor.ObtenerAlmacenMovimiento(guia),
            IdTipoOperacion = processor.IdTipoOperacionGuia,
            IdGuia = guia.IdGuia,
            IdUsuarioOperador = idOperador,
            Cantidad = totalLeido,
            CantidadDiferenciada = checked(totalLeido - cantidadEsperada),
            Observacion = observacion,
            FchMovimiento = DateTime.UtcNow,
            IdUbicacionOrigen = ObtenerUbicacionComun(lecturas.Select(lectura => lectura.IdUbicacionOrigen)),
            IdUbicacionDestino = ObtenerUbicacionComun(lecturas.Select(lectura => lectura.IdUbicacionDestino))
        };
    }

    private static int? ObtenerUbicacionComun(IEnumerable<int?> ubicaciones)
    {
        var ubicacionesDistintas = ubicaciones.Distinct().ToList();
        return ubicacionesDistintas.Count == 1 ? ubicacionesDistintas[0] : null;
    }

    private static string? Normalizar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private async Task<CabeceraGuia> ObtenerGuiaAsync(int idGuia, CancellationToken ct) =>
        await _repository.ObtenerGuiaParaLecturaAsync(idGuia, ct)
        ?? throw new NoEncontradoException($"La guía {idGuia} no existe.");

    private static GuiaOperadorDto ToGuiaOperadorDto(CabeceraGuia guia) => new(
        IdGuia: guia.IdGuia,
        NumeroGuia: guia.NumeroGuia,
        IdTipoOperacionGuia: guia.IdTipoOperacionGuia,
        IdMotivoGuia: guia.IdMotivoGuia,
        IdTipoComportamiento: guia.Motivo?.IdTipoComportamiento,
        NombreMotivo: guia.Motivo?.NombreMotivo,
        RequiereProveedor: RequiereProveedor(guia),
        RequiereCliente: RequiereCliente(guia),
        RequiereAlmacenOrigen: RequiereAlmacenOrigen(guia),
        RequiereAlmacenDestino: RequiereAlmacenDestino(guia),
        TipoTercero: TipoTercero(guia),
        NombreTercero: NombreTercero(guia),
        NombreAlmacenOrigen: NombreAlmacen(guia.AlmacenOrigen),
        NombreAlmacenDestino: NombreAlmacen(guia.AlmacenDestino),
        IdGuiaRelacionada: guia.IdGuiaRelacionada,
        NumeroGuiaRelacionada: guia.GuiaRelacionada?.NumeroGuia,
        IdEstadoGuia: guia.IdEstadoGuia,
        IdProveedor: guia.IdProveedor,
        IdCliente: guia.IdCliente,
        IdAlmacenOrigen: guia.IdAlmacenOrigen,
        IdAlmacenDestino: guia.IdAlmacenDestino,
        FechaProgramada: guia.FechaProgramada,
        FechaRegistro: guia.FechaRegistro,
        FechaActualizada: guia.FechaActualizada,
        TipoDocumentoReferencia: guia.TipoDocumentoReferencia,
        NumeroDocumentoReferencia: guia.NumeroDocumentoReferencia,
        Observacion: guia.Observacion,
        Detalles: guia.Detalles
            .Select(detalle => new DetalleGuiaResponse(
                detalle.IdGuiaDetalle, detalle.IdProducto, detalle.CantidadEsperada))
            .ToList());

    private static bool RequiereProveedor(CabeceraGuia g) => g.Motivo?.RequiereProveedor ?? g.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada;
    private static bool RequiereCliente(CabeceraGuia g) => g.Motivo?.RequiereCliente ?? g.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida;
    private static bool RequiereAlmacenOrigen(CabeceraGuia g) => g.Motivo?.RequiereAlmacenOrigen ?? g.IdTipoOperacionGuia is TipoOperacionGuiaIds.Salida or TipoOperacionGuiaIds.Traslado;
    private static bool RequiereAlmacenDestino(CabeceraGuia g) => g.Motivo?.RequiereAlmacenDestino ?? g.IdTipoOperacionGuia is TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Traslado;
    private static string? TipoTercero(CabeceraGuia g) => RequiereProveedor(g) ? "Proveedor" : RequiereCliente(g) ? "Cliente" : null;
    private static string? NombreTercero(CabeceraGuia g) => RequiereProveedor(g) ? g.Proveedor?.RazonSocial : RequiereCliente(g) ? g.Cliente?.RazonSocial : null;
    private static string? NombreAlmacen(Modules.Maestros.Almacenes.Entities.Almacen? almacen) => almacen is null ? null : string.Join(" · ", new[] { almacen.Codigo, almacen.Descripcion }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static LecturaGuiaDto ToLecturaDto(LecturaGuia lectura) => new(
        IdLecturaGuia: lectura.IdLecturaGuia,
        IdGuia: lectura.IdGuia,
        IdProducto: lectura.IdProducto,
        CodigoProducto: lectura.Producto?.Codigo ?? lectura.CodigoLeido,
        DescripcionProducto: lectura.Producto?.Descripcion ?? string.Empty,
        CodigoLeido: lectura.CodigoLeido,
        Cantidad: lectura.Cantidad,
        Lote: lectura.Lote,
        Serie: lectura.Serie,
        Observacion: lectura.Observacion,
        IdUbicacionOrigen: lectura.IdUbicacionOrigen,
        UbicacionOrigen: lectura.UbicacionOrigen?.Codigo,
        IdUbicacionDestino: lectura.IdUbicacionDestino,
        UbicacionDestino: lectura.UbicacionDestino?.Codigo,
        FechaRegistroUtc: lectura.FechaRegistroUtc);
}
