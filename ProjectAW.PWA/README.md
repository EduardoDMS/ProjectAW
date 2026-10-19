# ProjectAW.PWA

Aplicación ASP.NET Core independiente que sirve la interfaz estática de operaciones. El host no contiene controladores de negocio, autenticación propia, acceso a SQL Server ni referencia al proyecto `ProjectAW`. El navegador consume la API de `ProjectAW` mediante `fetch`, JSON y la cookie HTTP-only existente.

## Estructura

- `Program.cs`: host mínimo que redirige HTTP a HTTPS y publica archivos estáticos.
- `wwwroot/modules/seguridad/login/`: pantalla de acceso.
- `wwwroot/ modules/operaciones/inicio/`: Home con conteos reales de guías pendientes.
- `wwwroot/modules/operaciones/guias/`: listado por tipo de operación.
- `wwwroot/modules/operaciones/guias.js`: consultas y utilidades compartidas para guías.
- `wwwroot/modules/operaciones/operaciones.css`: estilos responsive comunes.
- `wwwroot/shared/js/api-client.js`: cliente HTTP central con `credentials: include`.
- `wwwroot/shared/js/session.js`: cierre de sesión y retorno al login.
- `wwwroot/config/api-config.js`: URL de la API por entorno.
- `wwwroot/css/`, `wwwroot/libs/` y `wwwroot/img/`: estilos, Bootstrap local y recursos gráficos.

Los recursos que se publican al navegador están bajo `wwwroot`. `/` abre el archivo de login. Las vistas operativas se sirven por sus rutas estáticas, por ejemplo `/modules/operaciones/inicio/index.html`.

## Ejecutar localmente

1. Abre `ProjectAW.slnx` en Visual Studio.
2. Configura varios proyectos de inicio si quieres ejecutar `ProjectAW` y `ProjectAW.PWA` juntos; usa el perfil HTTPS de cada uno.
3. La PWA usa `https://localhost:7144/` y la API conserva `https://localhost:7054`.

Se utiliza el certificado de desarrollo estándar de ASP.NET Core. No hay certificados ni claves privadas en el proyecto.

## API, autenticación y vistas

El login usa `POST /api/auth/login` con `{ "username": "…", "password": "…" }`. Las cuentas con rol Operador reciben la cookie HTTP-only `ProjectAW.Operador.Auth`; el backend rechaza credenciales de operador en el acceso web del administrador y rechaza otros roles en el acceso móvil. `POST /api/auth/logout` cierra solo la sesión de la aplicación desde la que se llamó. No se almacenan contraseñas, cookies ni tokens en almacenamiento web.

Home consulta:

- `GET /api/guias?idTipoOperacionGuia=1&idEstadoGuia=1` para Recepción/Entrada.
- `GET /api/guias?idTipoOperacionGuia=2&idEstadoGuia=1` para Despacho/Salida.
- `GET /api/guias?idTipoOperacionGuia=3&idEstadoGuia=1` para Traslado.

El estado pendiente tiene ID 1 según las constantes del backend. Los conteos se muestran solo tras una respuesta exitosa; ante errores se muestra `—`, no un cero inventado. Los listados se ordenan por `fechaProgramada` ascendente.

Recepción combina los campos de la respuesta de guías con:

- `GET /api/ProveedorApi`, para `idProveedor` y `razonSocial`.
- `GET /api/AlmacenApi`, para `idAlmacenDestino`, `codigo` y `descripcion`.

La API actual de almacenes no tiene un campo de nombre. Despacho y Traslado muestran por ahora código y hora programada. No se han implementado procesos de almacén.

Edita `wwwroot/config/api-config.js` para establecer `apiBaseUrls.production` por instalación. Desarrollo apunta a `https://localhost:7054`.

## CORS y cookies

El backend configura orígenes explícitos en `Cors:AllowedOrigins`; en desarrollo, el perfil HTTPS de `ProjectAW` permite exactamente `https://localhost:7144`. La política `PwaLogin` permite `POST` y `Content-Type` al controlador de autenticación. `PwaRead` permite `GET` y `Accept` para las consultas y catálogos de la PWA; `PwaOperations` permite `GET` para las guías propias y `POST` para tomar, registrar lecturas o finalizar una guía. Todas admiten credenciales y conservan la autorización de backend. En producción establece `Cors__AllowedOrigins__0` con el origen HTTPS de la PWA de ese cliente. No uses `AllowAnyOrigin` con credenciales.

Las dos aplicaciones están bajo HTTPS. El administrador usa la cookie `ProjectAW.Admin.Auth`, distinta de la cookie de operador. Como las cookies se comparten entre puertos del mismo host, la API selecciona el esquema de autenticación según el origen de la solicitud; un operador que inicia sesión en la PWA no sustituye ni autentica la sesión del administrador. `SameSite=Lax` se conserva; CORS permite el intercambio entre orígenes e `include` envía la cookie HTTP-only correspondiente.

## Alcance

Las tarjetas de Recepción, Despacho y Traslado consultan guías pendientes reales. Al seleccionar una, la PWA consulta su detalle y pide confirmación. Si se acepta, la API la asigna al operador autenticado y la PWA abre la vista `guias/detalle.html?id={id}`. La opción “Mis guías” lista por separado En proceso y Terminadas desde `GET /api/simulacion/guias/mias`; la API limita ambos resultados al operador autenticado y valida también la pertenencia al consultar el detalle. Así se puede volver a abrir una lectura después de salir de la pantalla.

## Registro de lecturas por operador

En el detalle de una guía en proceso, **Empezar registro** muestra una confirmación con su código y fecha y abre `guias/registro.html?id={id}`. La pantalla permite capturar producto, cantidad, lote/serie opcionales, ubicaciones y observación. La ubicación queda bloqueada tras la primera lectura; el candado permite cambiarla para las siguientes. La nota lateral consulta el detalle de la misma guía y el historial se solicita al abrirlo, sin sondeo en segundo plano.

Cada envío crea una fila de historial en `LecturaGuia` mediante `POST /api/simulacion/guias/{id}/lecturas`. Al volver a abrir el registro se recupera el historial del operador autenticado. `POST /api/simulacion/guias/{id}/finalizar` requiere al menos una lectura por producto; calcula la suma capturada y aplica stock, movimientos y estado Atendida en una sola transacción. Si hay diferencia respecto a lo esperado, la confirmación la muestra antes del cierre. Las ubicaciones de cada captura se conservan en el historial incluso si cambian durante la guía.

El acceso **Ajustes de lectura** guarda la preferencia PDA/cámara en el navegador. El modo PDA acepta el código de barras como entrada de teclado con Enter. El modo cámara pide permiso solo cuando se activa, procesa pocos cuadros por segundo y detiene el stream al ocultar o abandonar la página. La lectura de cámara utiliza `BarcodeDetector` y `getUserMedia`; requiere HTTPS o localhost y depende de que el navegador soporte el detector. Si falta esa capacidad, la captura manual/PDA sigue disponible.

Para desplegar el backend, aplica la migración `LecturasIndividualesGuia` a la base de datos con las herramientas EF Core del entorno antes de habilitar el registro.
