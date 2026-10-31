using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Parqueo.Mvc.Infraestructura;
using Parqueo.Mvc.Modelos;
using Parqueo.Mvc.Servicios;

namespace Parqueo.Mvc.Controladores;

/// <summary>
/// Mantenimiento de ingresos de automóviles mediante /api/ingresos: listado paginado,
/// detalles, crear (entrada), editar (por ejemplo, registrar la salida) y borrar. Las
/// opciones de automóvil y parqueo se cargan del API recorriendo todas sus páginas.
/// </summary>
public sealed class IngresosController : ControladorBase
{
    public const int TamanoPaginaPredeterminado = 10;

    private static readonly string[] Campos = ["IdAutomovil", "IdParqueo", "FechaHoraEntrada", "FechaHoraSalida"];

    public IngresosController(ClienteApiParqueo api)
        : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pagina = 1, int tamanoPagina = TamanoPaginaPredeterminado, CancellationToken cancelacion = default)
    {
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, ClienteApiParqueo.TamanoMaximoPagina);
        var respuesta = await Api.ListarIngresosAsync(pagina, tamanoPagina, cancelacion);
        RegistrarOrigen(respuesta);
        if (!respuesta.Exito)
        {
            return ErrorApi(respuesta);
        }

        var resultado = respuesta.Datos!;
        if (resultado.Registros.Count == 0 && resultado.TotalPaginas > 0 && pagina > resultado.TotalPaginas)
        {
            return RedirectToAction(nameof(Index), new { pagina = resultado.TotalPaginas, tamanoPagina });
        }

        return View(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> Detalles(int id, string? volver, CancellationToken cancelacion)
    {
        var respuesta = await Api.ObtenerIngresoAsync(id, cancelacion);
        RegistrarOrigen(respuesta);
        if (respuesta.NoEncontrado)
        {
            return NoEncontrado("Ingreso no encontrado", $"El API no tiene un ingreso con consecutivo {id}.");
        }

        if (!respuesta.Exito)
        {
            return ErrorApi(respuesta);
        }

        ViewData["Volver"] = UrlVolver(volver);
        return View(respuesta.Datos);
    }

    [HttpGet]
    public async Task<IActionResult> Crear(int? idAutomovil, string? volver, CancellationToken cancelacion)
    {
        ViewData["Volver"] = UrlVolver(volver);
        var formulario = new IngresoFormularioViewModel { IdAutomovil = idAutomovil };
        return await MostrarFormularioAsync(formulario, cancelacion);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(IngresoFormularioViewModel formulario, string? volver, CancellationToken cancelacion)
    {
        ViewData["Volver"] = UrlVolver(volver);
        if (ModelState.IsValid)
        {
            var respuesta = await Api.CrearIngresoAsync(formulario.ASolicitud(), cancelacion);
            RegistrarOrigen(respuesta);
            if (respuesta.Exito)
            {
                Aviso($"Se registró el ingreso #{respuesta.Datos!.Consecutivo} (API respondió {(int)respuesta.Estado}, origen {respuesta.OrigenDatos}).");
                return RedirectToAction(nameof(Detalles), new { id = respuesta.Datos.Consecutivo, volver });
            }

            AgregarErroresApi(respuesta, Campos);
        }

        return await MostrarFormularioAsync(formulario, cancelacion);
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id, string? volver, CancellationToken cancelacion)
    {
        var respuesta = await Api.ObtenerIngresoAsync(id, cancelacion);
        RegistrarOrigen(respuesta);
        if (respuesta.NoEncontrado)
        {
            return NoEncontrado("Ingreso no encontrado", $"El API no tiene un ingreso con consecutivo {id}.");
        }

        if (!respuesta.Exito)
        {
            return ErrorApi(respuesta);
        }

        ViewData["Volver"] = UrlVolver(volver);
        return await MostrarFormularioAsync(IngresoFormularioViewModel.Desde(respuesta.Datos!), cancelacion);
    }

    [HttpPost]
    public async Task<IActionResult> Editar(int id, IngresoFormularioViewModel formulario, string? volver, CancellationToken cancelacion)
    {
        formulario.Consecutivo = id;
        ViewData["Volver"] = UrlVolver(volver);
        if (ModelState.IsValid)
        {
            var respuesta = await Api.ActualizarIngresoAsync(id, formulario.ASolicitud(), cancelacion);
            RegistrarOrigen(respuesta);
            if (respuesta.NoEncontrado)
            {
                return NoEncontrado("Ingreso no encontrado", $"El ingreso {id} ya no existe en el API.");
            }

            if (respuesta.Exito)
            {
                Aviso($"Se actualizó el ingreso #{id} (API respondió {(int)respuesta.Estado}, origen {respuesta.OrigenDatos}).");
                return RedirectToAction(nameof(Detalles), new { id, volver });
            }

            AgregarErroresApi(respuesta, Campos);
        }

        return await MostrarFormularioAsync(formulario, cancelacion);
    }

    [HttpGet]
    public async Task<IActionResult> Borrar(int id, string? volver, CancellationToken cancelacion)
    {
        var respuesta = await Api.ObtenerIngresoAsync(id, cancelacion);
        RegistrarOrigen(respuesta);
        if (respuesta.NoEncontrado)
        {
            return NoEncontrado("Ingreso no encontrado", $"El API no tiene un ingreso con consecutivo {id}.");
        }

        if (!respuesta.Exito)
        {
            return ErrorApi(respuesta);
        }

        ViewData["Volver"] = UrlVolver(volver);
        return View(new BorrarIngresoViewModel(respuesta.Datos!));
    }

    [HttpPost]
    [ActionName("Borrar")]
    public async Task<IActionResult> BorrarConfirmado(int id, string? volver, CancellationToken cancelacion)
    {
        ViewData["Volver"] = UrlVolver(volver);
        var respuesta = await Api.BorrarIngresoAsync(id, cancelacion);
        RegistrarOrigen(respuesta);
        if (respuesta.Exito)
        {
            Aviso($"Se borró el ingreso #{id} (API respondió {(int)respuesta.Estado}, origen {respuesta.OrigenDatos}).");
            return Redirect(UrlVolver(volver));
        }

        if (respuesta.NoEncontrado)
        {
            return NoEncontrado("Ingreso no encontrado", $"El ingreso {id} ya no existe en el API.");
        }

        var ingreso = await Api.ObtenerIngresoAsync(id, cancelacion);
        return ingreso.Exito
            ? View(new BorrarIngresoViewModel(ingreso.Datos!, $"El API no pudo borrar el ingreso: {respuesta.MensajeError}"))
            : ErrorApi(respuesta);
    }

    /// <summary>Carga del API las opciones de automóvil y parqueo (todas las páginas) y muestra el formulario.</summary>
    private async Task<IActionResult> MostrarFormularioAsync(IngresoFormularioViewModel formulario, CancellationToken cancelacion)
    {
        var automoviles = await Api.ListarTodosLosAutomovilesAsync(null, cancelacion);
        var parqueos = await Api.ListarTodosLosParqueosAsync(cancelacion);
        RegistrarOrigen(parqueos);
        if (!automoviles.Exito)
        {
            return ErrorApi(automoviles);
        }

        if (!parqueos.Exito)
        {
            return ErrorApi(parqueos);
        }

        formulario.Automoviles = automoviles.Datos!
            .Select(a => new SelectListItem($"#{a.Id} · {a.Fabricante} {a.Color} {a.Anio} ({TiposAutomovil.Etiqueta(a.Tipo)})", a.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), a.Id == formulario.IdAutomovil))
            .ToList();
        formulario.Parqueos = parqueos.Datos!
            .Select(p => new SelectListItem($"#{p.Id} · {p.Nombre} ({p.NombreProvincia}) · {Formatos.Precio(p.PrecioPorHora)}/h", p.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Id == formulario.IdParqueo))
            .ToList();
        return View(formulario);
    }
}
