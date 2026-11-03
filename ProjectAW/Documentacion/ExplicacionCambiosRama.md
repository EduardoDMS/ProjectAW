# Explicación funcional de los cambios de la rama

## Alcance de esta guía

Esta guía revisa la diferencia de `main` a `josue-dev` disponible en Git. En esa comparación aparecen aproximadamente 404 archivos modificados o incorporados. Incluye código de varias personas y merges; Git no permite atribuir todos esos cambios a una sola persona. Por eso este documento explica el flujo funcional de la rama y sus clases principales, no afirma que cada archivo haya sido creado por Codex.

Además, al preparar esta guía se hicieron dos ajustes locales recientes que todavía no forman parte de esa comparación de commits:

- En `Modules/Stocks/Services/MovimientoStockService.cs`, se simplificó la validación final de ubicaciones y se añadieron los auxiliares `ValidarUbicacionOrigenLeida` y `ValidarUbicacionDestinoLeida`.
- En `Modules/Guias/Repositories/GuiaRepository.cs`, se ajustó `ObtenerSiguienteNumeroAsync` para leer una secuencia SQL Server sin que EF componga `NEXT VALUE FOR` dentro de una subconsulta.

## Mapa general de comunicación

```text
PWA / vistas MVC
       ↓ HTTP
Controllers (rutas, entrada y respuesta HTTP)
       ↓ interfaces
Services (reglas y coordinación)
       ├── Repositories (consultas y persistencia EF Core)
       └── Stock Processors (reglas concretas de entrada, salida o traslado)
                    ↓
          StockBalanceRepository (saldos por ubicación/almacén)
                    ↓
              AppDbContext / SQL Server
```

Los DTO representan los datos que cruzan la API. Las entidades representan los registros y relaciones persistidos. Las interfaces permiten que servicios y controladores dependan de contratos, en vez de construir consultas SQL o EF directamente.

## Guías y motivos de guía

### Clases principales

- `GuiaController` y `GuiasApiController`: reciben solicitudes para las vistas y la API, y delegan el trabajo a `IGuiaService`.
- `GuiaService`: aplica las reglas para crear, consultar, actualizar y cancelar guías; coordina el repositorio y los saldos cuando la operación requiere validarlos.
- `GuiaRepository`: obtiene y guarda guías con sus relaciones. `ConsultaGuiaCompleta` reúne los `Include` comunes a las lecturas de guía.
- `MotivosGuiaApiController` y `MotivoGuiaService`: administran los motivos y sus comportamientos configurables.
- `MotivoGuiaRepository`: consulta motivos, catálogos y referencias de uso; guarda o elimina el motivo solicitado.
- `CabeceraGuia`, `DetalleGuia`, `MotivoGuia`, `TipoComportamientoGuia` y sus configuraciones: modelan la cabecera, productos, reglas configurables y relaciones de base de datos.
- Los DTO de `Modules/Guias/DTOs`: validan o transportan solicitudes y respuestas sin exponer directamente las entidades.

### Métodos relevantes de `GuiaService`

- `CrearAsync`: valida la solicitud y sus referencias, solicita al repositorio el siguiente número, arma la cabecera y detalles, guarda la guía y devuelve su respuesta.
- `ObtenerPorIdAsync` y `ListarAsync`: piden guías al repositorio y transforman entidades a `GuiaResponse`.
- `ActualizarAsync`: obtiene la guía, verifica que pueda modificarse, valida las referencias y reemplaza los datos permitidos.
- `CancelarAsync`: valida la situación de la guía y aplica el cambio de estado de cancelación.
- `CrearGuiaRelacionadaAsync`, `ObtenerMotivoEntradaEspejoAsync` y `CrearEntradaRelacionadaAsync`: coordinan la guía complementaria cuando una operación de salida requiere una entrada relacionada. Buscan el motivo de entrada aplicable, asignan otro número y vinculan la guía nueva con la original.
- `ValidarReferenciasAsync`, `ValidarDisponibilidadAsync`, `ObtenerOFallarAsync` y `ObtenerMotivoActivoAsync`: concentran comprobaciones auxiliares para que los métodos públicos mantengan un flujo legible.
- `RegistrarModificacion`: actualiza los datos de auditoría de modificación.

### `GuiaRepository.ObtenerSiguienteNumeroAsync`

Selecciona una secuencia según el tipo de operación (`ENT`, `SAL` o `TRA`) y devuelve un número con seis dígitos. El servicio de guías lo usa al crear guías. Este método solo obtiene el valor de la secuencia; no guarda por sí mismo la guía.

La consulta se lee con `ToListAsync` y luego `Single` en memoria. `SingleAsync` agregaba composición SQL y ocasionaba el error de SQL Server: `NEXT VALUE FOR` no se puede usar dentro de una subconsulta.

## Inventario, lecturas y movimientos

### Clases y responsabilidades

- `LecturasApiController`: expone las acciones de operador para consultar guías, tomarlas, registrar/eliminar lecturas y finalizar.
- `MovimientosApiController`: expone la consulta del historial de movimientos.
- `MovimientoStockService`: valida permisos y reglas de lectura, coordina transacciones y decide qué procesador debe aplicar el ajuste.
- `IMovimientoStockRepository` y `MovimientoStockRepository`: consultan guías, productos, lecturas, ubicaciones y movimientos; agregan/eliminan entidades y guardan cambios.
- `IGuiaStockProcessor`: contrato común para validar una guía, indicar almacenes/ubicaciones requeridos y aplicar el cambio de stock de un producto.
- `RecepcionStockProcessor`: suma las lecturas de recepción a las ubicaciones destino.
- `DespachoStockProcessor`: descuenta las lecturas de salida de sus ubicaciones de origen.
- `TrasladoStockProcessor`: descuenta del origen y suma al destino.
- `StockService` y `StockApiController`: ofrecen consultas de saldos y disponibilidad.
- `StockRepository`: consulta los saldos que se exponen en la lista.
- `StockBalanceRepository`: lee, crea y actualiza saldos de ubicación y de almacén. Los procesadores lo utilizan para mantener ambos niveles coordinados.
- `StockAlmacen`, `StockUbicacion`, `LecturaGuia`, `MovimientoStock` y configuraciones: representan saldos agregados, saldos por ubicación, escaneos y auditoría de movimientos.

### Métodos de operador en `MovimientoStockService`

- `TomarGuiaAsync` valida que el usuario sea operador y abre una transacción serializable. `TomarGuiaEnTransaccionAsync` cambia la guía de pendiente a en proceso. `ValidarGuiaPuedeSerTomada` verifica el tipo y estado de la guía.
- `ListarMisGuiasAsync` obtiene las guías asignadas al usuario y valida los estados de filtro permitidos.
- `ObtenerMiGuiaAsync` obtiene una guía visible para ese operador, valida el tipo de operación y construye el DTO.
- `RegistrarLecturaAsync` valida al operador y delega el registro transaccional. `RegistrarLecturaEnTransaccionAsync` comprueba la asignación/estado, el producto y las ubicaciones, y solo entonces agrega y guarda la lectura.
- `ValidarProductoLeidoAsync` confirma que el producto pertenezca a la guía y que el código leído corresponda al producto.
- `ValidarDisponibilidadSalidaAsync` compara stock existente con cantidad ya leída más la cantidad nueva, para impedir exceder las unidades disponibles.
- `CrearLectura` transforma la solicitud en entidad y normaliza campos opcionales como lote, serie y observación.
- `ListarLecturasAsync` obtiene el historial de lecturas del operador y lo convierte a DTO.
- `EliminarLecturaAsync` vuelve a validar la guía en una transacción, comprueba que la lectura pertenezca al operador y la elimina si existe.
- `FinalizarLecturaAsync` ejecuta todo el cierre dentro de una transacción serializable.
- `FinalizarLecturaEnTransaccionAsync` valida la guía y lecturas, aplica el inventario mediante el procesador, registra un movimiento por producto, marca la guía atendida y guarda.
- `ValidarGuiaParaFinalizar` comprueba tipo, detalles, procesador, cantidades enteras y asignación del operador.
- `ObtenerLecturasValidadasAsync` verifica que todos los productos tengan lectura y que las ubicaciones continúen válidas.
- `AplicarLecturasAsync` itera los detalles de la guía. `AplicarLecturasDelProductoAsync` suma lecturas del producto, verifica el límite numérico, llama al procesador y agrega el movimiento histórico.
- `ListarAsync` consulta los movimientos con los filtros recibidos y devuelve DTOs.

### Validación de ubicaciones en `MovimientoStockService`

- `ValidarUbicacionesAsync` decide los requisitos para capturar una lectura: origen, destino o ubicación de despacho, según procesador y guía.
- `RequiereUbicacionDespachoAsync` consulta si el almacén de salida tiene ubicaciones activas habilitadas para despacho.
- `ValidarUbicacionesIndicadas` comprueba que la solicitud incluya exactamente las ubicaciones requeridas.
- `ValidarUbicacionOrigenAsync`, `ValidarUbicacionDespachoAsync` y `ValidarUbicacionDestinoAsync` verifican que las ubicaciones pertenezcan al almacén correspondiente y tengan el uso requerido.
- `ValidarUbicacionesFinalesAsync` reúne los IDs únicos de ubicación de todas las lecturas, consulta ubicaciones válidas por lote y delega la comprobación por lectura. Consultar en lote evita una consulta a la base de datos por cada lectura.
- `ObtenerUbicacionesOrigenValidasAsync` y `ObtenerUbicacionesDestinoValidasAsync` preparan esas consultas por lote.
- `ValidarLecturasContraUbicaciones` recorre las lecturas una sola vez y delega cada una a `ValidarUbicacionLeida`.
- `ValidarUbicacionLeida` comprueba que una lectura tenga las ubicaciones requeridas y delega la validación de origen y destino.
- `ValidarUbicacionOrigenLeida` (auxiliar agregado en el ajuste local reciente) comprueba pertenencia y actividad de la ubicación origen.
- `ValidarUbicacionDestinoLeida` (auxiliar agregado en el ajuste local reciente) comprueba destino válido y, en salida, habilitación de despacho.
- `UbicacionActivaAsync` comprueba una ubicación puntual durante el registro de una lectura.

### Cómo se conectan los procesadores con el saldo

El servicio común no contiene el algoritmo propio de cada operación: busca en `_stockProcessors` un `IGuiaStockProcessor` por tipo de operación. Al cerrar, llama `AplicarProductoAsync` una vez por producto.

- `RecepcionStockProcessor.ValidarGuia` verifica condiciones de recepción; `ObtenerAlmacenMovimiento` indica el almacén destino; `AplicarProductoAsync` y `SumarLecturasPorUbicacionDestinoAsync` aumentan saldos.
- `DespachoStockProcessor.ValidarGuia` verifica datos de salida; `ObtenerAlmacenMovimiento` devuelve almacén origen; `AplicarProductoAsync` delega el descuento por ubicación a `DescontarLecturasPorUbicacionAsync`.
- `TrasladoStockProcessor.ValidarGuia` verifica ambos almacenes; `ObtenerAlmacenMovimiento` define el almacén registrado en el historial; `AplicarProductoAsync` coordina `DescontarLecturasPorUbicacionOrigenAsync` y `SumarLecturasPorUbicacionDestinoAsync`.

Estos métodos llaman a `StockBalanceRepository` para leer el saldo afectado y crearlo o actualizarlo. El cierre transaccional asegura que el saldo, el movimiento histórico y el estado de la guía se confirmen juntos o se reviertan juntos.

**Detalle que conviene revisar:** el flujo actual de `GuiaService` solo permite operaciones Entrada y Salida; un traslado interno se representa como una Salida y una Entrada relacionada. Sin embargo, `TrasladoStockProcessor` todavía existe y está registrado en `Program.cs` para el tipo de operación 3. En el flujo nuevo no parece ser seleccionado para nuevas guías, pero podría seguir atendiendo registros antiguos de tipo 3. No lo eliminé: antes habría que decidir si esos registros históricos todavía deben procesarse.

### `StockService`

- `ListarAsync` recibe filtros de almacén, producto y ubicación, delega al repositorio y convierte resultados a `StockAlmacenDto`.
- `ObtenerDisponibilidadAsync` retorna una asociación producto→cantidad para consultar varios productos en una sola petición.

`StockApiController` traduce la petición HTTP a esas llamadas. `IReadOnlyDictionary<int,long>` representa los resultados por ID de producto y permite consultarlos por clave sin recorrer la lista completa.

## Almacenes y ubicaciones de despacho

- `AlmacenApiController` y `AlmacenController`: API y vistas del catálogo de almacenes.
- `AlmacenService`: crea, consulta, actualiza y cambia el estado de almacenes; valida códigos únicos y ubicaciones configuradas para despacho.
- `AlmacenRepository`: ejecuta consultas del almacén y mantiene la asignación de ubicaciones de despacho.
- `UbicacionDespacho` y su configuración: relacionan ubicaciones que el almacén permite usar para salida.
- `UbicacionService`, `UbicacionRepository`, controladores y DTO de Ubicaciones: mantienen el catálogo de ubicaciones y exponen operaciones por API/vistas.

`ListarUbicacionesDelAlmacenAsync`, `EliminarUbicacionesDespachoAsync` y `AgregarUbicacionesDespachoAsync` leen o actualizan la relación. El servicio valida primero las ubicaciones y el repositorio persiste la asignación. Luego el procesador de despacho solo permite lecturas desde ubicaciones marcadas para salida.

## Autenticación y usuarios

- `AuthApiController`: recibe login/logout para clientes API/PWA, determina el esquema de autenticación y establece o elimina cookies.
- `AuthController`: presenta vistas de inicio de sesión y acceso denegado, y limita redirecciones a destinos locales.
- `AuthService.ValidarCredencialAsync`: coordina la validación de credenciales y devuelve la sesión si son válidas.
- `AuthRepository.ObtenerPorUsernameAsync`: recupera el usuario y datos necesarios para validar credenciales.
- `IAuthService` e `IAuthRepository`: contratos que separan endpoint, regla de autenticación y persistencia.
- `CurrentUserService` e `ICurrentUserService`: extraen la identidad/claims de la solicitud actual para servicios que necesitan conocer usuario o permisos.
- `AppClaimTypes` y `AuthCookieSchemes`: centralizan nombres de claims y esquemas para evitar diferencias entre clientes.
- `UsuarioApiController` y `UsuarioController`: API y vistas de administración.
- `UsuarioService`: crea, consulta, actualiza y cambia estado de usuarios; `ValidarRolAsync` comprueba que el rol exista antes de asignarlo.
- `UsuarioRepository`: implementa consultas y persistencia de usuarios; DTOs evitan entregar la entidad completa a la API.

## Maestros

Las carpetas de Categorías, Clientes, Productos, Proveedores, Roles, Tipos de documento, Unidades de medida y Ubicaciones siguen el patrón de entidades/configuración, DTOs, repositorio/interfaz, servicio/interfaz y controlador cuando corresponde. Sus controladores reciben y responden HTTP; sus servicios validan reglas de catálogo y transforman entidades a DTO; sus repositorios consultan y guardan en EF Core.

Los cambios de la rama también incorporan o amplían relaciones entre estos catálogos: por ejemplo producto-categoría, almacén-ubicación y relaciones de usuarios con roles. Las configuraciones EF describen claves, índices y relaciones; las migraciones aplican esos cambios a SQL Server.

## PWA, vistas y API

La rama incorpora `ProjectAW.PWA` con páginas HTML/JS para autenticación y operación de guías, lecturas, ajustes y consultas. `shared/js/api-client.js` centraliza llamadas HTTP; `session.js` mantiene estado de sesión; `navigation.js` organiza navegación; las pantallas específicas invocan la API.

Los controladores API traducen las solicitudes a servicios, respetando códigos y formatos HTTP. Las vistas MVC del proyecto web sirven la administración tradicional. Los archivos CSS, iconos y bibliotecas son presentación/recursos del cliente, no lógica de inventario.

## Persistencia y migraciones

`AppDbContext` registra entidades y secuencias. Las migraciones bajo `Migrations/` crean o cambian tablas, relaciones, índices, catálogos y secuencias para guías. `AppDbContextModelSnapshot` representa el modelo EF al momento de la última migración.

Al revisar un despliegue, hay que comprobar que las migraciones aplicadas correspondan al mismo commit de la aplicación. Cambiar entidades/configuraciones sin aplicar migración puede dejar el modelo de EF y la base SQL desincronizados.

## Qué se comprobó y qué falta

- Se consultó el historial de la rama y su comparación `main...josue-dev`; el alcance incluye cambios de más de una contribución y no permite atribuir individualmente todos los archivos.
- La compilación del proyecto pasó después de los dos ajustes locales recientes; reportó tres advertencias CS8601 en `ProductoService.cs` y cero errores.
- No se ejecutaron pruebas automatizadas ni se validó una operación contra una base de datos real durante la elaboración de esta guía.
- Por esa razón, la guía describe el funcionamiento visible en código y contratos; no certifica el comportamiento de un entorno desplegado ni los datos reales de la base.
