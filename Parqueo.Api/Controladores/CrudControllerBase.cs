using Microsoft.AspNetCore.Mvc;
using Parqueo.BusinessLogic.Interfaces;

namespace Parqueo.Api.Controladores;

/// <summary>
/// Controlador CRUD genérico (idea mostrada en la clase 6 para no repetir código):
/// consulta por llave primaria (GET), inserción (POST), actualización (PUT) y borrado
/// (DELETE) llamando al repositorio. Los controladores concretos agregan sus consultas
/// (GET con filtros) y la conversión entre entidades y DTO.
/// </summary>
/// <typeparam name="TEntidad">Entidad del scaffold.</typeparam>
/// <typeparam name="TSolicitud">Cuerpo que recibe el API en POST y PUT.</typeparam>
/// <typeparam name="TRespuesta">Objeto que devuelve el API.</typeparam>
[ApiController]
[Produces("application/json")]
public abstract class CrudControllerBase<TEntidad, TSolicitud, TRespuesta> : ControllerBase
    where TEntidad : class
{
    protected CrudControllerBase(IRepositorio<TEntidad> repositorio)
    {
        Repositorio = repositorio;
    }

    protected IRepositorio<TEntidad> Repositorio { get; }

    /// <summary>Nombre del recurso para los mensajes ("el automóvil", "el parqueo"...).</summary>
    protected abstract string NombreRecurso { get; }

    protected abstract int LlaveDe(TEntidad entidad);

    /// <summary>Convierte la solicitud en entidad; <paramref name="id"/> es 0 al crear.</summary>
    protected abstract TEntidad AEntidad(TSolicitud solicitud, int id);

    /// <summary>Respuesta con sus propiedades de navegación (consulta por llave primaria).</summary>
    protected abstract TRespuesta ARespuesta(TEntidad entidad);

    /// <summary>Consulta por llave primaria, con las propiedades de navegación cargadas.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TRespuesta>> ObtenerPorId(int id, CancellationToken cancelacion)
    {
        var entidad = await Repositorio.ObtenerPorIdAsync(id, cancelacion);
        return entidad is null ? NoEncontrado(id) : Ok(ARespuesta(entidad));
    }

    /// <summary>Crea el registro; la llave la asigna el origen de datos.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TRespuesta>> Crear([FromBody] TSolicitud solicitud, CancellationToken cancelacion)
    {
        var creado = await Repositorio.InsertarAsync(AEntidad(solicitud, 0), cancelacion);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = LlaveDe(creado) }, ARespuesta(creado));
    }

    /// <summary>Reemplaza los datos del registro indicado y devuelve cómo quedó.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TRespuesta>> Actualizar(int id, [FromBody] TSolicitud solicitud, CancellationToken cancelacion)
    {
        if (!await Repositorio.ActualizarAsync(AEntidad(solicitud, id), cancelacion))
        {
            return NoEncontrado(id);
        }

        var actualizado = await Repositorio.ObtenerPorIdAsync(id, cancelacion);
        return actualizado is null ? NoEncontrado(id) : Ok(ARespuesta(actualizado));
    }

    /// <summary>Borra el registro; 409 si tiene ingresos que lo referencian.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Borrar(int id, CancellationToken cancelacion)
    {
        return await Repositorio.BorrarAsync(id, cancelacion) ? NoContent() : NoEncontrado(id);
    }

    protected ObjectResult NoEncontrado(int id) =>
        Problem(
            statusCode: StatusCodes.Status404NotFound,
            title: "Registro no encontrado",
            detail: $"No existe {NombreRecurso} con id {id}.");
}
