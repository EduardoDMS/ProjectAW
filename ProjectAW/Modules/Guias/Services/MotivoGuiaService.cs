using ProjectAW.Infrastructure.Errors;
using ProjectAW.Models;
using ProjectAW.Modules.Guias.DTOs.Request;
using ProjectAW.Modules.Guias.DTOs.Response;
using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Guias.Repositories;
using ProjectAW.Modules.Maestros.Operaciones.Entities;

namespace ProjectAW.Modules.Guias.Services;

public sealed class MotivoGuiaService(IMotivoGuiaRepository repository) : IMotivoGuiaService
{
    public async Task<IReadOnlyList<MotivoGuiaResponse>> ListarAsync(int? operacion, bool soloActivos, CancellationToken ct = default) =>
        (await repository.ListarAsync(operacion, soloActivos, ct)).Select(ToResponse).ToList();
    public async Task<MotivoGuiaResponse> ObtenerAsync(int id, CancellationToken ct = default) =>
        ToResponse(await repository.ObtenerAsync(id, ct) ?? throw new NoEncontradoException($"El motivo {id} no existe."));
    public async Task<IReadOnlyList<TipoComportamientoGuiaResponse>> ListarComportamientosAsync(CancellationToken ct = default) =>
        (await repository.ListarComportamientosAsync(ct)).Select(x => new TipoComportamientoGuiaResponse(x.IdTipoComportamiento, x.NombreComportamiento, x.DescripcionComportamiento)).ToList();

    public async Task<MotivoGuiaResponse> CrearAsync(GuardarMotivoGuiaRequest request, CancellationToken ct = default)
    {
        await ValidarAsync(request, null, ct);
        var entity = new MotivoGuia(); Apply(entity, request);
        await repository.AgregarAsync(entity, ct); await repository.GuardarAsync(ct);
        return await ObtenerAsync(entity.IdMotivoGuia, ct);
    }

    public async Task<MotivoGuiaResponse> ActualizarAsync(int id, GuardarMotivoGuiaRequest request, CancellationToken ct = default)
    {
        var entity = await repository.ObtenerAsync(id, ct) ?? throw new NoEncontradoException($"El motivo {id} no existe.");
        if (EsEntradaEspejo(entity) && (!request.Activo || request.IdTipoOperacionGuia != TipoOperacionGuiaIds.Entrada || request.IdTipoComportamiento != 1 || !request.RequiereAlmacenDestino || request.RequiereAlmacenOrigen || request.RequiereProveedor || request.RequiereCliente))
            throw new ReglaNegocioException("El motivo de entrada espejo es necesario para registrar traslados internos; conserva sus campos y mantenlo activo.");
        await ValidarAsync(request, id, ct); Apply(entity, request); await repository.GuardarAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    public async Task CambiarEstadoAsync(int id, bool activo, CancellationToken ct = default)
    {
        var entity = await repository.ObtenerAsync(id, ct) ?? throw new NoEncontradoException($"El motivo {id} no existe.");
        if (!activo && EsEntradaEspejo(entity)) throw new ReglaNegocioException("El motivo de entrada espejo es necesario para registrar traslados internos; crea otro flujo antes de desactivarlo.");
        entity.Activo = activo; await repository.GuardarAsync(ct);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var entity = await repository.ObtenerAsync(id, ct) ?? throw new NoEncontradoException($"El motivo {id} no existe.");
        if (EsEntradaEspejo(entity)) throw new ReglaNegocioException("El motivo de entrada espejo es necesario para registrar traslados internos; no se puede eliminar.");
        if (await repository.EstaEnUsoAsync(id, ct)) throw new ConflictoNegocioException("No se puede eliminar un motivo que ya está asociado a una guía. Desactívalo para ocultarlo en nuevas guías.");
        repository.Eliminar(entity); await repository.GuardarAsync(ct);
    }

    private async Task ValidarAsync(GuardarMotivoGuiaRequest request, int? id, CancellationToken ct)
    {
        request.NombreMotivo = request.NombreMotivo.Trim();
        if (request.IdTipoOperacionGuia is not (TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Salida))
            throw new ReglaNegocioException("Los motivos solo pueden pertenecer a Entrada o Salida. Traslado ya no está disponible.");
        if (!await repository.ExisteOperacionAsync(request.IdTipoOperacionGuia, ct)) throw new NoEncontradoException("El tipo de operación seleccionado no existe.");
        if (!await repository.ExisteComportamientoAsync(request.IdTipoComportamiento, ct)) throw new NoEncontradoException("El comportamiento seleccionado no existe.");
        if (await repository.NombreExisteAsync(request.IdTipoOperacionGuia, request.NombreMotivo, id, ct)) throw new ConflictoNegocioException("Ya existe un motivo con ese nombre para esta operación.");
        if (request.RequiereCliente && request.RequiereProveedor) throw new ReglaNegocioException("Un motivo no puede requerir cliente y proveedor al mismo tiempo.");
        var interno = request.IdTipoComportamiento == 2;
        if (interno && (request.IdTipoOperacionGuia != TipoOperacionGuiaIds.Salida || request.RequiereCliente || request.RequiereProveedor))
            throw new ReglaNegocioException("TRASLADO_INTERNO solo puede usarse en una salida sin cliente ni proveedor.");
        if (request.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida && (!request.RequiereAlmacenOrigen || interno && !request.RequiereAlmacenDestino))
            throw new ReglaNegocioException(interno ? "Un traslado interno requiere almacén de origen y destino." : "Una salida requiere almacén de origen.");
        if (request.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada && (!request.RequiereAlmacenDestino || request.IdTipoComportamiento != 1 || request.RequiereAlmacenOrigen))
            throw new ReglaNegocioException("Un motivo de entrada debe requerir solo almacén destino y usar MOVIMIENTO_NORMAL. La entrada espejo se genera automáticamente.");
        if (request.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada && !request.RequiereProveedor && !request.RequiereCliente)
        {
            var entradas = await repository.ListarAsync(TipoOperacionGuiaIds.Entrada, true, ct);
            if (entradas.Any(x => x.IdMotivoGuia != id && x.IdTipoComportamiento == 1 && x.RequiereAlmacenDestino && !x.RequiereAlmacenOrigen && !x.RequiereProveedor && !x.RequiereCliente))
                throw new ConflictoNegocioException("Solo puede existir un motivo técnico activo para recibir traslados internos.");
        }
    }

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

    private static bool EsEntradaEspejo(MotivoGuia x) => x.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada && x.IdTipoComportamiento == 1 && x.RequiereAlmacenDestino && !x.RequiereAlmacenOrigen && !x.RequiereProveedor && !x.RequiereCliente;
}
