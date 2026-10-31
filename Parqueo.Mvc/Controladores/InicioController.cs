using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Parqueo.Mvc.Modelos;
using Parqueo.Mvc.Servicios;

namespace Parqueo.Mvc.Controladores;

/// <summary>Página de inicio: accesos a las pantallas y estado del API configurado.</summary>
public sealed class InicioController : ControladorBase
{
    public InicioController(ClienteApiParqueo api)
        : base(api)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancelacion)
    {
        try
        {
            // Una consulta mínima (1 registro) basta para conocer el total y el origen de datos.
            var respuesta = await Api.ListarAutomovilesAsync(null, null, null, 1, 1, cancelacion);
            RegistrarOrigen(respuesta);
            ViewData["TotalAutomoviles"] = respuesta.Exito ? respuesta.Datos!.Total : null;
            ViewData["EstadoApi"] = respuesta.Exito ? "en línea" : $"respondió {(int)respuesta.Estado}: {respuesta.MensajeError}";
        }
        catch (HttpRequestException)
        {
            ViewData["EstadoApi"] = "no disponible (no respondió)";
        }

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new MensajeViewModel("Error inesperado", $"Identificador de la solicitud: {Activity.Current?.Id ?? HttpContext.TraceIdentifier}."));
}
