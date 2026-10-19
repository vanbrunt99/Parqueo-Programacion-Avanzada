# Parqueo — Programación Avanzada (BSI-19)

## Verificación en la nube del curso el 19 de octubre de 2026

Se ejecutaron los scripts y el scaffold contra la instancia PostgreSQL del profesor,
usando exclusivamente el esquema propio del estudiante `user_13`. Las tres tablas
contienen 6 automóviles, 3 parqueos y 17 ingresos, dos sin salida. El modelo generado
incluye explícitamente el esquema `user_13` y no contiene contraseñas.

La compilación terminó sin errores ni advertencias. Las **231 pruebas** pasaron contra
la nube, sin omisiones, y la salida de las consultas coincide con JSON salvo el encabezado
que identifica el origen. Evidencias: `docs/evidencias/11-verificacion-nube-2026-10-19.txt`.

La conexión se configura en User Secrets con la clave `ConnectionStrings:ParqueoDb`.
Para repetir el scaffold en este esquema, usar los nombres calificados
`user_13.prq_automovil`, `user_13.prq_parqueo` y `user_13.prq_ingreso_automovil` en
las tres opciones `--table` del comando de la sección 3. El proveedor puede incluir
metadatos de ENUM ajenos aunque se limiten las tablas: se retiraron esos metadatos
del contexto porque ninguna tabla PRQ usa ENUM.

El repositorio de entrega es privado:
https://github.com/vanbrunt99/Parqueo-Programacion-Avanzada
El profesor necesita invitación de GitHub para abrirlo; también se entrega el ZIP y el
bundle completos en una carpeta personal de OneDrive compartida únicamente con él.

Las evidencias 01 a 10 corresponden a la ejecución local histórica de Claude. No deben
confundirse con la nueva ejecución en la nube. El registro de Claude disponible contiene
el prompt y un resumen; todavía debe incorporarse la conversación original completa
al documento académico si se dispone de ella.

Trabajo en Clase N.º 2: **Diseño de tablas en el motor default y clases de modelo repositorio en C#**.

- Estudiante: Jonathan Van Brunt Valerio
- Profesor: MSc. Héctor Egérico Fernández Méndez
- Tecnología: C# · .NET 10 · Entity Framework Core 10 · Npgsql · PostgreSQL · xUnit

Sistema de parqueo de automóviles con tres tablas de prefijo `PRQ`, scaffold de Entity
Framework Core, campos calculados de la estadía y repositorios que consultan, insertan,
actualizan y borran tanto desde PostgreSQL (contexto de EF) como desde archivos JSON
locales, detrás de las mismas interfaces.

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
│   ├── Validacion/                   # reglas comunes a EF y JSON
│   ├── Repositorios/BaseDatos/       # implementación con el contexto de EF Core
│   ├── Repositorios/Json/            # implementación con archivos JSON
│   └── ConfiguracionServicios.cs     # inyección de dependencias: elige JSON o BaseDatos
├── Parqueo.AppConsole/               # consola de verificación; guarda el secreto (User Secrets)
├── Parqueo.Tests/                    # pruebas xUnit (JSON siempre; PostgreSQL opcional)
├── docs/                             # conversación con el agente y evidencias de ejecución
├── dotnet-tools.json                 # herramienta local dotnet-ef 10.0.13
└── Parqueo.slnx
```

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

## Requisitos

- .NET SDK 10 (`dotnet --list-sdks`)
- Acceso a la base PostgreSQL del curso con su usuario y contraseña (para la parte de base de datos)
- `psql` u otro cliente SQL para ejecutar los scripts (opcional: pgAdmin, DBeaver, extensión de VS Code)

```powershell
dotnet tool restore     # instala dotnet-ef 10.0.13 desde dotnet-tools.json
dotnet restore
dotnet build Parqueo.slnx
```

## 1. Crear las tablas y los datos en la instancia en la nube

Antes de ejecutar, confirme la base, el esquema y el usuario:

```sql
SELECT current_database(), current_schema(), current_user;
```

Luego ejecute los scripts en orden (psql pide la contraseña; no la escriba en el comando):

```powershell
psql "host=<servidor> port=5432 dbname=<base> user=<usuario> sslmode=require" -v ON_ERROR_STOP=1 -f "BaseDatos/diseño-bd.sql"
psql "host=<servidor> port=5432 dbname=<base> user=<usuario> sslmode=require" -v ON_ERROR_STOP=1 -f "BaseDatos/insertar-registros.sql"
```

Los scripts trabajan dentro de una transacción y no borran nada: si una tabla o un
registro ya existe, fallan sin dejar cambios parciales. `insertar-registros.sql` termina
con dos consultas de verificación (conteos y montos calculados en SQL).

## 2. Guardar la cadena de conexión como secreto

El secreto se guarda en el perfil del usuario de Windows
(`%APPDATA%\Microsoft\UserSecrets\99f7960e-6480-4a43-b911-104b6ecd8154\secrets.json`), fuera
del repositorio. La clave es `ConnectionStrings:ParqueoDb`.

```powershell
$cadena = Read-Host "Cadena de conexión" -MaskInput   # PowerShell 7; en Windows PowerShell 5.1 omita -MaskInput
# Ejemplo: Host=<servidor>;Port=5432;Database=<base>;Username=<usuario>;Password=<contraseña>;SSL Mode=Require
dotnet user-secrets set "ConnectionStrings:ParqueoDb" $cadena --project Parqueo.AppConsole
dotnet user-secrets list --project Parqueo.AppConsole   # verificar (muestra el valor: no lo comparta)
```

## 3. Scaffold de las tablas PRQ

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

## 4. Ejecutar la consola de verificación

```powershell
dotnet run --project Parqueo.AppConsole                                   # origen JSON (predeterminado)
dotnet run --project Parqueo.AppConsole -- --OrigenDatos=BaseDatos         # PostgreSQL con el secreto
dotnet run --project Parqueo.AppConsole -- --OrigenDatos=BaseDatos --Crud=true
```

La consola ejecuta todas las consultas del enunciado. `--Crud=true` agrega una
demostración de insertar, actualizar y borrar en las tres tablas que al final elimina lo
que insertó. Con JSON trabaja sobre la copia de `Datos/` que se coloca junto al
ejecutable (`bin/.../Datos`), por lo que nunca modifica los archivos entregados.

## 5. Pruebas

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

Las consultas y el CRUD se prueban con **las mismas aserciones** contra JSON y contra
PostgreSQL (clases base abstractas en `Parqueo.Tests/Repositorios`), así se comprueba que
ambos orígenes devuelven resultados equivalentes. Las pruebas de escritura en PostgreSQL
corren dentro de una transacción que se revierte; las de JSON usan una carpeta temporal.

## Repositorios e interfaces

| Interfaz | Métodos |
|---|---|
| `IRepositorio<T>` | `ObtenerPorIdAsync`, `InsertarAsync`, `ActualizarAsync`, `BorrarAsync` |
| `IAutomovilRepositorio` | `BuscarAsync(FiltroAutomovil)`: color, intervalo de años, fabricante y tipo |
| `IParqueoRepositorio` | `BuscarAsync(FiltroParqueo)`: provincia, nombre y rango de precio por hora |
| `IIngresoAutomovilRepositorio` | `ObtenerPrecioPorHoraPorParqueo(idParqueo)`, `ListarPorTipoAsync(tipo, desde, hasta)`, `ListarPorProvinciaAsync(provincia, desde, hasta)` |

Cada interfaz tiene dos implementaciones: `...RepositorioEf` (contexto de EF Core) y
`...RepositorioJson` (archivos). `ConfiguracionServicios.AgregarRepositoriosParqueo`
registra una u otra según `OrigenDatos`; quien usa las interfaces (por ejemplo,
`Demostracion` en la consola) no cambia.

Reglas de las consultas:

- **Búsquedas aproximadas**: el texto puede aparecer en cualquier parte del valor, sin
  distinguir mayúsculas ni tildes (`"sedan"` encuentra `sedán`, `"san jose"` encuentra
  `San José`). La misma expresión regular se usa en JSON y en PostgreSQL (Npgsql la
  traduce al operador `~`).
- **Intervalos** de años, precios y fechas: ambos extremos incluidos. Los listados de
  ingresos filtran por la fecha y hora de **entrada** y se ordenan por ella. El extremo
  `hasta` es un instante exacto: para incluir todo un día use, por ejemplo,
  `2026-10-08 23:59:59` (con `2026-10-08` a secas se toma la medianoche de ese día).
- Los filtros son opcionales y se combinan; un intervalo invertido produce `ArgumentException`.
- **Paginación** opcional (`new Paginacion(pagina, tamano)`), con `Skip`/`Take` como en la clase 6.
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
  JSON. Insertar con una llave existente produce `InvalidOperationException`; con otra
  llave distinta de 0, `ArgumentException`.
- `ActualizarAsync` y `BorrarAsync` devuelven `false` si el registro no existe.
- No se puede borrar un automóvil o parqueo con ingresos, ni guardar un ingreso que apunte
  a registros inexistentes (en ambos orígenes).
- Las validaciones (`Validacion/ValidadorEntidades.cs`) replican las restricciones `CHECK`
  del SQL, así JSON acepta exactamente los mismos datos que PostgreSQL.
- Diferencia esperada: después de borrar el último registro, JSON reutiliza el número y
  PostgreSQL no (las secuencias no retroceden).

## Flujo de trabajo en Git

Rama de trabajo `feature/trabajo-en-clase-02` con un commit por etapa del enunciado,
fusionada en `main`:

1. Esquema PRQ, datos semilla y archivos JSON.
2. Secreto, scaffold y campos personalizados.
3. Interfaces de repositorio y consultas desde el contexto y JSON.
4. Insertar, actualizar y borrar desde el contexto y JSON.

## Evidencias

La carpeta `docs/evidencias` guarda salidas reales de la ejecución en el entorno de
desarrollo del agente (PostgreSQL 16 **local**, no la instancia del profesor): creación de
tablas, inserción de datos, restricciones rechazadas, scaffold, consola con JSON y con
PostgreSQL, SQL generado por EF Core y resultados de compilación y pruebas.
`docs/conversacion-agente.md` contiene el prompt completo y el resumen de la sesión con el
agente de IA.

## Seguridad

- Ninguna contraseña ni cadena de conexión real está en el repositorio. El `.gitignore`
  excluye `bin/`, `obj/`, `secrets.json` y archivos `.env`.
- La consola solo muestra servidor, base y usuario de la conexión, nunca la contraseña.
