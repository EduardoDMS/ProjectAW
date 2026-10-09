using ProjectAW.Models;
using ProjectAW.Modules.Guias.DTOs.Request;
using ProjectAW.Modules.Guias.DTOs.Response;
using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Guias.Repositories;
using ProjectAW.Modules.Maestros.Almacenes.Entities;
using ProjectAW.Modules.Maestros.Clientes.Entities;
using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Maestros.Productos.Entities;
using ProjectAW.Modules.Maestros.Proveedores.Entities;
//using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;


namespace ProjectAW.Modules.Guias.Services
{

    public class GuiaService : IGuiaService
    {
        private readonly IGuiaRepository _repo;

        public GuiaService(IGuiaRepository repo) => _repo = repo;

        public async Task<GuiaResponse> CrearAsync(CrearGuiaRequest request, CancellationToken ct = default)
        {
            ValidarCabecera(request.IdTipoOperacionGuia, request);
            ValidarDetalles(request.Detalles);
            await ValidarReferenciasAsync(request, ct);

            var guia = new CabeceraGuia
            {
                NumeroGuia = await _repo.ObtenerSiguienteNumeroAsync(request.IdTipoOperacionGuia, ct),
                IdTipoOperacionGuia = request.IdTipoOperacionGuia,
                IdEstadoGuia = EstadoGuiasIds.Pendiente
            };
            AplicarDatos(guia, request);

            await _repo.AgregarAsync(guia, ct);
            await _repo.GuardarCambiosAsync(ct);

            return ToResponse(guia);
        }

        public async Task<GuiaResponse> ObtenerPorIdAsync(int idGuia, CancellationToken ct = default) =>
            ToResponse(await ObtenerOFallarAsync(idGuia, ct));

        public async Task<IReadOnlyList<GuiaResponse>> ListarAsync(
            int? idTipoOperacionGuia, int? idEstadoGuia, CancellationToken ct = default)
        {
            var guias = await _repo.ListarAsync(idTipoOperacionGuia, idEstadoGuia, ct);
            return guias.Select(ToResponse).ToList();
        }

        public async Task<GuiaResponse> ActualizarAsync(int idGuia, ActualizarGuiaRequest request, CancellationToken ct = default)
        {
            var guia = await ObtenerOFallarAsync(idGuia, ct);
            Exigir(guia.IdEstadoGuia == EstadoGuiasIds.Pendiente, "Solo se puede editar una guía en estado Pendiente.");

            ValidarCabecera(guia.IdTipoOperacionGuia, request);
            ValidarDetalles(request.Detalles);
            await ValidarReferenciasAsync(request, ct);

            AplicarDatos(guia, request);
            await _repo.GuardarCambiosAsync(ct);

            return ToResponse(guia);
        }

        public async Task CancelarAsync(int idGuia, CancellationToken ct = default)
        {
            var guia = await ObtenerOFallarAsync(idGuia, ct);
            Exigir(guia.IdEstadoGuia == EstadoGuiasIds.Pendiente, "Solo se puede cancelar una guía en estado Pendiente.");

            guia.IdEstadoGuia = EstadoGuiasIds.Cancelada;
            await _repo.GuardarCambiosAsync(ct);
        }


        private static void ValidarCabecera(int idTipoOperacionGuia, ActualizarGuiaRequest r)
        {
            Exigir(r.FechaProgramada != default, "La fecha programada es obligatoria.");

            switch (idTipoOperacionGuia)
            {
                case TipoOperacionGuiaIds.Entrada:
                    Exigir(r.IdProveedor.HasValue, "La entrada requiere un proveedor.");
                    Exigir(r.IdAlmacenDestino.HasValue, "La entrada requiere un almacén de destino.");
                    Exigir(!r.IdCliente.HasValue && !r.IdAlmacenOrigen.HasValue,
                        "La entrada no admite cliente ni almacén de origen.");
                    break;

                case TipoOperacionGuiaIds.Salida:
                    Exigir(r.IdAlmacenOrigen.HasValue, "La salida requiere un almacén de origen.");
                    Exigir(r.IdCliente.HasValue, "La salida requiere un cliente.");
                    Exigir(!r.IdProveedor.HasValue && !r.IdAlmacenDestino.HasValue,
                        "La salida no admite proveedor ni almacén de destino.");
                    break;

                case TipoOperacionGuiaIds.Traslado:
                    Exigir(r.IdAlmacenOrigen.HasValue && r.IdAlmacenDestino.HasValue,
                        "El traslado requiere almacén de origen y de destino.");
                    Exigir(r.IdAlmacenOrigen != r.IdAlmacenDestino,
                        "El almacén de origen y de destino deben ser distintos.");
                    Exigir(!r.IdProveedor.HasValue && !r.IdCliente.HasValue,
                        "El traslado no admite proveedor ni cliente.");
                    break;

                default:
                    throw new ReglaNegocioException("Tipo de operación no válido.");
            }
        }

        private static void ValidarDetalles(IReadOnlyCollection<DetalleGuiaRequest>? detalles)
        {
            if (detalles is null || detalles.Count == 0)
                throw new ReglaNegocioException("La guía debe tener al menos un producto.");

            Exigir(detalles.All(d => d.CantidadEsperada > 0), "Todas las cantidades deben ser mayores a cero.");
            Exigir(detalles.GroupBy(d => d.IdProducto).All(g => g.Count() == 1),
                "Un producto no puede repetirse en la misma guía.");
        }

        private async Task ValidarReferenciasAsync(ActualizarGuiaRequest r, CancellationToken ct)
        {
            if (r.IdProveedor is int idProveedor)
                Exigir(await _repo.ExisteAsync<Proveedor>(idProveedor, ct), $"El proveedor {idProveedor} no existe.");
            if (r.IdCliente is int idCliente)
                Exigir(await _repo.ExisteAsync<Cliente>(idCliente, ct), $"El cliente {idCliente} no existe.");
            if (r.IdAlmacenOrigen is int idOrigen)
                Exigir(await _repo.ExisteAsync<Almacen>(idOrigen, ct), $"El almacén de origen {idOrigen} no existe.");
            if (r.IdAlmacenDestino is int idDestino)
                Exigir(await _repo.ExisteAsync<Almacen>(idDestino, ct), $"El almacén de destino {idDestino} no existe.");

            foreach (var idProducto in r.Detalles.Select(d => d.IdProducto).Distinct())
                Exigir(await _repo.ExisteAsync<Producto>(idProducto, ct), $"El producto {idProducto} no existe.");
        }

        private static void Exigir(bool condicion, string mensaje)
        {
            if (!condicion) throw new ReglaNegocioException(mensaje);
        }

        private async Task<CabeceraGuia> ObtenerOFallarAsync(int idGuia, CancellationToken ct) =>
            await _repo.ObtenerPorIdAsync(idGuia, ct)
            ?? throw new NoEncontradoException($"La guía {idGuia} no existe.");

        private static void AplicarDatos(CabeceraGuia guia, ActualizarGuiaRequest r)
        {
            guia.IdProveedor = r.IdProveedor;
            guia.IdCliente = r.IdCliente;
            guia.IdAlmacenOrigen = r.IdAlmacenOrigen;
            guia.IdAlmacenDestino = r.IdAlmacenDestino;
            guia.FechaProgramada = r.FechaProgramada;
            guia.TipoDocumentoReferencia = r.TipODocumentoReferencia;
            guia.NumeroDocumentoReferencia = r.NumeroDocumentoReferencia;
            guia.Observacion = r.Observacion;

            // Al ser relación requerida con cascade, EF elimina los detalles huérfanos al guardar.
            guia.Detalles.Clear();
            foreach (var d in r.Detalles)
                guia.Detalles.Add(new DetalleGuia { IdProducto = d.IdProducto, CantidadEsperada = d.CantidadEsperada });
        }

        private static GuiaResponse ToResponse(CabeceraGuia g) => new(
            g.IdGuia, g.NumeroGuia, g.IdTipoOperacionGuia, g.IdEstadoGuia,
            g.IdProveedor, g.IdCliente, g.IdAlmacenOrigen, g.IdAlmacenDestino,
            g.FechaProgramada, g.FechaRegistro,
            g.TipoDocumentoReferencia, g.NumeroDocumentoReferencia, g.Observacion,
            g.Detalles.Select(d => new DetalleGuiaResponse(d.IdGuiaDetalle, d.IdProducto, d.CantidadEsperada)).ToList());
    }
}
