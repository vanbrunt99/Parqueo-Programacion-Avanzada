using Microsoft.EntityFrameworkCore;
using Parqueo.Model.Contexto;

namespace Parqueo.Tests.Infraestructura;

/// <summary>
/// Configuración de las pruebas de integración con PostgreSQL.
/// </summary>
/// <remarks>
/// Se ejecutan solo si existe la variable de entorno <c>PARQUEO_PRUEBAS_BD</c> con una
/// cadena de conexión a una base que tenga cargados diseño-bd.sql e
/// insertar-registros.sql. Si no existe, las pruebas se reportan como omitidas
/// (Skipped), nunca como aprobadas. Se recomienda una base local; las pruebas de
/// escritura se hacen dentro de una transacción que se revierte al final.
/// </remarks>
public static class BaseDatosPruebas
{
    public const string VariableEntorno = "PARQUEO_PRUEBAS_BD";

    public static string? CadenaConexion => Environment.GetEnvironmentVariable(VariableEntorno);

    public static string? MotivoOmision => string.IsNullOrWhiteSpace(CadenaConexion)
        ? $"Defina la variable de entorno {VariableEntorno} para ejecutar las pruebas contra PostgreSQL."
        : null;

    public static ParqueoContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ParqueoContext>()
            .UseNpgsql(CadenaConexion)
            .Options;
        return new ParqueoContext(opciones);
    }
}

/// <summary>
/// Las clases de prueba contra PostgreSQL se ejecutan en serie para que sus
/// transacciones no compitan por las mismas filas.
/// </summary>
[CollectionDefinition(Nombre, DisableParallelization = true)]
public sealed class ColeccionBaseDatos : ICollectionFixture<BloqueoBaseDatos>
{
    public const string Nombre = "BaseDatos";
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

