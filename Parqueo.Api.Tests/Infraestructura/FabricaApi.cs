using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Parqueo.Api.Tests.Infraestructura;

/// <summary>
/// Levanta el API en memoria (sin abrir puertos) con el origen de datos indicado.
/// Las solicitudes HTTP recorren el mismo pipeline que en ejecución real:
/// enrutamiento, validación, controladores, repositorios y manejo de errores.
/// </summary>
public sealed class FabricaApi : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _configuracion;

    private FabricaApi(Dictionary<string, string?> configuracion)
    {
        _configuracion = configuracion;
    }

    /// <summary>API sobre una copia temporal de los JSON (Datos/ no se modifica).</summary>
    public static FabricaApi ConJson(string carpetaJson) => new(new()
    {
        ["OrigenDatos"] = "Json",
        ["RutaDatosJson"] = carpetaJson,
    });

    /// <summary>API sobre PostgreSQL con la cadena de la variable PARQUEO_PRUEBAS_BD.</summary>
    public static FabricaApi ConBaseDatos(string cadenaConexion) => new(new()
    {
        ["OrigenDatos"] = "BaseDatos",
        ["ConnectionStrings:ParqueoDb"] = cadenaConexion,
    });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        foreach (var (clave, valor) in _configuracion)
        {
            builder.UseSetting(clave, valor);
        }
    }
}

/// <summary>Configuración compartida de las pruebas contra PostgreSQL.</summary>
public static class BaseDatosPruebas
{
    public const string VariableEntorno = "PARQUEO_PRUEBAS_BD";

    public static string? CadenaConexion => Environment.GetEnvironmentVariable(VariableEntorno);

    public static string? MotivoOmision => string.IsNullOrWhiteSpace(CadenaConexion)
        ? $"Defina la variable de entorno {VariableEntorno} para probar el API contra PostgreSQL."
        : null;
}

/// <summary>Las pruebas de API contra PostgreSQL escriben datos reales: se ejecutan en serie.</summary>
[CollectionDefinition(Nombre, DisableParallelization = true)]
public sealed class ColeccionBaseDatos : ICollectionFixture<BloqueoBaseDatos>
{
    public const string Nombre = "ApiBaseDatos";
}

/// <summary>
/// Mientras dure la colección de pruebas contra PostgreSQL, mantiene un candado consultivo
/// (pg_advisory_lock) en la base. Parqueo.Tests y Parqueo.Api.Tests usan la misma llave,
/// así que sus pruebas de base de datos no se ejecutan al mismo tiempo aunque dotnet test
/// corra ambos proyectos en paralelo (unas cuentan registros y otras insertan).
/// </summary>
public sealed class BloqueoBaseDatos : IDisposable
{
    /// <summary>Llave compartida por los dos proyectos de prueba.</summary>
    public const long Llave = 20261031;

    private readonly Npgsql.NpgsqlConnection? _conexion;

    public BloqueoBaseDatos()
    {
        string? cadena = Environment.GetEnvironmentVariable("PARQUEO_PRUEBAS_BD");
        if (string.IsNullOrWhiteSpace(cadena))
        {
            return; // sin base configurada las pruebas se omiten; no hay nada que bloquear
        }

        _conexion = new Npgsql.NpgsqlConnection(cadena);
        _conexion.Open();
        using var comando = new Npgsql.NpgsqlCommand($"SELECT pg_advisory_lock({Llave})", _conexion);
        comando.ExecuteNonQuery();
    }

    public void Dispose()
    {
        if (_conexion is null)
        {
            return;
        }

        using (var comando = new Npgsql.NpgsqlCommand($"SELECT pg_advisory_unlock({Llave})", _conexion))
        {
            comando.ExecuteNonQuery();
        }

        _conexion.Dispose();
    }
}

/// <summary>Copia los JSON entregados a una carpeta temporal única.</summary>
public sealed class CarpetaJsonTemporal : IDisposable
{
    public CarpetaJsonTemporal()
    {
        Ruta = Path.Combine(Path.GetTempPath(), "parqueo-api-pruebas", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Ruta);
        foreach (string archivo in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "DatosOriginales"), "*.json"))
        {
            File.Copy(archivo, Path.Combine(Ruta, Path.GetFileName(archivo)));
        }
    }

    public string Ruta { get; }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Ruta, recursive: true);
        }
        catch (IOException)
        {
            // Una carpeta temporal que no se pudo borrar no invalida la prueba.
        }
    }
}

/// <summary>Atajos para leer respuestas JSON.</summary>
public static class ExtensionesHttp
{
    public static async Task<JsonElement> LeerJsonAsync(this HttpResponseMessage respuesta) =>
        await respuesta.Content.ReadFromJsonAsync<JsonElement>();

    public static int[] Ids(this JsonElement arreglo, string propiedad = "id") =>
        arreglo.EnumerateArray().Select(e => e.GetProperty(propiedad).GetInt32()).ToArray();

    public static decimal? DecimalONulo(this JsonElement elemento, string propiedad) =>
        elemento.GetProperty(propiedad).ValueKind == JsonValueKind.Null ? null : elemento.GetProperty(propiedad).GetDecimal();
}
