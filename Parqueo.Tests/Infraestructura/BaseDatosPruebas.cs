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
public sealed class ColeccionBaseDatos
{
    public const string Nombre = "BaseDatos";
}
