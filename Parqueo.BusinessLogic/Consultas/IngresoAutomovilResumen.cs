namespace Parqueo.BusinessLogic.Consultas;

/// <summary>
/// Registro de salida de las consultas de ingresos por tipo y por provincia.
/// Incluye los datos que identifican al automóvil y al parqueo, la hora de entrada,
/// la hora de salida y el monto a cancelar (nulo si el automóvil no ha salido).
/// </summary>
public sealed record IngresoAutomovilResumen(
    int Consecutivo,
    int IdAutomovil,
    string Color,
    int Anio,
    string Fabricante,
    string Tipo,
    int IdParqueo,
    string NombreParqueo,
    string NombreProvincia,
    DateTime FechaHoraEntrada,
    DateTime? FechaHoraSalida,
    decimal? DuracionEstadiaMinutos,
    decimal? DuracionEstadiaHoras,
    decimal? PrecioPorHora,
    decimal? MontoACancelar);
