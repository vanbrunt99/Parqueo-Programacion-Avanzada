# Parqueo — Programación Avanzada (BSI-19)

- Estudiante: Jonathan Van Brunt Valerio
- Profesor: MSc. Héctor Egérico Fernández Méndez
- Tecnología: C# · .NET 10 · ASP.NET Core · Entity Framework Core 10 · Npgsql · PostgreSQL · xUnit · Postman

| Trabajo | Tema | Rama |
|---|---|---|
| Trabajo en Clase N.º 2 | Diseño de tablas en el motor default y clases de modelo repositorio en C# | `feature/trabajo-en-clase-02` (fusionada en `main`) |
| Trabajo en Clase N.º 3 | Diseño de API's en C# | `feature/trabajo-en-clase-03` |

Sistema de parqueo de automóviles con tres tablas de prefijo `PRQ`, scaffold de Entity
Framework Core, campos calculados de la estadía, repositorios que trabajan igual con
PostgreSQL (contexto de EF) y con archivos JSON locales, y un API REST que expone
consultas, inserción, actualización y borrado de las tres tablas.

## Estructura

```
Parqueo-Programacion-Avanzada/
├── BaseDatos/
│   ├── diseño-bd.sql                 # tablas, llaves, restricciones e índices
│   ├── insertar-registros.sql        # 6 automóviles, 3 parqueos, 17 ingresos
│   └── verificar-restricciones.sql   # casos que deben ser rechazados (opcional)
├── Datos/                            # un JSON por tabla (mismos datos que el SQL)
│   ├── automoviles.json
│   ├── parqueos.json
│   └── ingresos-automoviles.json
├── Parqueo.Model/                    # proyecto de modelo (sin secretos)
│   ├── Contexto/ParqueoContext.cs    # generado por scaffold
│   ├── Entidades/Prq*.cs             # generado por scaffold
│   └── Personalizados/               # campos calculados (clase parcial, no la toca el scaffold)
├── Parqueo.BusinessLogic/            # repositorios
│   ├── Interfaces/                   # IRepositorio<T>, IAutomovilRepositorio, IParqueoRepositorio, IIngresoAutomovilRepositorio
│   ├── Consultas/                    # filtros, búsqueda aproximada, paginación, resumen de ingresos
│   ├── Validacion/                   # reglas comunes a EF y JSON, ReglaNegocioException
│   ├── Repositorios/BaseDatos/       # implementación con el contexto de EF Core
│   ├── Repositorios/Json/            # implementación con archivos JSON
│   └── ConfiguracionServicios.cs     # inyección de dependencias: elige JSON o BaseDatos
├── Parqueo.Api/                      # API REST (Trabajo 03)
│   ├── Controladores/                # CrudControllerBase, Automoviles, Parqueos, Ingresos
│   ├── Dtos/                         # solicitudes, respuestas y conversión desde las entidades
│   ├── Infraestructura/              # manejo de errores (ProblemDetails)
│   └── Program.cs                    # origen de datos, secreto, controladores
├── Parqueo.AppConsole/               # consola de verificación del Trabajo 02
├── Parqueo.Tests/                    # pruebas de modelo y repositorios (JSON y PostgreSQL)
├── Parqueo.Api.Tests/                # pruebas HTTP del API (JSON y PostgreSQL)
├── Postman/                          # colección y entorno de Postman
├── docs/                             # conversación con el agente y evidencias de ejecución
├── dotnet-tools.json                 # herramienta local dotnet-ef 10.0.13
└── Parqueo.slnx
```

## Requisitos e instalación

- .NET SDK 10 (`dotnet --list-sdks`)
- Postman (aplicación de escritorio o web) para las pruebas del API
- Para el origen `BaseDatos`: acceso a la base PostgreSQL del curso con las tablas PRQ

```powershell
dotnet tool restore     # instala dotnet-ef 10.0.13 desde dotnet-tools.json
dotnet restore
dotnet build Parqueo.slnx
```

---

# Trabajo en Clase N.º 3 — API REST

## Ejecutar el API

```powershell
dotnet run --project Parqueo.Api --launch-profile http                              # origen JSON (predeterminado)
dotnet run --project Parqueo.Api --launch-profile http -- --OrigenDatos=BaseDatos    # PostgreSQL con el secreto
```

El API escucha en `http://localhost:5246`. Cada respuesta, incluidas las de error, trae el
encabezado **`X-Origen-Datos`** con el valor `Json` o `BaseDatos`, para comprobar desde
Postman qué origen respondió. En ambiente Development el documento OpenAPI queda en
`http://localhost:5246/openapi/v1.json`.

- **Origen JSON**: lee y escribe la copia de `Datos/` que el compilador coloca junto al
  ejecutable (`Parqueo.Api/bin/Debug/net10.0/Datos`). Los archivos entregados en `Datos/`
  nunca se modifican. Los cambios hechos por el API se conservan entre ejecuciones; para
  volver a los datos originales borre esa carpeta `bin/.../Datos` y compile de nuevo, o
  indique otra carpeta con `--RutaDatosJson=<ruta>`.
- **Origen BaseDatos**: usa la cadena `ConnectionStrings:ParqueoDb` de User Secrets. El
  API comparte el `UserSecretsId` de `Parqueo.AppConsole`, así que el secreto del Trabajo 02
  sirve sin configurar nada más (ver la sección "Guardar la cadena de conexión"). ASP.NET Core
  carga User Secrets solo en el ambiente `Development`, que es el que fija el perfil `http`;
  una variable de entorno `ConnectionStrings__ParqueoDb` o el argumento
  `--ConnectionStrings:ParqueoDb=...` tienen prioridad sobre el secreto.

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/automoviles?color=&anioDesde=&anioHasta=&fabricante=&tipo=&pagina=&tamanoPagina=` | Listado con filtros opcionales y combinables |
| GET | `/api/automoviles/{id}` | Automóvil con sus ingresos y el parqueo de cada ingreso |
| POST | `/api/automoviles` | Crea (`{ "color", "anio", "fabricante", "tipo" }`) → 201 |
| PUT | `/api/automoviles/{id}` | Actualiza → 200 con el registro actualizado |
| DELETE | `/api/automoviles/{id}` | Borra → 204; 409 si tiene ingresos |
| GET | `/api/parqueos?provincia=&nombre=&precioMinimo=&precioMaximo=&pagina=&tamanoPagina=` | Listado con filtros |
| GET | `/api/parqueos/{id}` | Parqueo con sus ingresos y el automóvil de cada ingreso |
| POST / PUT / DELETE | `/api/parqueos[/{id}]` | `{ "nombreProvincia", "nombre", "precioPorHora" }` |
| GET | `/api/ingresos?pagina=&tamanoPagina=` | Ingresos con automóvil, parqueo y campos calculados |
| GET | `/api/ingresos/{id}` | Ingreso con automóvil, parqueo y campos calculados |
| GET | `/api/ingresos/por-tipo?tipo=&desde=&hasta=` | Automóviles de un tipo que ingresaron en el intervalo, con entrada, salida y monto |
| GET | `/api/ingresos/por-provincia?provincia=&desde=&hasta=` | Igual, por provincia del parqueo |
| GET | `/api/ingresos/precio-por-hora/{idParqueo}` | `ObtenerPrecioPorHoraPorParqueo` |
| POST / PUT / DELETE | `/api/ingresos[/{id}]` | `{ "idParqueo", "idAutomovil", "fechaHoraEntrada", "fechaHoraSalida" }` |

- Paginación de los listados como en la clase 6: `pagina` (por defecto 1) y `tamanoPagina`
  (por defecto 10, máximo 100).
- Fechas sin zona horaria, en hora local de Costa Rica: `2026-10-18T09:00:00`. Una fecha
  con zona (`...Z` o `...-06:00`) se rechaza con 400 en lugar de convertirla, para que JSON y
  PostgreSQL guarden exactamente la misma hora. Las fechas del ingreso deben estar entre
  2000-01-01 y 2100-12-31.
- Los campos personalizados del ingreso son `duracionEstadiaMinutos` (2 decimales),
  `duracionEstadiaHoras` (4 decimales, solo presentación) y `montoTotalAPagar`; los tres
  son `null` cuando `fechaHoraSalida` es `null`. El monto se calcula con la duración exacta.
- Las propiedades de navegación se devuelven con un solo nivel y sin referencias de regreso
  (un ingreso dentro de un automóvil trae su parqueo, pero no vuelve a traer el automóvil),
  por eso el JSON no tiene ciclos.

Respuestas de error (formato ProblemDetails):

| Código | Cuándo |
|---|---|
| 400 | Datos o parámetros inválidos: campos faltantes, tipo o provincia no permitidos, precio ≤ 0, salida no posterior a la entrada, intervalos invertidos, fechas mal escritas, con zona horaria o fuera de 2000–2100, caracteres de control en los textos, paginación fuera de rango |
| 404 | El id no existe |
| 409 | Conflicto con los datos: borrar un automóvil o parqueo con ingresos, ingreso que apunta a un parqueo o automóvil inexistente (en PostgreSQL, solo las violaciones de integridad, SQLSTATE clase 23) |
| 503 | No hay conexión con PostgreSQL |

## Pruebas con Postman

1. En Postman, **Import** → `Postman/Parqueo-API.postman_collection.json` y
   `Postman/Parqueo-Local.postman_environment.json`.
2. Seleccione el entorno **Parqueo API local** (`baseUrl = http://localhost:5246`).
3. Inicie el API con el origen que quiere probar y ejecute la colección completa
   (**Run collection**) o carpeta por carpeta, en orden:
   1. Automóviles · 2. Parqueos · 3. Ingresos · 4. Consultas especializadas · 5. Pruebas negativas y de borde.
4. Cada POST guarda el id creado en una variable de colección que usan el GET, PUT y DELETE
   siguientes; al final el registro creado se borra, así que los datos quedan como estaban.
   Cada solicitud trae pruebas (pestaña **Test Results**) y la primera de ellas muestra el
   origen de datos que respondió.
5. Protecciones de la colección: el POST limpia el id de una corrida anterior, y si el POST
   no se ejecutó o falló, las solicitudes que dependen de ese id se **omiten** (mensaje en la
   consola de Postman) en lugar de llamar a `/api/.../` con el id vacío. Los listados
   completos exigen los registros iniciales pero toleran registros adicionales; los filtros
   esperan exactamente los datos de `BaseDatos/insertar-registros.sql`.

La colección tiene 74 solicitudes: 13 de automóviles, 13 de parqueos, 9 de ingresos, 7
consultas especializadas y 32 pruebas negativas y de borde.

La misma colección puede ejecutarse por línea de comandos con Newman:

```powershell
npm install -g newman
newman run Postman/Parqueo-API.postman_collection.json -e Postman/Parqueo-Local.postman_environment.json
```

---

# Trabajo en Clase N.º 2 — Base de datos y repositorios

## Modelo de datos

| Tabla | Columnas | Restricciones |
|---|---|---|
| `prq_automovil` | `id`, `color`, `anio`, `fabricante`, `tipo` | PK identidad; `tipo` ∈ {sedán, 4x4, moto}; `anio` entre 1886 y 2100; textos no vacíos |
| `prq_parqueo` | `id`, `nombre_provincia`, `nombre`, `precio_por_hora numeric(10,2)` | PK identidad; provincia de Costa Rica; precio > 0 |
| `prq_ingreso_automovil` | `consecutivo`, `id_parqueo`, `id_automovil`, `fecha_hora_entrada`, `fecha_hora_salida` (NULL = no ha salido) | PK identidad; FK a parqueo y automóvil con `ON DELETE RESTRICT`; salida posterior a la entrada |

Decisiones de diseño que el enunciado no fija (documentadas también en el script):

- Nombres en minúscula y en singular (`PRQ_AUTOMOVIL` y `prq_automovil` son el mismo
  identificador en PostgreSQL); así el scaffold genera `PrqAutomovil`, `PrqParqueo` y
  `PrqIngresoAutomovil`.
- El tipo de automóvil es `varchar` con restricción `CHECK`, no un `ENUM` nativo: evita
  el error de mapeo de enums de Npgsql visto en la clase 6 y no agrega una cuarta tabla.
- Fechas `timestamp without time zone` (hora local de Costa Rica, sin horario de verano).
- Dinero en `numeric(10,2)`, nunca en tipos de punto flotante.

## Crear las tablas y los datos en la instancia en la nube

Antes de ejecutar, confirme la base, el esquema y el usuario:

```sql
SELECT current_database(), current_schema(), current_user;
```

Luego ejecute los scripts en orden (psql pide la contraseña; no la escriba en el comando;
ajuste `sslmode` según la instancia):

```powershell
psql "host=<servidor> port=5432 dbname=<base> user=<usuario> sslmode=require" -v ON_ERROR_STOP=1 -f "BaseDatos/diseño-bd.sql"
psql "host=<servidor> port=5432 dbname=<base> user=<usuario> sslmode=require" -v ON_ERROR_STOP=1 -f "BaseDatos/insertar-registros.sql"
```

Los scripts trabajan dentro de una transacción y no borran nada: si una tabla o un
registro ya existe, fallan sin dejar cambios parciales. `insertar-registros.sql` termina
con dos consultas de verificación (conteos y montos calculados en SQL).

## Guardar la cadena de conexión como secreto

El secreto se guarda en el perfil del usuario de Windows
(`%APPDATA%\Microsoft\UserSecrets\99f7960e-6480-4a43-b911-104b6ecd8154\secrets.json`), fuera
del repositorio. La clave es `ConnectionStrings:ParqueoDb` y la usan la consola y el API.

```powershell
$cadena = Read-Host "Cadena de conexión" -MaskInput   # PowerShell 7; en Windows PowerShell 5.1 omita -MaskInput
# Ejemplo: Host=<servidor>;Port=5432;Database=<base>;Username=<usuario>;Password=<contraseña>;SSL Mode=Require
dotnet user-secrets set "ConnectionStrings:ParqueoDb" $cadena --project Parqueo.AppConsole
dotnet user-secrets list --project Parqueo.AppConsole   # verificar (muestra el valor: no lo comparta)
```

## Scaffold de las tablas PRQ

El scaffold lee la cadena desde User Secrets mediante `Name=` (no se escribe en la consola
ni en el código) y solo incluye las tres tablas del parqueo:

```powershell
dotnet ef dbcontext scaffold "Name=ConnectionStrings:ParqueoDb" Npgsql.EntityFrameworkCore.PostgreSQL `
  --project Parqueo.Model --startup-project Parqueo.AppConsole `
  --table prq_automovil --table prq_parqueo --table prq_ingreso_automovil `
  --context ParqueoContext --context-dir Contexto --output-dir Entidades `
  --namespace Parqueo.Model.Entidades --context-namespace Parqueo.Model.Contexto `
  --no-onconfiguring --force
```

`--no-onconfiguring` evita que la cadena quede escrita en el contexto; la conexión se
configura por inyección de dependencias. `--force` permite regenerar: los campos
personalizados están en `Parqueo.Model/Personalizados/` y no se sobrescriben.

## Consola de verificación

```powershell
dotnet run --project Parqueo.AppConsole                                   # origen JSON (predeterminado)
dotnet run --project Parqueo.AppConsole -- --OrigenDatos=BaseDatos         # PostgreSQL con el secreto
dotnet run --project Parqueo.AppConsole -- --OrigenDatos=BaseDatos --Crud=true
```

## Repositorios e interfaces

| Interfaz | Métodos |
|---|---|
| `IRepositorio<T>` | `ObtenerPorIdAsync`, `InsertarAsync`, `ActualizarAsync`, `BorrarAsync` |
| `IAutomovilRepositorio` | `BuscarAsync(FiltroAutomovil)`: color, intervalo de años, fabricante y tipo |
| `IParqueoRepositorio` | `BuscarAsync(FiltroParqueo)`: provincia, nombre y rango de precio por hora |
| `IIngresoAutomovilRepositorio` | `ListarAsync`, `ObtenerPrecioPorHoraPorParqueo(idParqueo)`, `ListarPorTipoAsync(tipo, desde, hasta)`, `ListarPorProvinciaAsync(provincia, desde, hasta)` |

Cada interfaz tiene dos implementaciones: `...RepositorioEf` (contexto de EF Core) y
`...RepositorioJson` (archivos). `ConfiguracionServicios.AgregarRepositoriosParqueo`
registra una u otra según `OrigenDatos`; quien usa las interfaces (los controladores del API
y la consola) no cambia. La consulta por id carga las propiedades de navegación con
`Include`/`ThenInclude` en EF; la versión JSON lee los otros archivos y enlaza los objetos.

Reglas de las consultas:

- **Búsquedas aproximadas**: el texto puede aparecer en cualquier parte del valor, sin
  distinguir mayúsculas ni tildes (`"sedan"` encuentra `sedán`, `"san jose"` encuentra
  `San José`). La misma expresión regular se usa en JSON y en PostgreSQL (Npgsql la
  traduce al operador `~`).
- **Intervalos** de años, precios y fechas: ambos extremos incluidos. Los listados de
  ingresos filtran por la fecha y hora de **entrada** y se ordenan por ella. El extremo
  `hasta` es un instante exacto: para incluir todo un día use, por ejemplo,
  `2026-10-08T23:59:59` (con `2026-10-08` a secas se toma la medianoche de ese día).
- Los filtros son opcionales y se combinan; un intervalo invertido produce `ArgumentException` (400 en el API).
- `ObtenerPrecioPorHoraPorParqueo` devuelve `null` si el parqueo no existe. Los listados
  por tipo y por provincia la invocan (una vez por parqueo distinto) para calcular el monto.

## Campos calculados

En `Parqueo.Model/Personalizados/PrqIngresoAutomovil.CamposPersonalizados.cs`:
`DuracionEstadiaMinutos`, `DuracionEstadiaHoras` y `MontoTotalAPagar` (`[NotMapped]`).
Los tres son `null` cuando `FechaHoraSalida` es `null`.

**Política de cobro provisional** (el enunciado no la define): cobro proporcional al tiempo
exacto (`horas × precio por hora`), redondeado a 2 decimales alejándose de cero, igual que
`round(numeric, 2)` en PostgreSQL. Ejemplo: 80 minutos a ₡1 250/h = ₡1 666,67. Si el
profesor indica cobrar horas completas, solo cambia `CalcularMontoTotal`.

## CRUD

- La llave primaria la asigna el origen: identidad en PostgreSQL y siguiente consecutivo en
  JSON. Insertar con una llave existente produce `ReglaNegocioException` (409 en el API);
  con otra llave distinta de 0, `ArgumentException` (400). En el API la llave nunca viene
  en el cuerpo: se toma de la URL.
- `ActualizarAsync` y `BorrarAsync` devuelven `false` si el registro no existe (404 en el API).
- No se puede borrar un automóvil o parqueo con ingresos, ni guardar un ingreso que apunte
  a registros inexistentes (en ambos orígenes).
- Las validaciones (`Validacion/ValidadorEntidades.cs`) replican las restricciones `CHECK`
  del SQL, así JSON acepta exactamente los mismos datos que PostgreSQL.
- Diferencia esperada: después de borrar el último registro, JSON reutiliza el número y
  PostgreSQL no (las secuencias no retroceden).
- En JSON, cada lectura-modificación-escritura (con sus validaciones) se ejecuta con un
  semáforo compartido por los tres archivos, así dos solicitudes simultáneas no se pisan.
  El bloqueo es dentro de un mismo proceso: no ejecute dos instancias del API sobre la
  misma carpeta de datos.

---

## Pruebas automatizadas

```powershell
dotnet test Parqueo.slnx
```

Sin configuración adicional se ejecutan las pruebas de JSON y de los campos calculados;
las de PostgreSQL aparecen como **omitidas** (Skipped). Para ejecutarlas, defina una base
con los dos scripts cargados (se recomienda una base local, no la del profesor):

```powershell
$env:PARQUEO_PRUEBAS_BD = "Host=localhost;Port=5432;Database=parqueo_local;Username=<usuario>;Password=<contraseña>"
dotnet test Parqueo.slnx
```

- `Parqueo.Tests`: modelo y repositorios. Las mismas aserciones se ejecutan contra JSON y
  contra PostgreSQL; la escritura en PostgreSQL ocurre dentro de una transacción que se revierte.
- `Parqueo.Api.Tests`: llamadas HTTP reales al API levantado en memoria con
  `WebApplicationFactory`, una vez con JSON (copia temporal de los datos) y otra con
  PostgreSQL. Las pruebas que crean registros los borran al final, aunque falle una
  aserción (`try/finally`). El API de prueba usa el ambiente `Pruebas`, así que nunca lee
  los User Secrets del desarrollador; una prueba sin base de datos comprueba que con una
  conexión inválida el API responde 503 sin exponer la cadena.
- Ambos proyectos toman un candado consultivo de PostgreSQL (`pg_advisory_lock`) durante
  sus pruebas de base de datos, para no ejecutarlas al mismo tiempo.

## Flujo de trabajo en Git

- `feature/trabajo-en-clase-02`, fusionada en `main`: esquema y datos; scaffold y campos
  personalizados; repositorios y consultas; CRUD; correcciones; documentación.
- `feature/trabajo-en-clase-03`, creada desde `main`: un commit por API, en el orden del
  enunciado (automóviles, parqueos, ingresos), cada uno con su carpeta de Postman ejecutada;
  un commit con las correcciones de una revisión independiente del código y un commit final
  de documentación.

## Evidencias

`docs/evidencias` guarda salidas reales de la ejecución en el entorno de desarrollo del
agente (PostgreSQL 16 **local**, no la instancia del profesor). En `docs/evidencias/tc3`
están, por etapa (`etapa1` a `etapa3`, una por commit de API), la ejecución de la colección
con Newman contra JSON y contra PostgreSQL, las respuestas HTTP reales extraídas de esas
ejecuciones y el resultado de `dotnet test`. Los archivos `final-*` repiten todo después
de las correcciones de la revisión; `final-newman-guardia-variable-vacia.txt` muestra que
una solicitud dependiente se omite si falta el id. Newman es el ejecutor de línea de
comandos de Postman: estas salidas **no** sustituyen las capturas de la aplicación Postman.
`docs/conversacion-agente.md` y `docs/conversacion-agente-tc3.md` contienen los prompts y el
resumen de las sesiones con el agente de IA.

## Seguridad

- Ninguna contraseña ni cadena de conexión real está en el repositorio. El `.gitignore`
  excluye `bin/`, `obj/`, `secrets.json` y archivos `.env`.
- La consola solo muestra servidor, base y usuario de la conexión, nunca la contraseña.
- Los errores 500 del API devuelven un mensaje genérico; el detalle queda en el registro
  del servidor.

## Limitaciones conocidas

- Los listados paginados no devuelven el total de registros (no hay `X-Total-Count`).
- No se valida que un mismo automóvil tenga dos estadías que se traslapan en el tiempo;
  el enunciado no lo pide y el script SQL tampoco lo restringe.
- La política de cobro proporcional es provisional hasta que el profesor la confirme.
