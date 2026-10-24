using System.ComponentModel.DataAnnotations;

namespace Parqueo.Api.Dtos;

/// <summary>
/// Cuerpo de POST y PUT de /api/ingresos. Las fechas se envían en hora local de Costa Rica
/// sin zona horaria (por ejemplo "2026-10-18T09:00:00"). La salida es opcional: si se
/// omite, el automóvil sigue en el parqueo. El modelo valida que la salida sea posterior
/// a la entrada y que existan el parqueo y el automóvil.
/// </summary>
public sealed class IngresoSolicitud
{
    [Required(ErrorMessage = "El id del parqueo es obligatorio.")]
    public int? IdParqueo { get; init; }

    [Required(ErrorMessage = "El id del automóvil es obligatorio.")]
    public int? IdAutomovil { get; init; }

    [Required(ErrorMessage = "La fecha y hora de entrada es obligatoria.")]
    public DateTime? FechaHoraEntrada { get; init; }

    /// <summary>Null (u omitida) mientras el automóvil no haya salido.</summary>
    public DateTime? FechaHoraSalida { get; init; }
}

/// <summary>
/// Ingreso devuelto por el API con sus propiedades de navegación (automóvil y parqueo) y
/// los tres campos personalizados. Si no hay salida, los tres campos son null.
/// </summary>
public sealed record IngresoRespuesta(
    int Consecutivo,
    int IdParqueo,
    int IdAutomovil,
    DateTime FechaHoraEntrada,
    DateTime? FechaHoraSalida,
    decimal? DuracionEstadiaMinutos,
    decimal? DuracionEstadiaHoras,
    decimal? MontoTotalAPagar,
    AutomovilResumen Automovil,
    ParqueoResumen Parqueo);

/// <summary>
/// Fila de los listados por tipo y por provincia: datos del automóvil, del parqueo, hora de
/// entrada, hora de salida y monto a cancelar (calculado con ObtenerPrecioPorHoraPorParqueo).
/// </summary>
public sealed record IngresoListadoRespuesta(
    int Consecutivo,
    AutomovilResumen Automovil,
    int IdParqueo,
    string NombreParqueo,
    string NombreProvincia,
    DateTime FechaHoraEntrada,
    DateTime? FechaHoraSalida,
    decimal? DuracionEstadiaMinutos,
    decimal? DuracionEstadiaHoras,
    decimal? PrecioPorHora,
    decimal? MontoACancelar);

/// <summary>Respuesta de GET /api/ingresos/precio-por-hora/{idParqueo}.</summary>
public sealed record PrecioPorHoraRespuesta(int IdParqueo, decimal PrecioPorHora);
