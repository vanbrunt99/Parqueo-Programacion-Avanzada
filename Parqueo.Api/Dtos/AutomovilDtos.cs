using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Parqueo.Api.Dtos;

/// <summary>
/// Cuerpo de POST y PUT de /api/automoviles. Aquí solo se exige que vengan los campos;
/// las reglas de contenido (tipo permitido, rango de años, longitudes) las aplica el
/// mismo validador que usan los repositorios, para no duplicarlas.
/// </summary>
public sealed class AutomovilSolicitud
{
    [Required(ErrorMessage = "El color es obligatorio.")]
    public string? Color { get; init; }

    [Required(ErrorMessage = "El año es obligatorio.")]
    public int? Anio { get; init; }

    [Required(ErrorMessage = "El fabricante es obligatorio.")]
    public string? Fabricante { get; init; }

    /// <summary>sedán, 4x4 o moto.</summary>
    [Required(ErrorMessage = "El tipo es obligatorio (sedán, 4x4 o moto).")]
    public string? Tipo { get; init; }
}

/// <summary>
/// Automóvil devuelto por el API. <see cref="Ingresos"/> solo aparece en la consulta por
/// llave primaria (propiedad de navegación de un nivel); en los listados se omite.
/// </summary>
public sealed record AutomovilRespuesta(
    int Id,
    string Color,
    int Anio,
    string Fabricante,
    string Tipo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<IngresoDeAutomovilRespuesta>? Ingresos = null);

/// <summary>Ingreso visto desde un automóvil: incluye el parqueo, no el automóvil (evita ciclos).</summary>
public sealed record IngresoDeAutomovilRespuesta(
    int Consecutivo,
    DateTime FechaHoraEntrada,
    DateTime? FechaHoraSalida,
    decimal? DuracionEstadiaMinutos,
    decimal? DuracionEstadiaHoras,
    decimal? MontoTotalAPagar,
    ParqueoResumen Parqueo);

/// <summary>Datos del automóvil anidados en otras respuestas (sin sus ingresos).</summary>
public sealed record AutomovilResumen(int Id, string Color, int Anio, string Fabricante, string Tipo);

/// <summary>Datos del parqueo anidados en otras respuestas (sin sus ingresos).</summary>
public sealed record ParqueoResumen(int Id, string NombreProvincia, string Nombre, decimal PrecioPorHora);
