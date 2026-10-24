using Microsoft.AspNetCore.Mvc;
using Parqueo.Api.Dtos;
using Parqueo.BusinessLogic.Consultas;
using Parqueo.BusinessLogic.Interfaces;
using Parqueo.Model.Entidades;

namespace Parqueo.Api.Controladores;

/// <summary>
/// API de parqueos (tabla prq_parqueo).
/// GET /api/parqueos (listado con filtros), GET /api/parqueos/{id},
/// POST /api/parqueos, PUT /api/parqueos/{id}, DELETE /api/parqueos/{id}.
/// </summary>
[Route("api/parqueos")]
public sealed class ParqueosController : CrudControllerBase<PrqParqueo, ParqueoSolicitud, ParqueoRespuesta>
{
    private readonly IParqueoRepositorio _parqueos;

    public ParqueosController(IParqueoRepositorio parqueos)
        : base(parqueos)
    {
        _parqueos = parqueos;
    }

    protected override string NombreRecurso => "el parqueo";

    protected override int LlaveDe(PrqParqueo entidad) => entidad.Id;

    protected override PrqParqueo AEntidad(ParqueoSolicitud solicitud, int id) => solicitud.AEntidad(id);

    protected override ParqueoRespuesta ARespuesta(PrqParqueo entidad) => entidad.ARespuestaConIngresos();

    /// <summary>
    /// Parqueos por nombre de provincia, nombre del parqueo y rango de precio por hora.
    /// Los filtros son opcionales y se combinan; provincia y nombre son aproximados y el
    /// rango de precios incluye ambos extremos. Sin filtros devuelve todos, paginados.
    /// </summary>
    /// <example>GET /api/parqueos?provincia=san jose&amp;precioMinimo=700&amp;precioMaximo=1250</example>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ParqueoRespuesta>>> Listar(
        [FromQuery] string? provincia,
        [FromQuery] string? nombre,
        [FromQuery] decimal? precioMinimo,
        [FromQuery] decimal? precioMaximo,
        [FromQuery] ParametrosPaginacion paginacion,
        CancellationToken cancelacion)
    {
        var filtro = new FiltroParqueo(provincia, nombre, precioMinimo, precioMaximo);
        var parqueos = await _parqueos.BuscarAsync(filtro, paginacion.ACriterio(), cancelacion);
        return Ok(parqueos.Select(p => p.ARespuesta()).ToList());
    }
}
