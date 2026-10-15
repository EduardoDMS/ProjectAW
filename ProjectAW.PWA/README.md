# ProjectAW.PWA

Aplicación ASP.NET Core independiente que sirve la interfaz estática de operaciones. El host no contiene controladores de negocio, autenticación propia, acceso a SQL Server ni referencia al proyecto `ProjectAW`. El navegador consume la API de `ProjectAW` mediante `fetch`, JSON y la cookie HTTP-only existente.

## Estructura

- `Program.cs`: host mínimo que redirige HTTP a HTTPS y publica archivos estáticos.
- `wwwroot/modules/seguridad/login/`: pantalla de acceso.
- `wwwroot/modules/operaciones/inicio/`: Home con conteos reales de guías pendientes.
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

El login existente usa `POST /api/auth/login` con `{ "username": "…", "password": "…" }` y establece la cookie `ProjectAW.Auth`. Después de un login correcto, la PWA abre Home. El botón Cerrar sesión llama a `POST /api/auth/logout`. No existe endpoint de consulta de sesión; Home y los listados comprueban la sesión consultando sus recursos protegidos y vuelven al login ante HTTP 401. No se almacenan contraseñas, cookies ni tokens en almacenamiento web.

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

El backend configura orígenes explícitos en `Cors:AllowedOrigins`; en desarrollo, el perfil HTTPS de `ProjectAW` permite exactamente `https://localhost:7144`. La política `PwaLogin` permite `POST` y `Content-Type` al controlador de autenticación. `PwaRead` permite `GET` y `Accept` solo en los controladores API de guías, proveedores y almacenes. Ambas admiten credenciales y conservan la autorización de backend. En producción establece `Cors__AllowedOrigins__0` con el origen HTTPS de la PWA de ese cliente. No uses `AllowAnyOrigin` con credenciales.

Las dos aplicaciones están bajo HTTPS. `SameSite=Lax` se conserva en la cookie; CORS permite el intercambio entre orígenes e `include` envía la cookie HTTP-only cuando corresponde.

## Alcance

Las tarjetas de Recepción, Despacho y Traslado consultan guías pendientes reales. Recepción muestra código, hora programada, proveedor y almacén destino. Despacho y Traslado tienen un listado provisional de código y hora. No hay sincronización offline, datos ficticios, usuarios de prueba ni endpoints nuevos.
