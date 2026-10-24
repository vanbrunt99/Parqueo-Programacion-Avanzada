using Microsoft.AspNetCore.Mvc;
using Parqueo.Api.Dtos;
using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Entidades;

namespace Parqueo.Api.Controladores;

/// <summary>
/// API de automóviles (tabla prq_automovil).
/// GET /api/automoviles (listado con filtros), GET /api/automoviles/{id},
/// POST /api/automoviles, PUT /api/automoviles/{id}, DELETE /api/automoviles/{id}.
/// </summary>
[Route("api/automoviles")]
public sealed class AutomovilesController : CrudControllerBase<PrqAutomovil, AutomovilSolicitud, AutomovilRespuesta>
{
    private readonly IAutomovilRepositorio _automoviles;

    public AutomovilesController(IAutomovilRepositorio automoviles)
        : base(automoviles)
    {
        _automoviles = automoviles;
    }

    protected override string NombreRecurso => "el automóvil";

    protected override int LlaveDe(PrqAutomovil entidad) => entidad.Id;

    protected override PrqAutomovil AEntidad(AutomovilSolicitud solicitud, int id) => solicitud.AEntidad(id);

    protected override AutomovilRespuesta ARespuesta(PrqAutomovil entidad) => entidad.ARespuestaConIngresos();

    /// <summary>
    /// Lista de automóviles por color, intervalo de año, nombre del fabricante y nombre
    /// del tipo. Todos los filtros son opcionales y se combinan; los de texto son
    /// aproximados (sin distinguir mayúsculas ni tildes) y el intervalo de años incluye
    /// ambos extremos. Sin filtros devuelve todos, paginados.
    /// </summary>
    /// <example>GET /api/automoviles?tipo=sedan&amp;anioDesde=2015&amp;anioHasta=2019</example>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<AutomovilRespuesta>>> Listar(
        [FromQuery] string? color,
        [FromQuery] int? anioDesde,
        [FromQuery] int? anioHasta,
        [FromQuery] string? fabricante,
        [FromQuery] string? tipo,
        [FromQuery] ParametrosPaginacion paginacion,
        CancellationToken cancelacion)
    {
        var filtro = new FiltroAutomovil(color, anioDesde, anioHasta, fabricante, tipo);
        var automoviles = await _automoviles.BuscarAsync(filtro, paginacion.ACriterio(), cancelacion);
        return Ok(automoviles.Select(a => a.ARespuesta()).ToList());
    }
}
