using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.BusinessLogic.Repositorios.BaseDatos;
using Parqueo.BusinessLogic.Repositorios.Json;
using Parqueo.Model.Contexto;

namespace Parqueo.BusinessLogic;

/// <summary>Origen de datos de los repositorios.</summary>
public enum OrigenDatos
{
    /// <summary>Archivos JSON locales (no requiere conexión a la base de datos).</summary>
    Json,

    /// <summary>PostgreSQL mediante el contexto de Entity Framework Core.</summary>
    BaseDatos,
}

/// <summary>
/// Registro de los repositorios en el contenedor de inyección de dependencias.
/// Quien consume las interfaces no sabe si los datos vienen de JSON o de PostgreSQL:
/// solo cambia el origen elegido aquí.
/// </summary>
public static class ConfiguracionServicios
{
    public static IServiceCollection AgregarRepositoriosParqueo(
        this IServiceCollection servicios,
        OrigenDatos origen,
        string? cadenaConexion = null,
        string? carpetaJson = null)
    {
        switch (origen)
        {
            case OrigenDatos.BaseDatos:
                if (string.IsNullOrWhiteSpace(cadenaConexion))
                {
                    throw new InvalidOperationException(
                        "Falta la cadena de conexión 'ConnectionStrings:ParqueoDb'. Configúrela con dotnet user-secrets.");
                }

                servicios.AddDbContext<ParqueoContext>(opciones => opciones.UseNpgsql(cadenaConexion));
                servicios.AddScoped<IAutomovilRepositorio, AutomovilRepositorioEf>();
                servicios.AddScoped<IParqueoRepositorio, ParqueoRepositorioEf>();
                servicios.AddScoped<IIngresoAutomovilRepositorio, IngresoAutomovilRepositorioEf>();
                break;

            case OrigenDatos.Json:
                if (string.IsNullOrWhiteSpace(carpetaJson))
                {
                    throw new InvalidOperationException("Falta la carpeta de los archivos JSON.");
                }

                servicios.AddSingleton(new FuenteDatosJson(carpetaJson));
                servicios.AddScoped<IAutomovilRepositorio, AutomovilRepositorioJson>();
                servicios.AddScoped<IParqueoRepositorio, ParqueoRepositorioJson>();
                servicios.AddScoped<IIngresoAutomovilRepositorio, IngresoAutomovilRepositorioJson>();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(origen), origen, "Origen de datos no soportado.");
        }

        return servicios;
    }
}
