---
name: projectaw-development
description: Develop or maintain ProjectAW code while following its existing .NET architecture, contracts, and security conventions. Use for changes to ProjectAW entities, DTOs, repositories, services, controllers, or dependency registration; not for unrelated projects.
---
# Skill: Desarrollo y mantenimiento de ProjectAW

## 1. Objetivo

Mantener una arquitectura consistente, un código legible y una estructura escalable al crear, modificar o corregir clases del proyecto ProjectAW.

Todas las modificaciones deben respetar las convenciones existentes, las responsabilidades de cada capa y los contratos definidos mediante interfaces.

La prioridad es producir código sencillo, mantenible, seguro y coherente con el proyecto actual, evitando complejidad innecesaria a menos que una situación excepcional lo requiera.

## 2. Reglas generales

Antes de crear o modificar código:

1. Inspeccionar los archivos relacionados con la tarea.
2. Identificar las clases existentes que cumplen una función similar.
3. Utilizar como referencia la implementación funcional más cercana.
4. Respetar los nombres, las carpetas, los espacios de nombres y las convenciones existentes.
5. Realizar únicamente los cambios necesarios para cumplir el objetivo solicitado.
6. Evitar duplicar funcionalidades o introducir nuevas abstracciones sin necesidad.
7. No modificar reglas de negocio existentes sin autorización.
8. No inventar propiedades, métodos, tablas, columnas ni dependencias que no estén respaldados por el proyecto.

Si falta información necesaria para implementar una solución correcta, solicitarla antes de asumir detalles importantes.

## 3. Repositories

Los repositorios son responsables del acceso a los datos y deben mantener una separación clara respecto de la lógica de negocio y la presentación.

Entre los repositorios existentes para modificarlos se encuentran:

- AlmacenRepository
- AuthRepository
- MovimientoStockRepository
- StockBalanceRepository
- StockRepository
- GuiaRepository
- MotivoGuiaRepository

También existen interfaces como IAuthRepository.

### Reglas para crear o modificar repositorios

1. Revisar primero un repositorio existente que cumpla una función similar.
2. Seguir su estructura, estilo y nivel de complejidad.
3. Implementar los contratos existentes cuando corresponda.
4. Respetar los nombres, parámetros y tipos de retorno de los métodos definidos en las interfaces.
5. Mantener el acceso a datos dentro de la capa de repositorios.
6. Reutilizar la configuración de conexión y los mecanismos de acceso a datos existentes.
7. Gestionar correctamente los recursos y las excepciones según las convenciones del proyecto.
8. Evitar consultas duplicadas, código innecesario y abstracciones que no aporten valor.

No modificar la arquitectura de acceso a datos sin justificar la necesidad del cambio.

## 4. Services

Los servicios coordinan las operaciones de negocio y conectan los controladores con los repositorios de acuerdo con la arquitectura existente.

Entre los servicios actuales a modificar se encuentran:

- AuthService
- UsuarioService
- MovimientoStockService
- StockService
- GuiaService
- MotivoGuiaService

También existen interfaces como IAuthService, IStockService e IMovimientoStockService.

### Reglas para crear o modificar servicios

1. Revisar un servicio existente con responsabilidades similares.
2. Respetar la separación entre la lógica de negocio y el acceso a datos.
3. Implementar la interfaz correspondiente cuando forme parte del diseño establecido.
4. Utilizar las dependencias definidas por el proyecto.
5. Mantener los métodos claros, pequeños y fáciles de comprender.
6. Evitar duplicar validaciones o reglas de negocio que ya tengan una ubicación definida.
7. Manejar errores de manera coherente con las convenciones existentes.
8. No agregar patrones, bibliotecas o capas adicionales sin una necesidad concreta.

El servicio debe cumplir su responsabilidad sin asumir innecesariamente funciones propias del controlador o del repositorio.

## 5. Controllers

Los controladores reciben solicitudes, validan los aspectos correspondientes a la entrada HTTP, delegan las operaciones y devuelven respuestas apropiadas.

Entre los controladores existentes a actualizar se encuentran:

- AuthApiController
- AuthController
- LecturasApiController
- MovimientosApiController
- MotivosGuiaApiController

### Reglas para crear o modificar controladores

1. Revisar un controlador existente que gestione operaciones similares.
2. Mantener la estructura y las convenciones utilizadas en el proyecto.
3. Delegar las operaciones de negocio a los servicios correspondientes.
4. No incorporar consultas directas a la base de datos cuando esa responsabilidad pertenezca a los repositorios.
5. Respetar las rutas, los métodos HTTP y los formatos de respuesta existentes.
6. Validar las entradas de acuerdo con las necesidades del endpoint.
7. Mantener la autenticación y la autorización correspondientes.
8. No exponer contraseñas, tokens, credenciales ni detalles internos sensibles.
9. Evitar cambios incompatibles con los clientes que consumen la API.
10. Documentar las decisiones que se aparten de las convenciones existentes.

Los comentarios deben explicar decisiones relevantes o comportamientos no evidentes. No deben describir innecesariamente cada línea de código.

## 6. Contratos e inyección de dependencias

Al incorporar una clase nueva:

1. Identificar si corresponde una interfaz según el diseño existente.
2. Definir o respetar el contrato correspondiente.
3. Implementar los métodos con las firmas y los tipos de retorno esperados.
4. Comprobar que las dependencias estén correctamente inyectadas.
5. Registrar la implementación en el contenedor de dependencias cuando corresponda.
6. Verificar que las capas puedan comunicarse correctamente.
7. Mantener compatibilidad con los consumidores existentes, salvo que el cambio solicitado requiera otra cosa.

No crear interfaces redundantes ni cambiar contratos públicos sin evaluar las consecuencias.

## 7. Seguridad

Todas las modificaciones deben respetar las medidas de seguridad existentes.

- No eliminar atributos o mecanismos de autenticación y autorización sin justificación y autorización.
- Validar las entradas que puedan afectar las operaciones del sistema.
- Evitar la exposición de información sensible en las respuestas y los registros.
- Utilizar los mecanismos seguros de acceso a datos establecidos en el proyecto.
- No introducir credenciales directamente en el código.
- No debilitar las restricciones de acceso para solucionar errores funcionales.

## 8. Procedimiento de trabajo

Para cada tarea:

1. Analizar el requerimiento.
2. Inspeccionar las clases y los archivos relacionados.
3. Identificar la plantilla existente que debe seguirse.
4. Determinar los cambios mínimos necesarios.
5. Implementar la solución respetando los contratos y las responsabilidades.
6. Revisar las referencias y los registros de dependencias afectados.
7. Ejecutar las comprobaciones disponibles.
8. Informar los archivos modificados, los motivos y los resultados de las verificaciones.

Si no es posible ejecutar una prueba o comprobar la compilación, indicarlo explícitamente.

## 9. Criterios de calidad

El código final debe ser:

- Legible.
- Simple.
- Consistente con el proyecto.
- Fácil de mantener.
- Escalable sin complejidad innecesaria.
- Seguro.
- Compatible con los contratos existentes.

La prioridad es mantener la coherencia arquitectónica y resolver el requerimiento sin introducir cambios ajenos a la tarea.

## 10. Justificación de desviaciones

Si una solución requiere apartarse de la estructura habitual, explicar:

- Qué convención se está modificando.
- Por qué la estructura actual no resulta suficiente.
- Qué alternativa se propone.
- Qué impacto tendrá en las demás capas.
- Cómo se verificará que el cambio funciona correctamente.

No realizar cambios arquitectónicos amplios cuando una corrección localizada sea suficiente.

## 11. Plantilla oficial y referencia arquitectónica

### 11.1. Referencia principal

La estructura de referencia para el desarrollo y mantenimiento de ProjectAW se encuentra en `Maestros/Productos`.

Las siguientes clases deben utilizarse como ejemplos principales:

- `Producto.cs`
- `ProductoConfiguration`
- `CrearProductoDto`
- `ActualizarProductoDto`
- `ProductoDto`
- `IProductoRepository`
- `ProductoRepository`
- `IProductoService`
- `ProductoService`
- `ProductoController`
- `ProductoApiController`

Estas clases representan las convenciones de implementación que deben seguirse al trabajar con otras entidades.

### 11.2. Inspección obligatoria

Antes de crear o modificar una clase:

1. Localizar las clases de referencia dentro del proyecto.
2. Leer su implementación y revisar sus dependencias.
3. Identificar su estructura, nomenclatura, manejo de errores y convenciones.
4. Comparar la clase que se va a modificar con su equivalente en Productos.
5. Aplicar las convenciones correspondientes sin copiar código que no se adapte a la entidad.

No asumir que una clase sigue una estructura determinada sin haberla inspeccionado.

### 11.3. Aplicación de las plantillas

Al desarrollar una nueva entidad, seguir la estructura de Productos en los componentes que correspondan:

- Entidad y configuración.
- DTO de creación, actualización y respuesta.
- Interfaz e implementación del repositorio.
- Interfaz e implementación del servicio.
- Controlador MVC, cuando corresponda.
- Controlador API, cuando corresponda.
- Registro de dependencias y demás configuraciones necesarias.

No crear automáticamente todos los componentes si la entidad o el requerimiento no los necesitan. Respetar las particularidades funcionales de cada módulo.

### 11.4. Manejo de diferencias

Si las clases existentes presentan diferencias respecto a la plantilla:

1. Identificar las diferencias relevantes.
2. Determinar si afectan al requerimiento actual.
3. Corregir únicamente lo necesario para cumplir el objetivo solicitado.
4. Evitar refactorizaciones generales no solicitadas.
5. Explicar cualquier desviación arquitectónica necesaria y sus posibles efectos.

No modificar contratos públicos ni introducir cambios incompatibles sin evaluar previamente sus consecuencias.

### 11.5. Verificación final

Después de cada modificación:

- Comprobar que las interfaces y sus implementaciones sean compatibles.
- Revisar las dependencias y su registro en el contenedor de inyección.
- Comprobar las referencias entre las capas afectadas.
- Verificar las rutas, los contratos HTTP y las respuestas cuando corresponda.
- Revisar las implicaciones de seguridad.
- Ejecutar la compilación y las pruebas pertinentes cuando sea posible.
- Informar los archivos modificados y las comprobaciones realizadas.

Distinguir siempre entre las verificaciones ejecutadas y las que todavía están pendientes.







