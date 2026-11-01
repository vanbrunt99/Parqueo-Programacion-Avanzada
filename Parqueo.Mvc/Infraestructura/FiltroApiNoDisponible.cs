using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Parqueo.Mvc.Modelos;
using Parqueo.Mvc.Servicios;

namespace Parqueo.Mvc.Infraestructura;

/// <summary>
/// Si el API no responde (no está levantado, URL incorrecta o se agotó el tiempo), muestra un
/// mensaje claro en lugar de la página de excepción: una vista para las pantallas y un
/// ProblemDetails 503 para las consultas que hace JavaScript.
/// </summary>
public sealed class FiltroApiNoDisponible : IExceptionFilter
{
    private readonly IModelMetadataProvider _metadatos;
    private readonly ClienteApiParqueo _api;
    private readonly ILogger<FiltroApiNoDisponible> _log;

    public FiltroApiNoDisponible(IModelMetadataProvider metadatos, ClienteApiParqueo api, ILogger<FiltroApiNoDisponible> log)
    {
        _metadatos = metadatos;
        _api = api;
        _log = log;
    }

    public void OnException(ExceptionContext contexto)
    {
        if (!EsApiNoDisponible(contexto.Exception, contexto.HttpContext))
        {
            return;
        }

        _log.LogWarning(contexto.Exception, "No se pudo comunicar con el API en {Url}", _api.UrlBase);
        string detalle = Mensaje(_api.UrlBase);

        if (EsSolicitudDeDatos(contexto.HttpContext.Request))
        {
            contexto.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "API no disponible",
                Detail = detalle,
            })
            { StatusCode = StatusCodes.Status503ServiceUnavailable };
        }
        else
        {
            contexto.Result = new ViewResult
            {
                ViewName = "ApiNoDisponible",
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                ViewData = new ViewDataDictionary(_metadatos, contexto.ModelState) { Model = new MensajeViewModel("API no disponible", detalle) },
            };
        }

        contexto.ExceptionHandled = true;
    }

    /// <summary>
    /// El API no respondió: no está levantado, la URL es incorrecta o se agotó el tiempo. Una
    /// cancelación porque el navegador abandonó la solicitud no cuenta.
    /// </summary>
    public static bool EsApiNoDisponible(Exception error, HttpContext contexto) =>
        error is HttpRequestException
        || (error is TaskCanceledException && !contexto.RequestAborted.IsCancellationRequested);

    public static string Mensaje(Uri? urlApi) =>
        $"No se pudo comunicar con el API del parqueo en {urlApi?.ToString() ?? "(sin configurar)"}. "
        + "Verifique que el API esté en ejecución y que Api:UrlBase apunte a él.";

    // Las consultas de JavaScript piden JSON (encabezado Accept: application/json).
    private static bool EsSolicitudDeDatos(HttpRequest solicitud) =>
        solicitud.Headers.Accept.Any(a => a?.Contains("application/json", StringComparison.OrdinalIgnoreCase) == true);
}
