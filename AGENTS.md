# Convenciones de frontend — ProjectAW

## 1. Tecnologías existentes
- El proyecto principal utiliza ASP.NET Core MVC y vistas Razor `.cshtml`.
- ProjectAW.PWA utiliza HTML, CSS y JavaScript y consume las API del proyecto principal.
- Bootstrap ya está integrado. Antes de realizar cambios, revisar la versión y los estilos existentes en el proyecto que se va a modificar.
- No introducir otro framework CSS ni una biblioteca de componentes sin autorización.

## 2. Tipografía
- Respetar los tamaños, pesos, familias y márgenes tipográficos predeterminados de Bootstrap.
- No crear clases CSS para cambiar la tipografía de `h1`, `h2`, `h3`, `h4`, `h5`, `h6`, `label`, párrafos o controles de formulario.
- No utilizar clases como `.titulo-grande`, `.texto-pequeno` o equivalentes para modificar la tipografía de elementos estándar.
- No asignar `font-size`, `font-weight` ni `font-family` mediante CSS personalizado, estilos inline o clases de utilidad de Bootstrap, salvo que un requisito explícito lo justifique.
- Estas reglas se aplican a las vistas y estilos que se creen o modifiquen. No requieren cambiar de forma general los archivos existentes; al trabajar en una vista, aplicar estas convenciones a los cambios de esa tarea.
- Si existe un estilo tipográfico global personalizado, comprobarlo antes de añadir o modificar estilos.
- Se permite definir una familia tipográfica mediante CSS cuando sea un requisito del proyecto; mantener esa definición centralizada y no usarla como motivo para cambiar tamaños o pesos tipográficos.

## 3. Uso de Bootstrap
- Priorizar las clases nativas de Bootstrap para formularios, botones, tablas, alertas, tarjetas, navegación y distribución.
- Utilizar `form-label`, `form-control`, `form-select`, `btn`, `table`, `row`, `col` y las clases de utilidad existentes según corresponda.
- Utilizar las clases de espaciado de Bootstrap antes de crear reglas CSS equivalentes.
- Respetar el diseño adaptable mediante el sistema de grid y las utilidades responsive de Bootstrap.

## 4. CSS personalizado
- Antes de crear una clase, comprobar si Bootstrap o una clase existente resuelve la necesidad.
- Usar las clases estándar de Bootstrap para estructura, distribución, espaciado, formularios, botones, tablas, navegación y comportamiento responsive. No crear clases CSS globales para reproducir utilidades o componentes que Bootstrap ya ofrece.
- No añadir ni ampliar selectores globales personalizados que cambien la apariencia de elementos HTML o componentes de Bootstrap.
- Se permite CSS personalizado únicamente para una necesidad visual muy específica que Bootstrap no cubra. Mantener esas reglas acotadas al componente o módulo que las necesita y evitar que afecten otras vistas.
- Se permiten reglas CSS para definir familias tipográficas, de forma centralizada; los tamaños y pesos deben seguir los valores predeterminados de Bootstrap salvo requisito explícito.
- No duplicar estilos entre vistas.
- Para una excepción visual que deba compartirse, reutilizar una clase existente o mantenerla en el CSS del componente o módulo correspondiente; no convertirla en un estilo global.
- Evitar estilos inline y bloques `<style>` dentro de las vistas, salvo justificación concreta.
- No modificar estilos globales para resolver una necesidad exclusiva de una vista.

## 5. Consistencia entre vistas
- Revisar las vistas existentes del mismo módulo antes de implementar una nueva.
- Reutilizar los patrones existentes de títulos, formularios, filtros, tablas, acciones, mensajes y estados vacíos.
- No crear variaciones visuales de un componente que ya existe sin un requisito funcional o de diseño.
- Mantener los cambios limitados al alcance solicitado.
- Formatear el marcado `.cshtml` y `.html` en varias líneas, con indentación que muestre claramente la jerarquía de etiquetas. No comprimir estructuras anidadas en una sola línea.
- Escribir cada etiqueta de apertura y todos sus atributos en una sola línea. Si la etiqueta tiene contenido, colocar el contenido y la etiqueta de cierre en líneas separadas y respetar la indentación. Ejemplo: `<button type="button" class="btn btn-light">`, contenido y `</button>` en su propia línea.
- Las etiquetas vacías, como `<input>`, `<img>` y `<link>`, deben ir completas en una sola línea, incluidos sus atributos y el cierre `/>` cuando corresponda.
- En listas, formularios y tablas, escribir cada elemento en su propia línea. En tablas, separar `<table>`, `<thead>`, `<tbody>`, cada `<tr>`, `<th>` y `<td>`; poner la apertura con sus atributos y el cierre de cada elemento en líneas propias.

## 6. Verificación
- Antes de finalizar, revisar los cambios para detectar CSS duplicado, clases tipográficas innecesarias, estilos inline y overrides de Bootstrap.
- Confirmar que no se añadieron estilos globales cuando una clase estándar de Bootstrap resuelve la necesidad y que el CSS personalizado está limitado a una excepción concreta.
- Comprobar que las vistas funcionan con los recursos y la versión de Bootstrap existentes en el proyecto correspondiente.
- Informar de cualquier excepción a estas convenciones y justificarla.
