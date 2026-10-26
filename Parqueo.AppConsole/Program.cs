using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Parqueo.AppConsole;
using Parqueo.BusinessLogic;

// Host: carga appsettings.json, variables de entorno, argumentos y User Secrets.
// Las herramientas de EF Core también lo usan para resolver
// "Name=ConnectionStrings:ParqueoDb" durante el scaffold.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory, // appsettings.json y Datos/ se copian junto al ejecutable
});
builder.Configuration.AddUserSecrets<Program>(optional: true);
// Se vuelven a agregar las variables de entorno y los argumentos para que tengan prioridad
// sobre User Secrets (por ejemplo, --ConnectionStrings:ParqueoDb=... para otra base).
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

// Origen de datos: "Json" (predeterminado) o "BaseDatos".
// Ejemplo: dotnet run --project Parqueo.AppConsole -- --OrigenDatos=BaseDatos
string textoOrigen = builder.Configuration["OrigenDatos"] ?? nameof(OrigenDatos.Json);
if (!Enum.TryParse(textoOrigen, ignoreCase: true, out OrigenDatos origen))
{
    Console.WriteLine($"OrigenDatos inválido: '{textoOrigen}'. Use Json o BaseDatos.");
    return 1;
}

string? cadenaConexion = builder.Configuration.GetConnectionString("ParqueoDb");
if (!string.IsNullOrWhiteSpace(cadenaConexion) && !CadenaValida(cadenaConexion))
{
    // No se muestra la cadena: puede contener la contraseña.
    Console.WriteLine("La cadena ConnectionStrings:ParqueoDb no tiene un formato válido. Vuelva a guardarla con dotnet user-secrets.");
    return 1;
}

// Herramientas para preparar la base del curso sin psql (ver HerramientasBaseDatos):
//   dotnet run --project Parqueo.AppConsole -- --EstadoBd=true
//   dotnet run --project Parqueo.AppConsole -- --EjecutarScripts="BaseDatos/diseño-bd.sql;BaseDatos/insertar-registros.sql"
bool estadoBd = builder.Configuration.GetValue<bool>("EstadoBd");
string? scripts = builder.Configuration["EjecutarScripts"];
if (estadoBd || !string.IsNullOrWhiteSpace(scripts))
{
    if (string.IsNullOrWhiteSpace(cadenaConexion))
    {
        Console.WriteLine("Falta la cadena ConnectionStrings:ParqueoDb (guárdela con dotnet user-secrets).");
        return 1;
    }

    Console.WriteLine($"Conexión: {DescribirConexion(cadenaConexion)}");
    var herramientas = new HerramientasBaseDatos(cadenaConexion);
    try
    {
        return estadoBd
            ? await herramientas.MostrarEstadoAsync()
            : await herramientas.EjecutarScriptsAsync(scripts!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
    catch (NpgsqlException error)
    {
        Console.WriteLine($"No se pudo usar la base de datos: {error.Message}");
        return 1;
    }
}
string carpetaJson = Path.GetFullPath(builder.Configuration["RutaDatosJson"] ?? "Datos", AppContext.BaseDirectory);

try
{
    builder.Services.AgregarRepositoriosParqueo(origen, cadenaConexion, carpetaJson);
}
catch (Exception error) when (error is InvalidOperationException or DirectoryNotFoundException)
{
    Console.WriteLine(error.Message);
    return 1;
}

builder.Services.AddScoped<Demostracion>();

using var host = builder.Build();

Console.WriteLine(origen == OrigenDatos.BaseDatos
    ? $"Origen de datos: PostgreSQL ({DescribirConexion(cadenaConexion!)})"
    : $"Origen de datos: archivos JSON ({carpetaJson})");

using var alcance = host.Services.CreateScope();
var demostracion = alcance.ServiceProvider.GetRequiredService<Demostracion>();
await demostracion.EjecutarConsultasAsync();

// Demostración opcional de insertar, actualizar y borrar: --Crud=true
if (builder.Configuration.GetValue<bool>("Crud"))
{
    await demostracion.EjecutarCrudAsync();
}

return 0;

// Muestra servidor, base y usuario, nunca la contraseña.
static string DescribirConexion(string cadena)
{
    var datos = new NpgsqlConnectionStringBuilder(cadena);
    return $"Host={datos.Host}; Database={datos.Database}; Username={datos.Username}";
}

static bool CadenaValida(string cadena)
{
    try
    {
        _ = new NpgsqlConnectionStringBuilder(cadena);
        return true;
    }
    catch (ArgumentException)
    {
        return false;
    }
}
