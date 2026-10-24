using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Parqueo.Api.Dtos;

/// <summary>
/// Cuerpo de POST y PUT de /api/parqueos. Solo se exige que vengan los campos; la provincia
/// permitida, el precio mayor que 0 y con máximo 2 decimales los valida el modelo.
/// </summary>
public sealed class ParqueoSolicitud
{
    [Required(ErrorMessage = "El nombre de la provincia es obligatorio.")]
    public string? NombreProvincia { get; init; }

    [Required(ErrorMessage = "El nombre del parqueo es obligatorio.")]
    public string? Nombre { get; init; }

    [Required(ErrorMessage = "El precio por hora es obligatorio.")]
    public decimal? PrecioPorHora { get; init; }
}

/// <summary>
/// Parqueo devuelto por el API. <see cref="Ingresos"/> solo aparece en la consulta por
/// llave primaria; en los listados se omite.
/// </summary>
public sealed record ParqueoRespuesta(
    int Id,
    string NombreProvincia,
    string Nombre,
    decimal PrecioPorHora,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<IngresoDeParqueoRespuesta>? Ingresos = null);

/// <summary>Ingreso visto desde un parqueo: incluye el automóvil, no el parqueo (evita ciclos).</summary>
public sealed record IngresoDeParqueoRespuesta(
    int Consecutivo,
    DateTime FechaHoraEntrada,
    DateTime? FechaHoraSalida,
    decimal? DuracionEstadiaMinutos,
    decimal? DuracionEstadiaHoras,
    decimal? MontoTotalAPagar,
    AutomovilResumen Automovil);
