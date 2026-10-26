using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Parqueo.BusinessLogic.Validacion;

namespace Parqueo.Api.Infraestructura;

/// <summary>
/// Convierte las excepciones de los repositorios en respuestas HTTP con formato
/// ProblemDetails (RFC 9457), sin exponer detalles internos ni credenciales.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><see cref="ArgumentException"/> lanzada por el código de Parqueo (validaciones del
/// modelo, filtros, paginación, fechas): datos inválidos → 400. Una ArgumentException de una
/// biblioteca (por ejemplo, una cadena de conexión mal formada) es un error del servidor → 500.</item>
/// <item><see cref="ReglaNegocioException"/>: llave duplicada, referencia inexistente o borrado
/// de un registro con ingresos → 409.</item>
/// <item><see cref="NpgsqlException"/> de conexión: la base de datos no está disponible → 503.</item>
/// <item>Cualquier otro error → 500 con un mensaje genérico (el detalle queda en el log).</item>
/// </list>
/// </remarks>
public sealed class ManejadorExcepciones : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ManejadorExcepciones> _log;

    public ManejadorExcepciones(IProblemDetailsService problemDetails, ILogger<ManejadorExcepciones> log)
    {
        _problemDetails = problemDetails;
        _log = log;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception error, CancellationToken cancelacion)
    {
        (int estado, string titulo, string detalle) = error switch
        {
            ArgumentException e when EsValidacionPropia(e) => (StatusCodes.Status400BadRequest, "Datos inválidos", e.Message),
            ReglaNegocioException e => (StatusCodes.Status409Conflict, "Conflicto con los datos existentes", e.Message),
            _ when EsErrorDeConexion(error) => (StatusCodes.Status503ServiceUnavailable, "Base de datos no disponible",
                "No se pudo comunicar con PostgreSQL. Revise la cadena de conexión y que el servidor esté en línea."),
            InvalidDataException e => (StatusCodes.Status500InternalServerError, "Datos JSON inconsistentes", e.Message),
            _ => (StatusCodes.Status500InternalServerError, "Error interno",
                "Ocurrió un error inesperado. El detalle quedó registrado en el servidor."),
        };

        if (estado >= StatusCodes.Status500InternalServerError)
        {
            _log.LogError(error, "Error no controlado al atender {Metodo} {Ruta}", contexto.Request.Method, contexto.Request.Path);
        }

        contexto.Response.StatusCode = estado;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            Exception = error,
            ProblemDetails = new ProblemDetails { Status = estado, Title = titulo, Detail = detalle },
        });
    }

    // Las validaciones de Parqueo lanzan ArgumentException desde sus propios tipos (también
    // dentro de métodos async, cuyo tipo generado está anidado en el mismo espacio de nombres).
    private static bool EsValidacionPropia(ArgumentException error) =>
        error.TargetSite?.DeclaringType?.Namespace?.StartsWith("Parqueo.", StringComparison.Ordinal) == true;

    // PostgresException (error devuelto por el servidor, p. ej. permisos) también es una
    // NpgsqlException, pero no indica falta de conexión: esa se responde como 500.
    private static bool EsErrorDeConexion(Exception error) =>
        error is NpgsqlException and not PostgresException
        || error.InnerException is NpgsqlException and not PostgresException;
}
