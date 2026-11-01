using Microsoft.AspNetCore.Mvc;
using Parqueo.Mvc.Infraestructura;
using Parqueo.Mvc.Modelos;
using Parqueo.Mvc.Servicios;

namespace Parqueo.Mvc.Controladores;

/// <summary>
/// Base de los controladores MVC: guarda el cliente del API y comparte la presentación del
/// origen de datos, de los errores del API y de las páginas "no encontrado".
/// </summary>
public abstract class ControladorBase : Controller
{
    protected ControladorBase(ClienteApiParqueo api)
    {
        Api = api;
    }

    protected ClienteApiParqueo Api { get; }

    /// <summary>
    /// Página más alta que se pide al API. Un número mayor (por ejemplo 2147483647) desbordaría
    /// el cálculo del desplazamiento en el API; con este límite el API responde una página vacía
    /// y el listado lleva a la última página real.
    /// </summary>
    protected const int PaginaMaxima = 100_000;

    /// <summary>
    /// Envía un formulario al API. Si el API no responde, agrega el motivo al resumen de errores,
    /// fija el código 503 y devuelve null: la acción vuelve a mostrar el formulario con lo escrito
    /// en lugar de perderlo en la página genérica de "API no disponible".
    /// </summary>
    protected async Task<RespuestaApi<T>?> EnviarFormularioAsync<T>(Func<Task<RespuestaApi<T>>> llamada)
    {
        try
        {
            return await llamada();
        }
        catch (Exception error) when (FiltroApiNoDisponible.EsApiNoDisponible(error, HttpContext))
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<ControladorBase>>()
                .LogWarning(error, "No se pudo comunicar con el API en {Url} al enviar un formulario", Api.UrlBase);
            ModelState.AddModelError(string.Empty, FiltroApiNoDisponible.Mensaje(Api.UrlBase) + " Los datos escritos se conservan; vuelva a guardar cuando el API esté disponible.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return null;
        }
    }

    /// <summary>
    /// Muestra en la página qué origen atendió la solicitud según el encabezado X-Origen-Datos
    /// que devolvió el API (no es una etiqueta fija del MVC).
    /// </summary>
    protected void RegistrarOrigen<T>(RespuestaApi<T> respuesta)
    {
        if (respuesta.OrigenDatos is not null)
        {
            ViewData["OrigenDatos"] = respuesta.OrigenDatos;
        }
    }

    /// <summary>
    /// Pasa los errores del API al formulario: los de cada campo (ValidationProblemDetails)
    /// junto al campo y el detalle general (por ejemplo, "la salida debe ser posterior a la
    /// entrada" o un conflicto 409) en el resumen de validación.
    /// </summary>
    /// <param name="camposFormulario">Propiedades del formulario; un error de otro campo va al resumen.</param>
    protected void AgregarErroresApi<T>(RespuestaApi<T> respuesta, params string[] camposFormulario)
    {
        var problema = respuesta.Problema;
        if (problema is null)
        {
            return;
        }

        bool agregado = false;
        foreach (var (campo, mensajes) in problema.Errors ?? new Dictionary<string, string[]>())
        {
            string nombre = campo.TrimStart('$', '.');
            string clave = camposFormulario.FirstOrDefault(c => string.Equals(c, nombre, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            foreach (string mensaje in mensajes)
            {
                ModelState.AddModelError(clave, mensaje);
                agregado = true;
            }
        }

        if (!agregado || !string.IsNullOrWhiteSpace(problema.Detail))
        {
            ModelState.AddModelError(string.Empty, $"El API rechazó la operación: {respuesta.MensajeError}");
        }
    }

    protected ViewResult NoEncontrado(string titulo, string detalle)
    {
        var vista = View("NoEncontrado", new MensajeViewModel(titulo, detalle));
        vista.StatusCode = StatusCodes.Status404NotFound;
        return vista;
    }

    /// <summary>Error del API que no corresponde a un formulario (500, 503 de base de datos...).</summary>
    protected ViewResult ErrorApi<T>(RespuestaApi<T> respuesta)
    {
        var vista = View("ErrorApi", new MensajeViewModel(respuesta.Problema?.Title ?? "Error del API", respuesta.MensajeError ?? string.Empty));
        vista.StatusCode = (int)respuesta.Estado;
        return vista;
    }

    /// <summary>URL local de regreso (conserva el filtro y la página del listado).</summary>
    protected string UrlVolver(string? volver, string accionPredeterminada = "Index") =>
        !string.IsNullOrWhiteSpace(volver) && Url.IsLocalUrl(volver) ? volver : Url.Action(accionPredeterminada)!;

    protected void Aviso(string mensaje) => TempData["Aviso"] = mensaje;
}
