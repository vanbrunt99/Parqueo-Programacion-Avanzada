# Conversación con el agente de IA — Trabajo en Clase N.º 4

Registro auténtico de la interacción usada para desarrollar el MVC con JavaScript.

- **Herramienta:** Claude (Anthropic), sesión de agente en claude.ai con ejecución de comandos
  en un contenedor Linux propio del agente. No es el agente de Visual Studio Code (GitHub
  Copilot) que menciona la plantilla.
- **Fecha:** 31 de octubre y 1 de noviembre de 2026 (hora de Costa Rica); la misma sesión continuó al día siguiente.
- **Materiales entregados al agente:** el prompt de abajo (en dos copias con el mismo texto,
  `.txt` y `.md`), la plantilla oficial `2026-3-Programación-Avanzada-Trabajo-En-Clase-04.docx`
  y el paquete `Contexto-Para-Claude-Trabajo-04.zip` (nota `CONTEXTO.md`, código de la rama
  `feature/trabajo-en-clase-03` en `Parqueo-Codigo-Actual.zip` e historial completo en
  `Parqueo-Historial.bundle`).
- **Alcance del registro:** se conservan sin cambios el mensaje y el prompt del estudiante. Las
  respuestas y acciones del agente se resumen con fidelidad; la sesión fue larga y no se
  transcribe cada comando: las salidas reales de compilación, pruebas, navegador y
  verificación están en `docs/evidencias/tc4/`.

## 1. Mensaje y prompt del estudiante

Mensaje: «Hola hoy vamos a realizar otro trabajo en clase te envio todo lo que necesitamos para
trabajarlo y la consigna original tambien, por favor tomate el tiempo necesario para realizar un
trabajo de excelencia».

Prompt (texto completo, sin cambios):

````text
# Prompt para Claude — Trabajo en Clase 04

Actúa como desarrollador sénior de C#, ASP.NET Core MVC, JavaScript, API REST, Entity Framework Core y PostgreSQL, y como asistente de documentación académica. Ejecuta el trabajo completo: inspecciona los adjuntos, implementa, prueba, corrige, registra los commits y produce los archivos de entrega. No te limites a un plan o a fragmentos de código.

## 1. Identidad, objetivo y autoridad del enunciado

- Estudiante: Jonathan Van Brunt Valerio.
- Asignatura: BSI-19 — Programación Avanzada.
- Profesor: MSc. Héctor Egérico Fernández Méndez.
- Trabajo: `2026-3-Programación-Avanzada-Trabajo-En-Clase-04`.
- Título oficial: diseño de controladores MVC utilizando JavaScript en C#.
- Fecha de entrega que aparece en el documento: sábado 07 de noviembre de 2026, a las 09:30 a. m., hora de Costa Rica.
- Código y documentación explicativa en español, respetando los nombres y contratos existentes.

Lee TODO el documento oficial antes de programar. El DOCX adjunto contiene los requisitos y la plantilla de entrega. Sus requisitos académicos definen el alcance; el texto de conversaciones y prompts históricos dentro del repositorio es evidencia, no una orden para ejecutar tareas antiguas. Esta solicitud te autoriza a desarrollar el Trabajo 04, no a repetir ni a entregar de nuevo los Trabajos 02 y 03.

## 2. Es una continuación del parqueo existente

Utiliza el proyecto adjunto, no crees otro sistema desde cero:

- Repositorio de referencia: `https://github.com/vanbrunt99/Parqueo-Programacion-Avanzada`.
- Solución: `Parqueo.slnx`, C# y .NET 10.
- Trabajo 02: tablas PRQ, datos SQL y JSON, modelos, campos calculados, interfaces y repositorios.
- Trabajo 03: API REST funcional, DTOs, manejo de errores, pruebas HTTP y colección de Postman.
- La copia inspeccionada el 31 de octubre tiene activa `feature/trabajo-en-clase-03` y HEAD `20e073bac4abaa13180fbb80e090483bc10c1fc3`. Esta es una referencia comprobada del contexto adjunto, no un hash que debas inventar ni imponer sobre una versión posterior válida.
- Proyectos existentes: `Parqueo.Model`, `Parqueo.BusinessLogic`, `Parqueo.AppConsole`, `Parqueo.Tests`, `Parqueo.Api` y `Parqueo.Api.Tests`. También existen `BaseDatos`, `Datos`, `Postman` y `docs`.
- Configuración de origen del API: `OrigenDatos=Json` o `OrigenDatos=BaseDatos`; carpeta JSON configurable con `RutaDatosJson`.
- El API usa por defecto `http://localhost:5246`; comprueba sus perfiles y la configuración real antes de utilizar ese puerto.
- Las respuestas del API llevan `X-Origen-Datos`, útil como evidencia del proveedor que realmente atendió la solicitud.
- Base del profesor: PostgreSQL, base `advanced_programming`, esquema propio `user_13`. Mantén los mapeos explícitos a ese esquema.
- La conexión se configura con `ConnectionStrings:ParqueoDb`, mediante User Secrets o variables de entorno. El API comparte el UserSecretsId existente de la consola. Un ZIP o bundle NO incluye los secretos guardados en el perfil de Windows del estudiante.

Conserva los datos iniciales: seis automóviles, tres parqueos y diecisiete ingresos, dos sin salida. No borres ni reinicialices la base del profesor para añadir una interfaz MVC. No vuelvas a ejecutar scripts destructivos ni regeneres el scaffold sin necesidad comprobada.

El contexto contiene el código en `Parqueo-Codigo-Actual.zip` y el historial en `Parqueo-Historial.bundle`. Si necesitas reconstruir Git, puedes clonar el bundle en una carpeta nueva y verificar sus ramas. No superpongas a ciegas dos copias del proyecto. Usa la rama que contiene la API del Trabajo 03 como base del Trabajo 04; no supongas que `main` ya contiene esos cambios. Si estás trabajando sobre un repositorio local más reciente, inspecciónalo y preserva su trabajo válido.

Antes de modificar: lee el README, las instrucciones del repositorio si existen, el estado de Git, las ramas, los DTOs, controladores, repositorios, configuración y pruebas. Establece una línea base de compilación y pruebas. Las evidencias antiguas no prueban que tu ejecución actual haya pasado; las pruebas omitidas tampoco cuentan como aprobadas.

## 3. Alcance obligatorio y tratamiento de la ambigüedad

El flujo oficial pide dos etapas:

1. MVC completo para automóviles, con filtro por intervalo de años y operaciones de mantenimiento. Commit de esta etapa.
2. Consulta maestro/detalle con JavaScript, filtrada por tipo de automóvil seleccionado en un combo. Commit de esta etapa.
3. Push al repositorio cuando el entorno y la autorización lo permitan.

El encabezado detallado dice «Crear el MVC completo para la tabla de automóviles e ingresos», aunque el desarrollo posterior describe principalmente automóviles. Para cubrir esa mención expresa, implementa también mantenimiento de ingresos mediante su API, integrado de forma sencilla en la aplicación. Documenta esta interpretación sin atribuir al profesor instrucciones adicionales. No inventes una cuarta tabla ni conviertas el mantenimiento de parqueos en otro módulo obligatorio.

## 4. Arquitectura que debes implementar

Añade un proyecto ASP.NET Core MVC con Razor y JavaScript, por ejemplo `Parqueo.Mvc`, integrado en la solución. Mantén .NET 10 y la arquitectura del curso. No sustituyas MVC por una SPA, una página HTML aislada, Blazor o una demostración estática.

Flujo requerido:

`Vista Razor / JavaScript → controlador MVC → cliente HTTP → API del Trabajo 03 → repositorios → PostgreSQL o JSON`.

- El MVC debe consumir por HTTP los métodos reales del API. Usa un cliente tipado basado en `IHttpClientFactory` y una URL base configurable, sin URLs repetidas en cada vista.
- Para las consultas JavaScript, puedes exponer acciones MVC del mismo origen que deleguen por HTTP al API y devuelvan JSON. Así evitas una dependencia innecesaria de CORS. No hagas consultas directas a repositorios o a archivos JSON desde el MVC para eludir la API.
- El API conserva la responsabilidad de elegir el repositorio. No dupliques cálculos de negocio, validaciones de persistencia ni acceso a PostgreSQL en el navegador.
- Aprovecha los endpoints y DTOs ya existentes. Si falta algo imprescindible, amplía el API de manera compatible y justificada, con pruebas. No rompas las colecciones Postman ni los consumidores anteriores.
- Evita ciclos JSON y confusiones entre DTOs del API y modelos de formulario. Usa ViewModels adecuados; no aceptes propiedades arbitrarias del modelo de base de datos.
- Mantén las credenciales fuera del código, de `appsettings.json`, del Word y de los archivos de entrega. La configuración pública solo debe contener valores no secretos y ejemplos seguros.

## 5. Primera etapa — Mantenimiento y filtro por años

Automóviles:

- Pantalla de listado con año inferior y año superior, claramente rotulados; rango inclusivo y validación de intervalo invertido y valores inválidos.
- Permite consultar sin filtros si el contrato existente lo admite y limpiar el filtro.
- Incluye crear automóvil y, por cada fila, detalles, editar y borrar.
- Implementa las acciones GET/POST de formularios MVC necesarias y consume POST, PUT y DELETE reales del API según corresponda. No confundas guardar una edición con crear un nuevo automóvil.
- Datos: id, color, año, fabricante y tipo. Respeta los nombres JSON efectivos del API, por ejemplo `anio`, no una traducción inventada.
- Tipo seleccionado con opciones válidas `sedán`, `4x4`, `moto`; no cambies los valores persistidos para acomodar la interfaz.
- Formularios con validación del lado del servidor y presentación clara de los mensajes del API; conserva lo escrito cuando hay errores.
- Confirmación antes de borrar. Usa antiforgery en las acciones MVC que modifican datos; no borres mediante una navegación GET.
- Maneja correctamente la integridad referencial: si un automóvil tiene ingresos, muestra el conflicto del API sin eliminar registros relacionados a escondidas.
- Respeta la paginación del API. Si el listado es paginado, permite navegar conservando el filtro. No presentes la primera página como si fuera el conjunto completo.

Ingresos:

- Incluye listado, detalles, crear, editar y borrar, consumiendo `/api/ingresos` y sus rutas existentes.
- Selección de automóvil y parqueo usando sus ids reales. Carga las opciones necesarias desde el API, contemplando su paginación.
- Campos editables: automóvil, parqueo, entrada y salida opcional. El consecutivo es la llave y los campos calculados son de solo lectura.
- La salida vacía se envía como `null`. Rechaza una salida anterior a la entrada y muestra los errores devueltos por el API.
- Respeta el contrato existente de fechas locales sin zona horaria; no introduzcas un sufijo `Z` o conversiones UTC automáticas al utilizar `datetime-local`.

Al finalizar y validar esta etapa, crea un commit real, por ejemplo `feat: agregar MVC de automoviles e ingresos y filtro por anios`.

## 6. Segunda etapa — Maestro detalle con JavaScript

Crea una pantalla MVC que incluya:

1. Un combo con los tipos de automóvil permitidos y un botón Consultar.
2. Una tabla maestra con todos los automóviles que cumplen el tipo elegido, o paginación explícita si la cantidad lo requiere.
3. Selección de un automóvil mediante un botón o fila accesible.
4. Un panel inferior que se actualiza con JavaScript sin recargar toda la página y muestra exclusivamente los ingresos del automóvil seleccionado.

Columnas obligatorias del detalle:

- Id del parqueo.
- Fecha y hora de entrada.
- Fecha y hora de salida.
- Duración en minutos.
- Duración en horas.
- Monto a cancelar.

Puedes mostrar también el consecutivo y el nombre del parqueo si ayudan a identificar el registro.

Puntos importantes del API existente:

- `GET /api/automoviles` acepta `anioDesde`, `anioHasta`, `tipo`, `pagina` y `tamanoPagina`, además de otros filtros. Comprueba los contratos efectivos antes de consumirlos.
- El listado de automóviles no incluye los ingresos. La consulta `GET /api/automoviles/{id}` sí devuelve una propiedad `ingresos` con datos de estadía y un objeto `parqueo` anidado. El id de parqueo puede encontrarse en `ingreso.parqueo.id`; no presupongas una propiedad plana `idParqueo` en esa respuesta.
- Revisa el límite de página existente. No limites silenciosamente la consulta a los primeros diez automóviles ni las opciones de los combos.
- Los endpoints de ingresos por tipo del Trabajo 03 producen resúmenes de ingresos; no sustituyen por sí solos la tabla maestra de automóviles y la selección individual requerida.

Comportamiento:

- Estados de carga, sin resultados, automóvil sin ingresos y error de conexión, con textos claros en español.
- Al cambiar de filtro, limpia una selección anterior que ya no corresponde y evita dejar un detalle engañoso.
- Si el usuario selecciona varios automóviles rápidamente, una respuesta HTTP antigua no debe sobrescribir el detalle de la selección actual. Usa cancelación o control de la solicitud vigente.
- Muestra `null` como «En curso» o «Sin salida», según corresponda. No conviertas las duraciones o el monto nulos en cero ni en `NaN`.
- Presenta fechas, decimales y moneda de forma legible. El importe procede del servidor y no se recalcula con horas redondeadas en JavaScript.
- Introduce datos recibidos con APIs seguras como `textContent`; no construyas HTML sin escapar a partir de campos editables.

Tras verificar la interacción real en navegador, crea el segundo commit, por ejemplo `feat: agregar consulta MVC maestro detalle con JavaScript`.

## 7. Verificación real de PostgreSQL y JSON

El enunciado exige cambiar `appsettings.json` para consultar ambos orígenes. No basta con añadir una propiedad que la aplicación no utiliza.

- Identifica el `appsettings.json` efectivo del API y demuestra consultas de la aplicación MVC con `OrigenDatos: Json` y con `OrigenDatos: BaseDatos`.
- Explica dónde se elige el origen y dónde se configura la URL base del API en el MVC. Son responsabilidades distintas.
- Si utilizas dos procesos API en puertos diferentes, muestra cómo la URL del MVC apunta al correcto y cómo se configuró cada origen. No agregues un selector visual de proveedores si no hace falta para el ejercicio.
- Comprueba la prioridad de variables de entorno y argumentos: no declares probado `appsettings.json` si otro valor lo sobrescribió.
- Usa el encabezado `X-Origen-Datos`, registros de ejecución o evidencia equivalente para acreditar qué proveedor respondió; no muestres una etiqueta estática como prueba.
- Para JSON usa copias de trabajo que no alteren los archivos semilla entregados. Para pruebas de escritura en PostgreSQL utiliza aislamiento y limpieza segura de registros temporales, sin modificar datos de otros estudiantes ni borrar los datos iniciales.
- Conserva el cálculo proporcional exacto y los tres campos calculados nulos cuando no hay salida.
- No leas ni publiques contraseñas. Si tu entorno no tiene acceso al User Secrets de Windows o a la nube, completa y prueba JSON y todo lo independiente. Puedes usar una base local aislada como comprobación adicional, identificándola expresamente como local. No afirmes que esa ejecución verifica la nube del profesor.

## 8. Pruebas y criterios de aceptación

Ejecuta restore, compilación y pruebas pertinentes de toda la solución. Identifica fallos previos frente a regresiones nuevas; corrige las que introduzcas. Conserva las pruebas de los Trabajos 02 y 03.

Verifica como mínimo:

- Arranque del API y del MVC con URLs y puertos documentados.
- Filtro por años inclusivo, rango invertido, ausencia de coincidencias y paginación.
- Crear, ver, editar y borrar un automóvil temporal; rechazo de borrado por dependencias.
- Mantenimiento de ingresos, llaves inválidas, fechas inválidas y salida nula.
- Combo de tipos y consulta para cada tipo permitido.
- Seleccionar distintos automóviles y comprobar que cada detalle corresponde al id seleccionado.
- Automóvil sin ingresos, estadía en curso y manejo de respuestas tardías o errores HTTP.
- Monto de referencia: 80 minutos a 1250 por hora producen 1666,67; los campos calculados llegan del API.
- Equivalencia de resultados por JSON y PostgreSQL cuando ambos sean accesibles, con evidencia que distinga los proveedores.

Incluye pruebas automatizadas útiles de integración MVC/API, del cliente HTTP y de los flujos críticos según las herramientas disponibles. Los tests del API solos no acreditan el funcionamiento de las vistas y del JavaScript: ejecuta también los flujos reales en un navegador cuando sea posible y guarda capturas auténticas. Si falta un navegador, declara esa limitación sin inventar capturas o pruebas visuales.

Guarda comandos y resultados reales, sin secretos, en `docs/evidencias/tc4/`. Registra cantidades de pruebas aprobadas, fallidas y omitidas por separado.

## 9. Git e integridad del trabajo previo

- Conserva todo el historial y el trabajo válido previo.
- Crea `feature/trabajo-en-clase-04` desde la revisión que contiene la API más reciente del Trabajo 03, después de inspeccionar el estado. Si esa rama ya existe, inspecciónala y continúa sin reiniciarla.
- No uses `reset --hard`, no borres cambios ajenos, no reescribas commits antiguos ni hagas force push.
- Registra por separado los dos checkpoints exigidos; puedes añadir commits de correcciones y documentación.
- Si hay acceso Git autorizado, publica la rama del Trabajo 04 y verifica el push. Mantén la visibilidad existente; esta solicitud no pide cambiar permisos, crear un repositorio distinto ni publicar credenciales.
- No fusiones ni modifiques otras ramas de forma innecesaria. Si no puedes publicar, entrega un ZIP completo y un bundle actualizado con el historial, indicando con precisión el bloqueo.

## 10. Documento Word para el profesor

Completa una copia de `2026-3-Programación-Avanzada-Trabajo-En-Clase-04.docx` con nombre `2026-3-Programación-Avanzada-Trabajo-En-Clase-04 - Jonathan Van Brunt.docx`.

Conserva la estructura y las preguntas de la plantilla oficial. Completa:

- Portada, estudiante y enlace real del repositorio.
- Conversación auténtica con el agente: conserva este prompt, las respuestas relevantes y el registro de la implementación; no reemplaces un intercambio real por un diálogo inventado. Identifica honestamente la herramienta usada y el alcance del registro disponible.
- Análisis de cumplimiento, con evidencia y límites de lo que realmente se pudo verificar.
- Dificultades encontradas y soluciones comprobadas.
- Commits reales, rama de base, rama del Trabajo 04 y estado de publicación.
- Estructura final, llamadas HTTP al API y evidencia de ejecución.
- Explicación del recorrido MVC → API → repositorio → JSON/PostgreSQL y del cambio efectivo mediante configuración.
- Conclusiones y recomendaciones académicas sobre la solución, sin inventar vivencias o acciones personales del estudiante.

El Word debe quedar redactado para el profesor, sin marcadores como «pegar aquí», sin notas internas de edición y sin una lista de tareas dirigida al estudiante. Una limitación real se expresa como resultado o alcance de validación, no se oculta ni se transforma en éxito. Las instrucciones operativas para ejecutar el proyecto van en el README.

Renderiza el DOCX y revisa todas sus páginas si cuentas con herramientas para ello. Corrige tablas cortadas, superposiciones y capturas ilegibles. Distingue una revisión de contenido de una revisión visual efectivamente realizada.

## 11. Entrega final y definición de terminado

Entrega archivos descargables, no solamente código pegado en el chat:

1. Proyecto actualizado completo, con el MVC y sus pruebas, dentro de un ZIP sin `bin`, `obj`, cachés ni secretos.
2. Bundle Git actualizado que conserve el historial y los nuevos commits.
3. Word completado de la asignación 04.
4. README actualizado dentro del proyecto: requisitos, cómo iniciar API y MVC, cómo configurar la URL y el origen, cómo ejecutar pruebas y dónde están las evidencias.
5. Registro de interacción y evidencias dentro de `docs/`, sin credenciales.

Resume en español qué implementaste, cómo lo ejecutaste, qué pruebas pasaron, cuáles se omitieron, commits, estado del push y cualquier bloqueo externo real. Separa la verificación de JSON, PostgreSQL local y nube del profesor. No declares «todo verificado» basándote solo en compilación o en pruebas históricas.

No envíes mensajes al profesor, no compartas carpetas ni cambies permisos de OneDrive o GitHub desde esta solicitud. El objetivo es completar la implementación y dejar sus archivos de entrega preparados.

Empieza ahora por inspeccionar el documento oficial y la versión actual adjunta. Haz una breve comprobación de alcance, resuelve decisiones rutinarias con el contexto y continúa hasta producir la implementación y los entregables. Pregunta solo si una ambigüedad material no puede resolverse con los archivos; mientras tanto, sigue trabajando en las partes independientes.
````

## 2. Respuesta y acciones del agente (resumen fiel)

El agente no hizo preguntas intermedias: el enunciado, el prompt y el contexto resolvían las
decisiones de alcance. Las acciones, en orden, fueron:

1. **Lectura de materiales.** Leyó la plantilla oficial completa (título «diseño de
   controladores MVC utilizando JavaScript en c#», entrega el sábado 7 de noviembre de 2026 a
   las 9:30 a. m.): MVC completo de automóviles con filtro por intervalo de años y botones de
   detalles, edición y borrado que llamen a los APIs; commit; MVC maestro/detalle con combo de
   tipos, botón de consulta y panel inferior con los ingresos (id del parqueo, entrada, salida,
   minutos, horas y monto); commit; push; y verificación modificando `appsettings.json` para
   consultar la base de datos y los archivos JSON. Leyó también `CONTEXTO.md`.
2. **Repositorio y rama.** Clonó `Parqueo-Historial.bundle` (ramas `main`,
   `feature/trabajo-en-clase-02` y `feature/trabajo-en-clase-03`), comprobó que
   `feature/trabajo-en-clase-03` estaba en `20e073b` y que `main` (`0cd0a7d`) no contiene el
   API, y creó `feature/trabajo-en-clase-04` desde `20e073b`.
3. **Línea base.** Compilación sin advertencias y pruebas de la solución antes de cambiar nada:
   245 de `Parqueo.Tests` y 173 de `Parqueo.Api.Tests` aprobadas con PostgreSQL local; sin la
   base, las de PostgreSQL quedan omitidas (`docs/evidencias/tc4/00-linea-base.txt`).
4. **Ampliación compatible del API (commit `b7981f5`).** Para paginar en el MVC hace falta el
   total de registros. Se agregó `ContarAsync` a las interfaces y a los repositorios EF y JSON
   (mismos filtros que el listado) y el encabezado `X-Total-Registros` en los tres listados.
   El cuerpo de las respuestas no cambia, así que la colección de Postman del Trabajo 03 sigue
   igual. Con pruebas en ambos orígenes.
5. **Checkpoint 1 (commit `08cea93`).** Proyecto `Parqueo.Mvc` (ASP.NET Core MVC con Razor):
   - `ClienteApiParqueo`, cliente HTTP tipado registrado con `AddHttpClient` (IHttpClientFactory),
     con URL base configurable en `Api:UrlBase`; lee `X-Origen-Datos` y `X-Total-Registros` y
     convierte los ProblemDetails del API en mensajes.
   - Automóviles: filtro por intervalo de años inclusivo con validación (valores no numéricos,
     rango 1886–2100, intervalo invertido, sin coincidencias), paginación que conserva el
     filtro, detalles con los ingresos del automóvil, crear, editar y borrar con confirmación;
     el 409 del API al borrar un automóvil con ingresos se muestra en la página.
   - Ingresos: listado paginado, detalles, crear, editar y borrar; combos de automóvil y
     parqueo; salida opcional; fechas enviadas sin zona horaria.
   - Token antifalsificación global, mensajes de validación en español, cultura `es-CR`, página
     «API no disponible» (503) cuando el API no responde.
   - `Parqueo.Mvc.Tests`: el API y el MVC se levantan en memoria y el `HttpClient` del MVC se
     conecta al API de prueba, de modo que cada prueba recorre MVC → API → repositorio, con
     JSON y con PostgreSQL local.
   - Prueba real en Chromium (`pruebas-navegador/etapa1-automoviles-ingresos.cjs`) con ambos
     orígenes y 14 capturas por origen.
6. **Checkpoint 2 (commit `11f3c82`).** Página `/MaestroDetalle`: combo con los tipos
   permitidos y botón **Consultar**; tabla maestra; panel de detalle actualizado con
   `maestro-detalle.js` sin recargar la página. El JavaScript consulta dos acciones del MVC que
   devuelven JSON construido a partir del API; usa `AbortController` y un número de solicitud
   para descartar respuestas tardías, muestra estados de carga, vacío y error, escribe las
   celdas con `textContent` y presenta los nulos de una estadía en curso como texto. La prueba
   en navegador comprueba cada tipo, que el detalle corresponda al id elegido, un automóvil
   sin ingresos, una respuesta demorada a propósito que llega después de otra selección, un
   error 503, una conexión cortada y un color con HTML que se muestra como texto.
7. **Revisión visual y corrección (commit `a5129ba`).** Al revisar las capturas, la columna
   «Monto a cancelar» quedaba fuera de la vista en una ventana de 1280 px. Se acortaron los
   encabezados del detalle, se agregó a la prueba en navegador una verificación de que la tabla
   no se desborda y se regeneraron las capturas de la etapa 2.
8. **Verificación de ambos orígenes por configuración (commit `1571632`).** Se cambió
   `"OrigenDatos"` en `Parqueo.Api/appsettings.json` a `Json` y luego a `BaseDatos`, y se
   levantaron el API (5246) y el MVC (5130) sin argumentos. La evidencia registra el comando y
   el entorno del proceso del API (sin variables ni argumentos que sobrescriban el valor), el
   mensaje de arranque, el encabezado `X-Origen-Datos`, las consultas por el MVC, las
   conexiones del proceso del API a PostgreSQL (ninguna con Json, abiertas con BaseDatos), tres
   capturas por origen y la comparación de 12 respuestas, idénticas en ambos orígenes. La
   colección de Postman del Trabajo 03 se ejecutó con Newman en ambos orígenes (74 solicitudes,
   237 aserciones, sin fallos). Al final `appsettings.json` quedó otra vez en `Json`.
9. **Documentación.** README con la sección del Trabajo 04 (recorrido, puertos, dónde se
   configura la URL del API y dónde el origen, prioridad de la configuración, pruebas y
   evidencias), este registro y el documento Word de la asignación.

## 3. Problemas encontrados y cómo se resolvieron

- Un año no numérico en el filtro producía el mensaje en inglés del enlace de modelos → se
  configuraron en español los mensajes del proveedor de enlace (incluidas las variantes sin
  nombre de propiedad) y el filtro valida y explica el valor recibido.
- jQuery Validate registraba un error de consola con `step` en los campos `datetime-local` →
  se quitó el atributo (los segundos de un registro que ya los tiene se conservan desde la
  corrección de la sección 5).
- El registro del `HttpClient` ocultaba la cadena de consulta (`?*`), lo que impedía mostrar
  qué filtro llegaba al API → se activó `System.Net.Http.DisableUriRedaction` solo en el MVC.
- El pie de página absoluto dejaba un espacio en blanco en las capturas → pie estático.
- Las pruebas comparaban montos con un espacio no separable que Razor codifica como `&#xA0;`
  → las aserciones usan el texto codificado.
- La columna del monto desbordaba en el maestro/detalle (punto 7).

## 4. Sugerencias y decisiones del agente

- El MVC solo conoce la URL del API; nunca la cadena de conexión ni los repositorios. El origen
  de datos lo decide el API con `OrigenDatos`. Son dos configuraciones distintas.
- No se agregó un selector visual de proveedor: el ejercicio pide cambiar `appsettings.json`.
  Para tener ambos orígenes a la vez se documentan dos procesos del API en puertos distintos.
- El JavaScript consulta acciones del propio MVC (mismo origen) en lugar de llamar al API desde
  el navegador: no hace falta habilitar CORS en el API y el navegador no conoce su dirección.
- Los campos calculados se muestran tal como llegan del API; el MVC no recalcula montos.
- Fechas sin zona horaria de punta a punta, igual que la columna `timestamp without time zone`.
- Para la nube del profesor, el mismo cambio de `appsettings.json` funciona con el secreto
  `ConnectionStrings:ParqueoDb` del equipo del estudiante.

## 5. Revisión independiente

Para que el trabajo no se calificara a sí mismo, el agente pidió a un segundo agente una
revisión sobre una copia del repositorio (sin modificar el original), con los requisitos del
prompt y la instrucción de reportar solo defectos reproducidos. Hallazgos confirmados y
corrección (commit `3863a28`):

- Editar un ingreso sin cambios recortaba los segundos de entrada y salida (un registro con
  79,5 minutos pasaba a 80 y el monto cambiaba) → el formulario conserva los segundos cuando
  el registro los tiene y las vistas los muestran.
- Una fecha con zona horaria enviada al formulario se convertía a UTC antes de llegar al API
  → se rechaza con un mensaje y se conserva lo escrito.
- Cambiar el tipo mientras llegaba el cuerpo de una respuesta mostraba un falso «No se pudo
  conectar con el servidor» → `obtenerJson` propaga la cancelación y toda cancelación
  invalida el número de solicitud. La prueba en navegador agrega un proxy local que demora el
  cuerpo; falla con el JavaScript anterior (`revision-js-anterior-etapa2.txt`) y pasa con el
  corregido.
- Con el API caído al guardar se perdía lo escrito → el formulario se vuelve a mostrar con el
  motivo y código 503.
- `/Automoviles` mostraba un 503 del API (base de datos caída) como un 200 «rechazó la
  consulta» → conserva el código.
- Una página enorme (`pagina=2147483647`) mostraba el error crudo del API → la página pedida
  se limita y el listado lleva a la última página real.

También se ajustaron detalles señalados como posibles: los extremos deshabilitados de la
paginación ya no son enlaces, el filtro marca `aria-invalid` y el mensaje del 409 no repite
«No se puede borrar». Se agregaron pruebas de llaves foráneas inexistentes y de fechas mal
escritas en ingresos. Quedó como decisión de diseño documentada el límite inferior de 1886
para el año del filtro.

Resultado final (`docs/evidencias/tc4/final-*`): compilación sin advertencias; con PostgreSQL
local, 255/255 (`Parqueo.Tests`), 187/187 (`Parqueo.Api.Tests`) y 121/121
(`Parqueo.Mvc.Tests`); sin la base, las pruebas de PostgreSQL quedan omitidas (104, 92 y 56)
y no se cuentan como aprobadas. Las pruebas en navegador de las etapas 1 y 2 pasan en ambos
orígenes y la base local termina con 6 automóviles, 3 parqueos y 17 ingresos.

## 6. Entrega y límites del registro

- **Push.** No se publicó la rama desde el entorno del agente: la conexión de GitHub de la
  sesión no tiene permiso de escritura sobre `vanbrunt99/Parqueo-Programacion-Avanzada` y el
  equipo del estudiante, que tiene las credenciales del repositorio, no estaba conectado. No
  se cambiaron permisos ni visibilidad. Se entregan el ZIP del proyecto y un bundle de Git con
  todas las ramas.
- **Base del profesor.** No se consultó ni se modificó la instancia del curso en la nube; la
  verificación de `BaseDatos` usó una base PostgreSQL local, identificada así en cada
  evidencia.
- **Postman.** La colección del Trabajo 03 se ejecutó con Newman (línea de comandos de
  Postman), no con la aplicación de escritorio.
- **Credenciales.** Ninguna contraseña ni cadena de conexión está en el repositorio, el Word
  ni los archivos de entrega.
- **Sección 1 de la plantilla.** Menciona el agente de Visual Studio Code; esta sesión no
  ocurrió en Visual Studio Code, por eso se identifica la herramienta realmente usada.
