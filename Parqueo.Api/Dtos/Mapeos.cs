using Parqueo.Model.Entidades;

namespace Parqueo.Api.Dtos;

/// <summary>
/// Conversión entre entidades del scaffold y DTO del API. Las respuestas anidan solo un
/// nivel de navegación y sin referencias de regreso, para que el JSON no tenga ciclos.
/// </summary>
public static class Mapeos
{
    // ------------------------------------------------------------------ automóviles

    public static PrqAutomovil AEntidad(this AutomovilSolicitud solicitud, int id) => new()
    {
        Id = id,
        Color = solicitud.Color!,
        Anio = solicitud.Anio!.Value,
        Fabricante = solicitud.Fabricante!,
        Tipo = solicitud.Tipo!,
    };

    /// <summary>Automóvil sin ingresos (listados).</summary>
    public static AutomovilRespuesta ARespuesta(this PrqAutomovil automovil) =>
        new(automovil.Id, automovil.Color, automovil.Anio, automovil.Fabricante, automovil.Tipo);

    /// <summary>Automóvil con sus ingresos y el parqueo de cada uno (consulta por id).</summary>
    public static AutomovilRespuesta ARespuestaConIngresos(this PrqAutomovil automovil) =>
        automovil.ARespuesta() with
        {
            Ingresos = automovil.PrqIngresoAutomovils
                .OrderBy(i => i.FechaHoraEntrada)
                .Select(i => new IngresoDeAutomovilRespuesta(
                    i.Consecutivo,
                    i.FechaHoraEntrada,
                    i.FechaHoraSalida,
                    Minutos(i.DuracionEstadiaMinutos),
                    Horas(i.DuracionEstadiaHoras),
                    i.MontoTotalAPagar,
                    i.IdParqueoNavigation.AResumen()))
                .ToList(),
        };

    public static AutomovilResumen AResumen(this PrqAutomovil automovil) =>
        new(automovil.Id, automovil.Color, automovil.Anio, automovil.Fabricante, automovil.Tipo);

    // ------------------------------------------------------------------ parqueos

    public static ParqueoResumen AResumen(this PrqParqueo parqueo) =>
        new(parqueo.Id, parqueo.NombreProvincia, parqueo.Nombre, parqueo.PrecioPorHora);

    // ------------------------------------------------------------------ campos calculados

    /// <summary>
    /// Presentación de la duración: minutos con 2 decimales y horas con 4. Es solo
    /// formato de salida; el monto se calcula en el modelo con la duración exacta.
    /// </summary>
    public static decimal? Minutos(decimal? minutos) => minutos is decimal m ? Math.Round(m, 2) : null;

    public static decimal? Horas(decimal? horas) => horas is decimal h ? Math.Round(h, 4) : null;
}
