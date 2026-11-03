using ProjectAW.Models;
using ProjectAW.Infrastructure.Data;
using ProjectAW.Modules.Guias.DTOs.Request;
using ProjectAW.Modules.Guias.DTOs.Response;
using ProjectAW.Modules.Guias.Entities;
using ProjectAW.Modules.Guias.Repositories;
using ProjectAW.Modules.Maestros.Almacenes.Entities;
using ProjectAW.Modules.Maestros.Clientes.Entities;
using ProjectAW.Modules.Maestros.Estados.Entities;
using ProjectAW.Modules.Maestros.Productos.Entities;
using ProjectAW.Modules.Maestros.Proveedores.Entities;
using ProjectAW.Modules.Maestros.Operaciones.Entities;
using ProjectAW.Modules.Seguridad.Security.HttpAuth;
using ProjectAW.Infrastructure.Errors;
using ProjectAW.Modules.Stocks.Services;


namespace ProjectAW.Modules.Guias.Services
{

    public class GuiaService : IGuiaService
    {
        private readonly IGuiaRepository _repo;
        private readonly ICurrentUserService _currentUser;
        private readonly IStockService _stockService;
        private readonly IMotivoGuiaRepository _motivos;
        private readonly ITransactionManager _transactions;

        public GuiaService(
            IGuiaRepository repo,
            ICurrentUserService currentUser,
            IStockService stockService,
            IMotivoGuiaRepository motivos,
            ITransactionManager transactions)
        {
            _repo = repo;
            _currentUser = currentUser;
            _stockService = stockService;
            _motivos = motivos;
            _transactions = transactions;
        } 

        public async Task<GuiaResponse> CrearAsync(CrearGuiaRequest request, CancellationToken ct = default)
        {
            // El motivo define qué datos son obligatorios; después se validan detalles, referencias y stock.
            var motivo = await ObtenerMotivoActivoAsync(request.IdMotivoGuia, request.IdTipoOperacionGuia, ct);
            ValidarCabecera(motivo, request);
            ValidarDetalles(request.Detalles);
            await ValidarReferenciasAsync(request, ct);
            await ValidarDisponibilidadAsync(request.IdTipoOperacionGuia, motivo, request, ct);

            var guia = new CabeceraGuia
            {
                NumeroGuia = await _repo.ObtenerSiguienteNumeroAsync(request.IdTipoOperacionGuia, ct),
                IdTipoOperacionGuia = request.IdTipoOperacionGuia,
                IdMotivoGuia = motivo.IdMotivoGuia,
                IdEstadoGuia = EstadoGuiasIds.Pendiente,
                IdUsuarioRegistro = _currentUser.IdUsuario
            };
            AplicarDatos(guia, request, motivo);

            if (motivo.IdTipoComportamiento == TipoComportamientoGuiaIds.TrasladoInterno)
            {
                // El traslado necesita crear juntas la salida y su entrada espejo.
                await CrearGuiaRelacionadaAsync(guia, request, ct);
            }
            else
            {
                await _repo.AgregarAsync(guia, ct);
                await _repo.GuardarCambiosAsync(ct);
            }

            // Vuelve a consultar la guía con sus relaciones para devolver el mismo modelo de detalle.
            return await ObtenerPorIdAsync(guia.IdGuia, ct);
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
            // Solo una guía pendiente y no relacionada puede editarse individualmente.
            var guia = await ObtenerOFallarAsync(idGuia, ct);
            Exigir(guia.IdEstadoGuia == EstadoGuiasIds.Pendiente, "Solo se puede editar una guía en estado Pendiente.");

            if (guia.IdGuiaRelacionada.HasValue)
                throw new ReglaNegocioException("Esta guía pertenece a un traslado interno relacionado. Cancela ambas guías y crea nuevamente el movimiento para conservar su coherencia.");
            var motivo = await ObtenerMotivoActivoAsync(request.IdMotivoGuia, guia.IdTipoOperacionGuia, ct);
            ValidarCabecera(motivo, request);
            ValidarDetalles(request.Detalles);
            await ValidarReferenciasAsync(request, ct);
            await ValidarDisponibilidadAsync(guia.IdTipoOperacionGuia, motivo, request, ct);

            AplicarDatos(guia, request, motivo);
            RegistrarModificacion(guia);
            await _repo.GuardarCambiosAsync(ct);
           
            return await ObtenerPorIdAsync(guia.IdGuia, ct);
        }

        public async Task CancelarAsync(int idGuia, CancellationToken ct = default)
        {
            // Las guías relacionadas se cancelan juntas y solo mientras ambas sigan pendientes.
            var guia = await ObtenerOFallarAsync(idGuia, ct);
            Exigir(guia.IdEstadoGuia == EstadoGuiasIds.Pendiente, "Solo se puede cancelar una guía en estado Pendiente.");
            if (guia.IdGuiaRelacionada.HasValue)
            {
                var relacionada = await ObtenerOFallarAsync(guia.IdGuiaRelacionada.Value, ct);
                Exigir(relacionada.IdEstadoGuia == EstadoGuiasIds.Pendiente, "No se pueden cancelar las dos guías: la guía relacionada ya está en proceso o atendida.");
                relacionada.IdEstadoGuia = EstadoGuiasIds.Cancelada;
                RegistrarModificacion(relacionada);
            }
            RegistrarModificacion(guia);
            guia.IdEstadoGuia = EstadoGuiasIds.Cancelada;
            await _repo.GuardarCambiosAsync(ct);
        }

        /// <summary>
        /// Un traslado interno se representa con una salida y una entrada espejo.
        /// Ambas guías se guardan juntas para que nunca quede registrado solo un lado.
        /// </summary>
        private async Task CrearGuiaRelacionadaAsync(
            CabeceraGuia guia, CrearGuiaRequest request, CancellationToken ct)
        {
            var motivoEntrada = await ObtenerMotivoEntradaEspejoAsync(ct);
            var entrada = await CrearEntradaRelacionadaAsync(guia, request, motivoEntrada, ct);

            // Genera primero ambos identificadores y luego persiste las referencias cruzadas.
            await _transactions.ExecuteAsync(async transactionCt =>
            {
                await _repo.AgregarAsync(guia, transactionCt);
                await _repo.AgregarAsync(entrada, transactionCt);
                await _repo.GuardarCambiosAsync(transactionCt);

                guia.IdGuiaRelacionada = entrada.IdGuia;
                entrada.IdGuiaRelacionada = guia.IdGuia;
                await _repo.GuardarCambiosAsync(transactionCt);
            }, ct);

            guia.GuiaRelacionada = entrada;
            entrada.GuiaRelacionada = guia;
        }

        private async Task<MotivoGuia> ObtenerMotivoEntradaEspejoAsync(CancellationToken ct)
        {
            var motivos = (await _motivos.ListarAsync(TipoOperacionGuiaIds.Entrada, true, ct))
                .Where(motivo => motivo.IdTipoComportamiento == TipoComportamientoGuiaIds.MovimientoNormal &&
                    motivo.RequiereAlmacenDestino && !motivo.RequiereAlmacenOrigen &&
                    !motivo.RequiereProveedor && !motivo.RequiereCliente)
                .ToList();

            if (motivos.Count != 1)
                throw new ReglaNegocioException("Para crear un traslado interno configura exactamente un motivo activo de Entrada normal que requiera almacén destino, sin proveedor, cliente ni almacén de origen.");

            return motivos[0];
        }

        private async Task<CabeceraGuia> CrearEntradaRelacionadaAsync(
            CabeceraGuia guia, CrearGuiaRequest request, MotivoGuia motivoEntrada, CancellationToken ct)
        {
            // La entrada espejo comparte productos, cantidades, fecha y referencia de la salida.
            var entrada = new CabeceraGuia
            {
                NumeroGuia = await _repo.ObtenerSiguienteNumeroAsync(TipoOperacionGuiaIds.Entrada, ct),
                IdTipoOperacionGuia = TipoOperacionGuiaIds.Entrada,
                IdMotivoGuia = motivoEntrada.IdMotivoGuia,
                IdEstadoGuia = EstadoGuiasIds.Pendiente,
                IdUsuarioRegistro = _currentUser.IdUsuario,
                IdAlmacenDestino = request.IdAlmacenDestino,
                FechaProgramada = request.FechaProgramada,
                TipoDocumentoReferencia = request.TipODocumentoReferencia,
                NumeroDocumentoReferencia = request.NumeroDocumentoReferencia,
                Observacion = $"Entrada relacionada a {guia.NumeroGuia}. {request.Observacion}".Trim()
            };

            foreach (var detalle in request.Detalles)
                entrada.Detalles.Add(new DetalleGuia
                {
                    IdProducto = detalle.IdProducto,
                    CantidadEsperada = detalle.CantidadEsperada
                });

            return entrada;
        }


        /// <summary>Valida que los datos de cabecera coincidan con el motivo antes de persistir.</summary>
        private static void ValidarCabecera(MotivoGuia motivo, ActualizarGuiaRequest r)
        {
            Exigir(motivo.Activo, "El motivo está inactivo y no se puede usar en nuevas guías.");
            Exigir(r.IdProveedor.HasValue == motivo.RequiereProveedor, motivo.RequiereProveedor ? "El motivo requiere proveedor." : "El motivo seleccionado no admite proveedor.");
            Exigir(r.IdCliente.HasValue == motivo.RequiereCliente, motivo.RequiereCliente ? "El motivo requiere cliente." : "El motivo seleccionado no admite cliente.");
            Exigir(r.IdAlmacenOrigen.HasValue == motivo.RequiereAlmacenOrigen, motivo.RequiereAlmacenOrigen ? "El motivo requiere almacén de origen." : "El motivo seleccionado no admite almacén de origen.");
            Exigir(r.IdAlmacenDestino.HasValue == motivo.RequiereAlmacenDestino, motivo.RequiereAlmacenDestino ? "El motivo requiere almacén de destino." : "El motivo seleccionado no admite almacén de destino.");
            var idTipoOperacionGuia = motivo.IdTipoOperacionGuia;
            Exigir(idTipoOperacionGuia is TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Salida, "Traslado ya no es una operación disponible.");
            if (motivo.IdTipoComportamiento == TipoComportamientoGuiaIds.TrasladoInterno)
                Exigir(idTipoOperacionGuia == TipoOperacionGuiaIds.Salida && r.IdAlmacenOrigen != r.IdAlmacenDestino, "Un traslado interno requiere una salida y dos almacenes distintos.");
            Exigir(r.FechaProgramada != default, "La fecha programada es obligatoria.");
            Exigir(r.FechaProgramada.Date >= DateTime.Today,
                "La fecha programada no puede ser anterior a hoy.");

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

            // Comprueba cada producto una vez aunque el request repita el identificador.
            foreach (var idProducto in r.Detalles.Select(d => d.IdProducto).Distinct())
                Exigir(await _repo.ExisteAsync<Producto>(idProducto, ct), $"El producto {idProducto} no existe.");
        }

        private async Task ValidarDisponibilidadAsync(
            int idTipoOperacionGuia, MotivoGuia motivo, ActualizarGuiaRequest request, CancellationToken ct)
        {
            // La disponibilidad previa solo aplica a salidas que descuentan stock del almacén origen.
            if (idTipoOperacionGuia != TipoOperacionGuiaIds.Salida || !motivo.RequiereAlmacenOrigen)
                return;

            // ValidarCabecera garantiza almacén de origen para salida y traslado.
            var idAlmacenOrigen = request.IdAlmacenOrigen!.Value;
            var idsProductos = request.Detalles.Select(d => d.IdProducto).Distinct().ToArray();
            var disponibilidad = await _stockService.ObtenerDisponibilidadAsync(idAlmacenOrigen, idsProductos, ct);

            foreach (var detalle in request.Detalles)
            {
                var disponible = disponibilidad.TryGetValue(detalle.IdProducto, out var cantidad)
                    ? cantidad
                    : 0;

                Exigir(disponible >= detalle.CantidadEsperada,
                    $"Stock insuficiente para el producto {detalle.IdProducto} en el almacén {idAlmacenOrigen}. " +
                    $"Disponible: {disponible}; solicitado: {detalle.CantidadEsperada}.");
            }
        }

        private static void Exigir(bool condicion, string mensaje)
        {
            if (!condicion) throw new ReglaNegocioException(mensaje);
        }

        private async Task<CabeceraGuia> ObtenerOFallarAsync(int idGuia, CancellationToken ct) =>
            await _repo.ObtenerPorIdAsync(idGuia, ct)
            ?? throw new NoEncontradoException($"La guía {idGuia} no existe.");

        private static void AplicarDatos(CabeceraGuia guia, ActualizarGuiaRequest r, MotivoGuia motivo)
        {
            guia.IdMotivoGuia = motivo.IdMotivoGuia;
            guia.Motivo = motivo;
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
            g.IdGuia, g.NumeroGuia, g.IdTipoOperacionGuia, g.IdMotivoGuia, g.Motivo?.NombreMotivo,
            RequiereProveedor(g), RequiereCliente(g), RequiereAlmacenOrigen(g), RequiereAlmacenDestino(g),
            TipoTercero(g), NombreTercero(g), NombreAlmacen(g.AlmacenOrigen), NombreAlmacen(g.AlmacenDestino),
            g.IdGuiaRelacionada, g.GuiaRelacionada?.NumeroGuia, g.IdEstadoGuia,
            g.IdProveedor, g.IdCliente, g.IdAlmacenOrigen, g.IdAlmacenDestino,
            g.FechaProgramada, g.FechaRegistro,
            g.IdUsuarioRegistro, g.IdUsuarioModificacion, g.FechaActualizada,
            g.TipoDocumentoReferencia, g.NumeroDocumentoReferencia, g.Observacion,
            g.Detalles.Select(d => new DetalleGuiaResponse(d.IdGuiaDetalle, d.IdProducto, d.CantidadEsperada)).ToList());

        private static bool RequiereProveedor(CabeceraGuia g) => g.Motivo?.RequiereProveedor ?? g.IdTipoOperacionGuia == TipoOperacionGuiaIds.Entrada;
        private static bool RequiereCliente(CabeceraGuia g) => g.Motivo?.RequiereCliente ?? g.IdTipoOperacionGuia == TipoOperacionGuiaIds.Salida;
        private static bool RequiereAlmacenOrigen(CabeceraGuia g) => g.Motivo?.RequiereAlmacenOrigen ?? g.IdTipoOperacionGuia is TipoOperacionGuiaIds.Salida or TipoOperacionGuiaIds.Traslado;
        private static bool RequiereAlmacenDestino(CabeceraGuia g) => g.Motivo?.RequiereAlmacenDestino ?? g.IdTipoOperacionGuia is TipoOperacionGuiaIds.Entrada or TipoOperacionGuiaIds.Traslado;
        private static string? TipoTercero(CabeceraGuia g) => RequiereProveedor(g) ? "Proveedor" : RequiereCliente(g) ? "Cliente" : null;
        private static string? NombreTercero(CabeceraGuia g) => RequiereProveedor(g) ? g.Proveedor?.RazonSocial : RequiereCliente(g) ? g.Cliente?.RazonSocial : null;
        private static string? NombreAlmacen(ProjectAW.Modules.Maestros.Almacenes.Entities.Almacen? almacen) => almacen is null ? null : string.Join(" · ", new[] { almacen.Codigo, almacen.Descripcion }.Where(x => !string.IsNullOrWhiteSpace(x)));

        private void RegistrarModificacion(CabeceraGuia guia)
        {
            guia.IdUsuarioModificacion = _currentUser.IdUsuario;
            guia.FechaActualizada = DateTime.UtcNow;
        }

        private async Task<MotivoGuia> ObtenerMotivoActivoAsync(int idMotivo, int idOperacion, CancellationToken ct)
        {
            var motivo = await _motivos.ObtenerAsync(idMotivo, ct) ?? throw new NoEncontradoException($"El motivo {idMotivo} no existe.");
            Exigir(motivo.Activo, "El motivo seleccionado está inactivo.");
            Exigir(motivo.IdTipoOperacionGuia == idOperacion, "El motivo no corresponde al tipo de operación seleccionado.");
            return motivo;
        }
    }
}
