using ProjectAW.Infrastructure.Errors;
using ProjectAW.Models;
using ProjectAW.Modules.Guias.DTOs.Request;
using ProjectAW.Modules.Guias.DTOs.Response;
using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Guias.Repositories;
using ProjectAW.Modules.Maestros.Operaciones.Entities;

namespace ProjectAW.Modules.Guias.Services;

public sealed class MotivoGuiaService : IMotivoGuiaService
{
    private readonly IMotivoGuiaRepository _repository;

    public MotivoGuiaService(IMotivoGuiaRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<MotivoGuiaResponse>> ListarAsync(
        int? operacion, bool soloActivos, CancellationToken ct = default)
    {
        var motivos = await _repository.ListarAsync(operacion, soloActivos, ct);
        return motivos.Select(ToResponse).ToList();
    }

    public async Task<MotivoGuiaResponse> ObtenerAsync(int id, CancellationToken ct = default)
    {
        var motivo = await _repository.ObtenerAsync(id, ct)
            ?? throw new NoEncontradoException($"El motivo {id} no existe.");
        return ToResponse(motivo);
    }

    public async Task<IReadOnlyList<TipoComportamientoGuiaResponse>> ListarComportamientosAsync(
        CancellationToken ct = default)
    {
        var comportamientos = await _repository.ListarComportamientosAsync(ct);
        return comportamientos
            .Select(x => new TipoComportamientoGuiaResponse(
                x.IdTipoComportamiento, x.NombreComportamiento, x.DescripcionComportamiento))
            .ToList();
    }

    public async Task<MotivoGuiaResponse> CrearAsync(GuardarMotivoGuiaRequest request, CancellationToken ct = default)
    {
        await ValidarAsync(request, null, ct);
        var entity = new MotivoGuia();
        Apply(entity, request);
        await _repository.AgregarAsync(entity, ct);
        await _repository.GuardarAsync(ct);
        return await ObtenerAsync(entity.IdMotivoGuia, ct);
    }

    public async Task<MotivoGuiaResponse> ActualizarAsync(int id, GuardarMotivoGuiaRequest request, CancellationToken ct = default)
    {
        var entity = await _repository.ObtenerAsync(id, ct) ?? throw new NoEncontradoException($"El motivo {id} no existe.");
        if (DebePreservarEntradaEspejo(entity, request))
            throw new ReglaNegocioException("El motivo de entrada espejo es necesario para registrar traslados internos; conserva sus campos y mantenlo activo.");
        await ValidarAsync(request, id, ct);
        Apply(entity, request);
        await _repository.GuardarAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task CambiarEstadoAsync(int id, bool activo, CancellationToken ct = default)
    {
        var entity = await _repository.ObtenerAsync(id, ct) ?? throw new NoEncontradoException($"El motivo {id} no existe.");
        if (!activo && EsEntradaEspejo(entity))
            throw new ReglaNegocioException("El motivo de entrada espejo es necesario para registrar traslados internos; crea otro flujo antes de desactivarlo.");

        entity.Activo = activo;
        await _repository.GuardarAsync(ct);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.ObtenerAsync(id, ct) ?? throw new NoEncontradoException($"El motivo {id} no existe.");
        if (EsEntradaEspejo(entity))
            throw new ReglaNegocioException("El motivo de entrada espejo es necesario para registrar traslados internos; no se puede eliminar.");
        if (await _repository.EstaEnUsoAsync(id, ct))
            throw new ConflictoNegocioException("No se puede eliminar un motivo que ya está asociado a una guía. Desactívalo para ocultarlo en nuevas guías.");

        _repository.Eliminar(entity);
        await _repository.GuardarAsync(ct);
    }

    private async Task ValidarAsync(GuardarMotivoGuiaRequest request, int? id, CancellationToken ct)
    {
        // Normaliza el nombre antes de comprobar duplicados y sigue el orden de validación:
        // catálogos, reglas propias de Entrada/Salida y motivo técnico del espejo.
        request.NombreMotivo = request.NombreMotivo.Trim();
        await ValidarCatalogosAsync(request, id, ct);
        ValidarReglasDeOperacion(request);
        await ValidarMotivoEntradaTecnicoAsync(request, id, ct);
    }

    private async Task ValidarCatalogosAsync(
        GuardarMotivoGuiaRequest request, int? id, CancellationToken ct)
    {
        if (request.IdTipoOperacionGuia is not (TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Salida))
            throw new ReglaNegocioException("Los motivos solo pueden pertenecer a Entrada o Salida. Traslado ya no está disponible.");
        if (!await _repository.ExisteOperacionAsync(request.IdTipoOperacionGuia, ct))
            throw new NoEncontradoException("El tipo de operación seleccionado no existe.");
        if (!await _repository.ExisteComportamientoAsync(request.IdTipoComportamiento, ct))
            throw new NoEncontradoException("El comportamiento seleccionado no existe.");
        if (await _repository.NombreExisteAsync(request.IdTipoOperacionGuia, request.NombreMotivo, id, ct))
            throw new ConflictoNegocioException("Ya existe un motivo con ese nombre para esta operación.");
    }

    private static void ValidarReglasDeOperacion(GuardarMotivoGuiaRequest request)
    {
        // Las reglas comunes de terceros se comprueban antes de las específicas de cada operación.
        var interno = request.IdTipoComportamiento == TipoComportamientoGuiaIds.TrasladoInterno;
        ValidarTerceros(request);
        ValidarReglasSalida(request, interno);
        ValidarReglasEntrada(request);
    }

    private static void ValidarTerceros(GuardarMotivoGuiaRequest request)
    {
        if (request.RequiereCliente && request.RequiereProveedor)
            throw new ReglaNegocioException("Un motivo no puede requerir cliente y proveedor al mismo tiempo.");
    }

    private static void ValidarReglasSalida(GuardarMotivoGuiaRequest request, bool interno)
    {
        if (interno && (request.IdTipoOperacionGuia != TipoOperacionGuiaIds.Salida ||
                        request.RequiereCliente || request.RequiereProveedor))
            throw new ReglaNegocioException("TRASLADO_INTERNO solo puede usarse en una salida sin cliente ni proveedor.");

        if (request.IdTipoOperacionGuia != TipoOperacionGuiaIds.Salida)
            return;

        if (!request.RequiereAlmacenOrigen || interno && !request.RequiereAlmacenDestino)
        {
            var mensaje = interno
                ? "Un traslado interno requiere almacén de origen y destino."
                : "Una salida requiere almacén de origen.";
            throw new ReglaNegocioException(mensaje);
        }
    }

    private static void ValidarReglasEntrada(GuardarMotivoGuiaRequest request)
    {
        if (request.IdTipoOperacionGuia != TipoOperacionGuiaIds.Entrada)
            return;

        var requiereConfiguracionEntradaNormal = request.RequiereAlmacenDestino &&
            request.IdTipoComportamiento == TipoComportamientoGuiaIds.MovimientoNormal &&
            !request.RequiereAlmacenOrigen;
        if (!requiereConfiguracionEntradaNormal)
            throw new ReglaNegocioException("Un motivo de entrada debe requerir solo almacén destino y usar MOVIMIENTO_NORMAL. La entrada espejo se genera automáticamente.");
    }

    private async Task ValidarMotivoEntradaTecnicoAsync(
        GuardarMotivoGuiaRequest request, int? id, CancellationToken ct)
    {
        // El traslado interno necesita exactamente un motivo técnico activo para su entrada espejo.
        if (request.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada && !request.RequiereProveedor && !request.RequiereCliente)
        {
            // Excluye el motivo que se está editando y busca otro que cumpla la misma configuración.
            var entradas = await _repository.ListarAsync(TipoOperacionGuiaIds.Entrada, true, ct);
            if (entradas.Any(motivo => motivo.IdMotivoGuia != id && EsMotivoEntradaTecnico(motivo)))
                throw new ConflictoNegocioException("Solo puede existir un motivo técnico activo para recibir traslados internos.");
        }
    }

    private static bool EsMotivoEntradaTecnico(MotivoGuia motivo) =>
        motivo.IdTipoComportamiento == TipoComportamientoGuiaIds.MovimientoNormal &&
        motivo.RequiereAlmacenDestino &&
        !motivo.RequiereAlmacenOrigen &&
        !motivo.RequiereProveedor &&
        !motivo.RequiereCliente;

    private static void Apply(MotivoGuia entity, GuardarMotivoGuiaRequest request)
    {
        entity.NombreMotivo = request.NombreMotivo;
        entity.IdTipoOperacionGuia = request.IdTipoOperacionGuia;
        entity.RequiereProveedor = request.RequiereProveedor;
        entity.RequiereCliente = request.RequiereCliente;
        entity.RequiereAlmacenOrigen = request.RequiereAlmacenOrigen;
        entity.RequiereAlmacenDestino = request.RequiereAlmacenDestino;
        entity.IdTipoComportamiento = request.IdTipoComportamiento;
        entity.Activo = request.Activo;
    }

    private static MotivoGuiaResponse ToResponse(MotivoGuia x) => new(
        x.IdMotivoGuia, x.NombreMotivo, x.IdTipoOperacionGuia, x.TipoOperacionGuia?.Nombre ?? string.Empty,
        x.RequiereProveedor, x.RequiereCliente, x.RequiereAlmacenOrigen, x.RequiereAlmacenDestino,
        x.IdTipoComportamiento, x.TipoComportamiento?.NombreComportamiento ?? string.Empty,
        x.TipoComportamiento?.DescripcionComportamiento ?? string.Empty, x.Activo);

    private static bool EsEntradaEspejo(MotivoGuia x) =>
        x.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada &&
        EsMotivoEntradaTecnico(x);

    private static bool DebePreservarEntradaEspejo(MotivoGuia entity, GuardarMotivoGuiaRequest request) =>
        EsEntradaEspejo(entity) && !EsConfiguracionEntradaEspejo(request);

    private static bool EsConfiguracionEntradaEspejo(GuardarMotivoGuiaRequest request) =>
        request.Activo &&
        request.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada &&
        request.IdTipoComportamiento == TipoComportamientoGuiaIds.MovimientoNormal &&
        request.RequiereAlmacenDestino &&
        !request.RequiereAlmacenOrigen &&
        !request.RequiereProveedor &&
        !request.RequiereCliente;
}
