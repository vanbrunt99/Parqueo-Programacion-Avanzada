using Microsoft.AspNetCore.Mvc;
using Parqueo.Mvc.Infraestructura;
using Parqueo.Mvc.Modelos;
using Parqueo.Mvc.Servicios;

namespace Parqueo.Mvc.Controladores;

/// <summary>
/// Consulta maestro/detalle con JavaScript (segunda etapa del Trabajo 04). La vista muestra el
/// combo de tipos; el JavaScript (wwwroot/js/maestro-detalle.js) pide al MVC, en el mismo
/// origen, los automóviles del tipo y luego los ingresos del automóvil seleccionado. El MVC
/// obtiene esos datos del API por HTTP (GET /api/automoviles?tipo=... y
/// GET /api/automoviles/{id}) y devuelve JSON: el navegador nunca llama al API directamente,
/// así no se necesita CORS.
/// </summary>
public sealed class MaestroDetalleController : ControladorBase
{
    public MaestroDetalleController(ClienteApiParqueo api)
        : base(api)
    {
    }

    [HttpGet]
    public IActionResult Index() =>
        View(new MaestroDetalleViewModel(
            TiposAutomovil.Opciones(),
            Url.Action(nameof(Automoviles))!,
            Url.Action(nameof(Ingresos))!,
            Url.Action(nameof(AutomovilesController.Detalles), "Automoviles")!));

    /// <summary>Maestro: TODOS los automóviles del tipo (recorre las páginas del API).</summary>
    [HttpGet]
    public async Task<IActionResult> Automoviles(string? tipo, CancellationToken cancelacion)
    {
        if (!TiposAutomovil.EsValido(tipo))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Tipo inválido",
                detail: "Seleccione un tipo de automóvil válido: sedán, 4x4 o moto.");
        }

        var respuesta = await Api.ListarTodosLosAutomovilesAsync(tipo, cancelacion);
        if (!respuesta.Exito)
        {
            return ProblemaDelApi(respuesta);
        }

        var automoviles = respuesta.Datos!;
        return Json(new
        {
            tipo,
            origenDatos = respuesta.OrigenDatos,
            total = automoviles.Count,
            automoviles = automoviles.Select(a => new { a.Id, a.Color, a.Anio, a.Fabricante, a.Tipo }),
        });
    }

    /// <summary>
    /// Detalle: los ingresos del automóvil indicado, tomados de GET /api/automoviles/{id}
    /// (propiedad ingresos, con el parqueo anidado). Duración y monto vienen calculados por el
    /// API; son null mientras el automóvil no haya salido.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Ingresos(int id, CancellationToken cancelacion)
    {
        var respuesta = await Api.ObtenerAutomovilAsync(id, cancelacion);
        if (respuesta.NoEncontrado)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Automóvil no encontrado",
                detail: $"El API no tiene un automóvil con id {id}.");
        }

        if (!respuesta.Exito)
        {
            return ProblemaDelApi(respuesta);
        }

        var automovil = respuesta.Datos!;
        var ingresos = automovil.Ingresos ?? [];
        return Json(new
        {
            automovil = new { automovil.Id, automovil.Color, automovil.Anio, automovil.Fabricante, automovil.Tipo },
            origenDatos = respuesta.OrigenDatos,
            ingresos = ingresos
                .OrderBy(i => i.FechaHoraEntrada)
                .Select(i => new
                {
                    i.Consecutivo,
                    idParqueo = i.Parqueo.Id,
                    nombreParqueo = i.Parqueo.Nombre,
                    i.FechaHoraEntrada,
                    i.FechaHoraSalida,
                    i.DuracionEstadiaMinutos,
                    i.DuracionEstadiaHoras,
                    montoACancelar = i.MontoTotalAPagar,
                }),
        });
    }

    private ObjectResult ProblemaDelApi<T>(RespuestaApi<T> respuesta) =>
        Problem(statusCode: (int)respuesta.Estado, title: respuesta.Problema?.Title ?? "Error del API", detail: respuesta.MensajeError);
}
