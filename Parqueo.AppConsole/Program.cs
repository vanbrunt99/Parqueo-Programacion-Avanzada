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

// Origen de datos: "Json" (predeterminado) o "BaseDatos".
// Ejemplo: dotnet run --project Parqueo.AppConsole -- --OrigenDatos=BaseDatos
string textoOrigen = builder.Configuration["OrigenDatos"] ?? nameof(OrigenDatos.Json);
if (!Enum.TryParse(textoOrigen, ignoreCase: true, out OrigenDatos origen))
{
    Console.WriteLine($"OrigenDatos inválido: '{textoOrigen}'. Use Json o BaseDatos.");
    return 1;
}

string? cadenaConexion = builder.Configuration.GetConnectionString("ParqueoDb");
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
