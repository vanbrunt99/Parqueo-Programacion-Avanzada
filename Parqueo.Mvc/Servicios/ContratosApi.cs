namespace Parqueo.Mvc.Servicios;

// Formato JSON que devuelve y recibe el API del Trabajo 03 (Parqueo.Api/Dtos). El MVC no
// referencia el proyecto del API: solo conoce su contrato HTTP. Los nombres de las
// propiedades coinciden con el JSON en camelCase (id, color, anio, fabricante, tipo...).
// Las fechas viajan sin zona horaria ("2026-10-05T12:00:00"), en hora local de Costa Rica.

/// <summary>Automóvil del API. <see cref="Ingresos"/> solo viene en GET /api/automoviles/{id}.</summary>
public sealed record AutomovilApi(
    int Id,
    string Color,
    int Anio,
    string Fabricante,
    string Tipo,
    IReadOnlyList<IngresoDeAutomovilApi>? Ingresos = null);

/// <summary>Ingreso visto desde un automóvil: trae el parqueo anidado (no hay idParqueo plano).</summary>
public sealed record IngresoDeAutomovilApi(
    int Consecutivo,
    DateTime FechaHoraEntrada,
    DateTime? FechaHoraSalida,
    decimal? DuracionEstadiaMinutos,
    decimal? DuracionEstadiaHoras,
    decimal? MontoTotalAPagar,
    ParqueoResumenApi Parqueo);

/// <summary>Parqueo anidado en otras respuestas.</summary>
public sealed record ParqueoResumenApi(int Id, string NombreProvincia, string Nombre, decimal PrecioPorHora);

/// <summary>Automóvil anidado en otras respuestas.</summary>
public sealed record AutomovilResumenApi(int Id, string Color, int Anio, string Fabricante, string Tipo);

/// <summary>Parqueo de GET /api/parqueos (sin ingresos en el listado).</summary>
public sealed record ParqueoApi(int Id, string NombreProvincia, string Nombre, decimal PrecioPorHora);

/// <summary>Ingreso de /api/ingresos con su automóvil, su parqueo y los campos calculados.</summary>
public sealed record IngresoApi(
    int Consecutivo,
    int IdParqueo,
    int IdAutomovil,
    DateTime FechaHoraEntrada,
    DateTime? FechaHoraSalida,
    decimal? DuracionEstadiaMinutos,
    decimal? DuracionEstadiaHoras,
    decimal? MontoTotalAPagar,
    AutomovilResumenApi Automovil,
    ParqueoResumenApi Parqueo);

/// <summary>Cuerpo de POST y PUT de /api/automoviles.</summary>
public sealed record AutomovilSolicitudApi(string Color, int Anio, string Fabricante, string Tipo);

/// <summary>
/// Cuerpo de POST y PUT de /api/ingresos. <see cref="FechaHoraSalida"/> se envía como null
/// cuando el automóvil no ha salido.
/// </summary>
public sealed record IngresoSolicitudApi(int IdParqueo, int IdAutomovil, DateTime FechaHoraEntrada, DateTime? FechaHoraSalida);

/// <summary>Error del API en formato ProblemDetails (RFC 9457), con los errores por campo si los hay.</summary>
public sealed record ProblemaApi(
    string? Title,
    string? Detail,
    int? Status,
    IReadOnlyDictionary<string, string[]>? Errors = null);

/// <summary>Página de un listado del API con el total informado en X-Total-Registros.</summary>
public sealed record PaginaApi<T>(IReadOnlyList<T> Registros, int Pagina, int TamanoPagina, int Total)
{
    public int TotalPaginas => Total == 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanoPagina);

    public int Desde => Registros.Count == 0 ? 0 : ((Pagina - 1) * TamanoPagina) + 1;

    public int Hasta => Desde == 0 ? 0 : Desde + Registros.Count - 1;
}
