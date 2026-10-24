using Parqueo.BusinessLogic.Consultas;
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

    public static PrqParqueo AEntidad(this ParqueoSolicitud solicitud, int id) => new()
    {
        Id = id,
        NombreProvincia = solicitud.NombreProvincia!,
        Nombre = solicitud.Nombre!,
        PrecioPorHora = solicitud.PrecioPorHora!.Value,
    };

    /// <summary>Parqueo sin ingresos (listados).</summary>
    public static ParqueoRespuesta ARespuesta(this PrqParqueo parqueo) =>
        new(parqueo.Id, parqueo.NombreProvincia, parqueo.Nombre, parqueo.PrecioPorHora);

    /// <summary>Parqueo con sus ingresos y el automóvil de cada uno (consulta por id).</summary>
    public static ParqueoRespuesta ARespuestaConIngresos(this PrqParqueo parqueo) =>
        parqueo.ARespuesta() with
        {
            Ingresos = parqueo.PrqIngresoAutomovils
                .OrderBy(i => i.FechaHoraEntrada)
                .Select(i => new IngresoDeParqueoRespuesta(
                    i.Consecutivo,
                    i.FechaHoraEntrada,
                    i.FechaHoraSalida,
                    Minutos(i.DuracionEstadiaMinutos),
                    Horas(i.DuracionEstadiaHoras),
                    i.MontoTotalAPagar,
                    i.IdAutomovilNavigation.AResumen()))
                .ToList(),
        };

    public static ParqueoResumen AResumen(this PrqParqueo parqueo) =>
        new(parqueo.Id, parqueo.NombreProvincia, parqueo.Nombre, parqueo.PrecioPorHora);

    // ------------------------------------------------------------------ ingresos

    public static PrqIngresoAutomovil AEntidad(this IngresoSolicitud solicitud, int consecutivo) => new()
    {
        Consecutivo = consecutivo,
        IdParqueo = solicitud.IdParqueo!.Value,
        IdAutomovil = solicitud.IdAutomovil!.Value,
        FechaHoraEntrada = solicitud.FechaHoraEntrada!.Value,
        FechaHoraSalida = solicitud.FechaHoraSalida,
    };

    /// <summary>Requiere las navegaciones cargadas (los repositorios siempre las cargan).</summary>
    public static IngresoRespuesta ARespuesta(this PrqIngresoAutomovil ingreso) => new(
        ingreso.Consecutivo,
        ingreso.IdParqueo,
        ingreso.IdAutomovil,
        ingreso.FechaHoraEntrada,
        ingreso.FechaHoraSalida,
        Minutos(ingreso.DuracionEstadiaMinutos),
        Horas(ingreso.DuracionEstadiaHoras),
        ingreso.MontoTotalAPagar,
        ingreso.IdAutomovilNavigation.AResumen(),
        ingreso.IdParqueoNavigation.AResumen());

    public static IngresoListadoRespuesta ARespuesta(this IngresoAutomovilResumen resumen) => new(
        resumen.Consecutivo,
        new AutomovilResumen(resumen.IdAutomovil, resumen.Color, resumen.Anio, resumen.Fabricante, resumen.Tipo),
        resumen.IdParqueo,
        resumen.NombreParqueo,
        resumen.NombreProvincia,
        resumen.FechaHoraEntrada,
        resumen.FechaHoraSalida,
        Minutos(resumen.DuracionEstadiaMinutos),
        Horas(resumen.DuracionEstadiaHoras),
        resumen.PrecioPorHora,
        resumen.MontoACancelar);

    // ------------------------------------------------------------------ campos calculados

    /// <summary>
    /// Presentación de la duración: minutos con 2 decimales y horas con 4. Es solo
    /// formato de salida; el monto se calcula en el modelo con la duración exacta.
    /// </summary>
    public static decimal? Minutos(decimal? minutos) => minutos is decimal m ? Math.Round(m, 2) : null;

    public static decimal? Horas(decimal? horas) => horas is decimal h ? Math.Round(h, 4) : null;
}
