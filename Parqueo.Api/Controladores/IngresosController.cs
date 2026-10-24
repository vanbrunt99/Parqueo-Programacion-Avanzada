using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Parqueo.Api.Dtos;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Entidades;

namespace Parqueo.Api.Controladores;

/// <summary>
/// API de ingresos de automóviles (tabla prq_ingreso_automovil).
/// GET /api/ingresos, GET /api/ingresos/{id}, GET /api/ingresos/por-tipo,
/// GET /api/ingresos/por-provincia, GET /api/ingresos/precio-por-hora/{idParqueo},
/// POST /api/ingresos, PUT /api/ingresos/{id}, DELETE /api/ingresos/{id}.
/// Cada ingreso se devuelve con su automóvil, su parqueo y los campos personalizados
/// (duración en minutos, en horas y monto total), que son null si no hay salida.
/// </summary>
[Route("api/ingresos")]
public sealed class IngresosController : CrudControllerBase<PrqIngresoAutomovil, IngresoSolicitud, IngresoRespuesta>
{
    private readonly IIngresoAutomovilRepositorio _ingresos;

    public IngresosController(IIngresoAutomovilRepositorio ingresos)
        : base(ingresos)
    {
        _ingresos = ingresos;
    }

    protected override string NombreRecurso => "el ingreso";

    protected override int LlaveDe(PrqIngresoAutomovil entidad) => entidad.Consecutivo;

    protected override PrqIngresoAutomovil AEntidad(IngresoSolicitud solicitud, int id) => solicitud.AEntidad(id);

    protected override IngresoRespuesta ARespuesta(PrqIngresoAutomovil entidad) => entidad.ARespuesta();

    /// <summary>Todos los ingresos (ordenados por consecutivo) con automóvil y parqueo, paginados.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<IngresoRespuesta>>> Listar(
        [FromQuery] ParametrosPaginacion paginacion,
        CancellationToken cancelacion)
    {
        var ingresos = await _ingresos.ListarAsync(paginacion.ACriterio(), cancelacion);
        return Ok(ingresos.Select(i => i.ARespuesta()).ToList());
    }

    /// <summary>
    /// Automóviles de un tipo (aproximado) que ingresaron entre <paramref name="desde"/> y
    /// <paramref name="hasta"/> (ambos incluidos, según la fecha y hora de entrada), con
    /// hora de entrada, hora de salida y monto a cancelar.
    /// </summary>
    /// <example>GET /api/ingresos/por-tipo?tipo=4x4&amp;desde=2026-10-01T00:00:00&amp;hasta=2026-10-31T23:59:59</example>
    [HttpGet("por-tipo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<IngresoListadoRespuesta>>> ListarPorTipo(
        [FromQuery, Required(ErrorMessage = "Indique el tipo de automóvil.")] string? tipo,
        [FromQuery, Required(ErrorMessage = "Indique la fecha y hora inicial (desde).")] DateTime? desde,
        [FromQuery, Required(ErrorMessage = "Indique la fecha y hora final (hasta).")] DateTime? hasta,
        [FromQuery] ParametrosPaginacion paginacion,
        CancellationToken cancelacion)
    {
        var resultado = await _ingresos.ListarPorTipoAsync(tipo!, desde!.Value, hasta!.Value, paginacion.ACriterio(), cancelacion);
        return Ok(resultado.Select(r => r.ARespuesta()).ToList());
    }

    /// <summary>
    /// Automóviles que ingresaron entre <paramref name="desde"/> y <paramref name="hasta"/>
    /// a parqueos de la provincia indicada (aproximada), con hora de entrada, hora de salida
    /// y monto a cancelar.
    /// </summary>
    /// <example>GET /api/ingresos/por-provincia?provincia=heredia&amp;desde=2026-10-01T00:00:00&amp;hasta=2026-10-31T23:59:59</example>
    [HttpGet("por-provincia")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<IngresoListadoRespuesta>>> ListarPorProvincia(
        [FromQuery, Required(ErrorMessage = "Indique la provincia.")] string? provincia,
        [FromQuery, Required(ErrorMessage = "Indique la fecha y hora inicial (desde).")] DateTime? desde,
        [FromQuery, Required(ErrorMessage = "Indique la fecha y hora final (hasta).")] DateTime? hasta,
        [FromQuery] ParametrosPaginacion paginacion,
        CancellationToken cancelacion)
    {
        var resultado = await _ingresos.ListarPorProvinciaAsync(provincia!, desde!.Value, hasta!.Value, paginacion.ACriterio(), cancelacion);
        return Ok(resultado.Select(r => r.ARespuesta()).ToList());
    }

    /// <summary>Función ObtenerPrecioPorHoraPorParqueo del Trabajo 02 expuesta por HTTP.</summary>
    [HttpGet("precio-por-hora/{idParqueo:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrecioPorHoraRespuesta>> ObtenerPrecioPorHora(int idParqueo, CancellationToken cancelacion)
    {
        decimal? precio = await _ingresos.ObtenerPrecioPorHoraPorParqueo(idParqueo, cancelacion);
        return precio is decimal valor
            ? Ok(new PrecioPorHoraRespuesta(idParqueo, valor))
            : Problem(statusCode: StatusCodes.Status404NotFound, title: "Registro no encontrado",
                detail: $"No existe el parqueo con id {idParqueo}.");
    }
}
